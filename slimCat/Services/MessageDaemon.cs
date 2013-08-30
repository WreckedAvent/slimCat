/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ViewModels;
using Views;

namespace Services
{
    /// <summary>
    /// The message daemon is the service layer responsible for managing what the user sees and the commands the user sends.
    /// </summary>
    public class MessageDaemon : DispatcherObject, IChannelManager
    {
        #region Fields
        private readonly IRegionManager _region;
        private readonly IUnityContainer _container;
        private readonly IEventAggregator _events;
        private readonly IChatModel _model;
        private readonly IChatConnection _connection;

        private ChannelModel _lastSelected;
        private ILogger _logger;
        #endregion

        #region Constructors
        public MessageDaemon(IRegionManager regman, IUnityContainer contain, IEventAggregator events, IChatModel model, IChatConnection connection)
        {
            try
            {
                if (regman == null) throw new ArgumentNullException("regman");
                if (contain == null) throw new ArgumentNullException("contain");
                if (events == null) throw new ArgumentNullException("contain");
                if (model == null) throw new ArgumentNullException("model");
                if (connection == null) throw new ArgumentException("connection");

                _region = regman;
                _container = contain;
                _events = events;
                _model = model;
                _connection = connection;

                _model.SelectedChannelChanged += (s, e) => RequestNavigate(_model.SelectedChannel.ID);

                _events.GetEvent<ChatOnDisplayEvent>().Subscribe(BuildHomeChannel, ThreadOption.UIThread, true);
                _events.GetEvent<RequestChangeTabEvent>().Subscribe(RequestNavigate, ThreadOption.UIThread, true);
                _events.GetEvent<UserCommandEvent>().Subscribe(CommandRecieved, ThreadOption.UIThread, true);
            }

            catch (Exception ex)
            {
                ex.Source = "Message Daemon, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Daemon Logic
        public void CommandRecieved(IDictionary<string, object> command)
        {
            string type = command["type"] as string;

            try
            {
                switch (type)
                {
                    #region Create PM command
                    case "priv":
                    {
                        var args = command["character"] as string;
                        if (args.Equals(_model.SelectedCharacter.Name, StringComparison.OrdinalIgnoreCase))
                            _events.GetEvent<ErrorEvent>().Publish("Hmmm... talking to yourself?");
                        else
                        {
                            // orderby ensures that our search string won't produce a premature
                            var guess = _model.OnlineCharacters.OrderBy(character => character.Name).FirstOrDefault(character => character.Name.ToLower().StartsWith(args.ToLower()));

                            if (guess == null)
                                JoinChannel(ChannelType.pm, args);
                            else
                                JoinChannel(ChannelType.pm, guess.Name);
                        }
                        return;
                    }
                    #endregion

                    #region Send Private Message command
                    case "PRI":
                    {
                        AddMessage(command["message"] as string, command["recipient"] as string, "_thisCharacter");
                        break;
                    }
                    #endregion

                    #region Send Channel Message command
                    case "MSG":
                    {
                        AddMessage(command["message"] as string, command["channel"] as string, "_thisCharacter");
                        break;
                    }
                    #endregion

                    #region Send Channel Ad command
                    case "LRP":    
                    {
                        AddMessage(command["message"] as string, command["channel"] as string, "_thisCharacter", MessageType.ad);
                        break;
                    }
                    #endregion

                    #region Status Command
                    case "STA":
                    {
                        string statusmsg = command["statusmsg"] as string;
                        StatusType status = (StatusType)Enum.Parse(typeof(StatusType), command["status"] as string);

                        _model.SelectedCharacter.Status = status;
                        _model.SelectedCharacter.StatusMessage = statusmsg;
                        break;
                    }
                    #endregion

                    #region Close Channel command
                    case "close":
                        {
                            var args = (string)command["channel"];
                            RemoveChannel(args);
                            return;
                        }
                    #endregion

                    #region Join Channel Command
                    case "join":
                        {
                            var args = (string)command["channel"];

                            if (_model.CurrentChannels.FirstByIdOrDefault(args) != null)
                            {
                                RequestNavigate(args);
                                return;
                            }

                            // orderby ensures that our search string won't produce a premature
                            var guess = _model.AllChannels.OrderBy(channel => channel.Title).FirstOrDefault(channel => channel.Title.ToLower().StartsWith(args.ToLower()));
                            if (guess != null)
                            {
                                var toSend = new { channel = guess.ID };
                                _connection.SendMessage(toSend, "JCH");
                            }
                            else
                            {
                                var toSend = new { channel = args };
                                _connection.SendMessage(toSend, "JCH");
                            }
                            return;
                        }
                    #endregion

                    #region Un/Ignore Command
                    case "IGN":
                        {
                            var args = command["character"] as string;

                            if ((string)command["action"] == "add")
                                _model.Ignored.Add(args);
                            else if ((string)command["action"] == "delete")
                                _model.Ignored.Remove(args);
                            else
                                break;

                            _events.GetEvent<NewUpdateEvent>().Publish(
                                new CharacterUpdateModel(
                                    _model.FindCharacter(args),
                                    new CharacterUpdateModel.ListChangedEventArgs()
                                    {
                                        IsAdded = _model.Ignored.Contains(args),
                                        ListArgument = CharacterUpdateModel.ListChangedEventArgs.ListType.ignored
                                    }));
                            break;
                        }
                    #endregion

                    #region Clear Commands
                    case "clear":
                        {
                            foreach (var item in _model.SelectedChannel.Messages)
                                item.Dispose();
                            foreach (var item in _model.SelectedChannel.Ads)
                                item.Dispose();

                            _model.SelectedChannel.Messages.Clear();
                            _model.SelectedChannel.Ads.Clear();
                            return;
                        }

                    case "clearall":
                        {
                            foreach (var channel in _model.CurrentChannels)
                            {
                                foreach (var item in channel.Messages)
                                    item.Dispose();
                                foreach (var item in channel.Ads)
                                    item.Dispose();

                                channel.Messages.Clear();
                                channel.Ads.Clear();
                            }

                            foreach (var pm in _model.CurrentPMs)
                            {
                                foreach (var item in pm.Messages)
                                    item.Dispose();

                                pm.Messages.Clear();
                            }

                            return;
                        }
                    #endregion

                    #region Logger Commands
                    case "_logger_new_line":
                        {
                            InstanceLogger();
                            _logger.LogSpecial(_model.SelectedChannel.Title, _model.SelectedChannel.ID,
                                SpecialLogMessageKind.LineBreak, "");

                            _events.GetEvent<ErrorEvent>().Publish("Logged a new line.");
                            return;
                        }

                    case "_logger_new_header":
                        {
                            InstanceLogger();
                            _logger.LogSpecial(_model.SelectedChannel.Title, _model.SelectedChannel.ID,
                                SpecialLogMessageKind.Header, command["title"] as string);

                            _events.GetEvent<ErrorEvent>().Publish("Logged a header of \'" + command["title"] as string + "\'");
                            return;
                        }

                    case "_logger_new_section":
                        {
                            InstanceLogger();
                            _logger.LogSpecial(_model.SelectedChannel.Title, _model.SelectedChannel.ID,
                                SpecialLogMessageKind.Section, command["title"] as string);

                            _events.GetEvent<ErrorEvent>().Publish("Logged a section of \'" + command["title"] as string + "\'");
                            return;
                        }

                    case "_logger_open_log":
                        {
                            InstanceLogger();

                            _logger.OpenLog(false, _model.SelectedChannel.Title, _model.SelectedChannel.ID);
                            return;
                        }

                    case "_logger_open_folder":
                        {
                            InstanceLogger();
                            if (command.ContainsKey("channel"))
                            {
                                var toOpen = command["channel"] as string;
                                if (!string.IsNullOrWhiteSpace(toOpen))
                                    _logger.OpenLog(true, toOpen);
                            }
                            else
                                _logger.OpenLog(true, _model.SelectedChannel.Title, _model.SelectedChannel.ID);
                            return;
                        }
                    #endregion

                    #region Code Command
                    case "code":
                    {
                        if (_model.SelectedChannel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase))
                        {
                            _events.GetEvent<ErrorEvent>().Publish("Home channel does not have a code.");
                            return;
                        }
                        var toCopy = String.Format("[session={0}]{1}[/session]", _model.SelectedChannel.Title, _model.SelectedChannel.ID);
                        System.Windows.Forms.Clipboard.SetData(System.Windows.Forms.DataFormats.Text, toCopy);
                        _events.GetEvent<ErrorEvent>().Publish("Channel's code copied to clipboard.");
                        return;
                    }
                    #endregion

                    #region Notification Snap To
                    case "_snap_to_last_update":
                    {
                        string target = null;
                        string kind = null;

                        Action showMyDamnWindow = () => 
                        {
                            Application.Current.MainWindow.Show();
                            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                                Application.Current.MainWindow.WindowState = WindowState.Normal;
                            Application.Current.MainWindow.Focus();
                        };

                        if (command.ContainsKey("target"))
                            target = command["target"] as string;

                        if (command.ContainsKey("kind"))
                            kind = command["kind"] as string;

                        // first off, see if we have a target defined. If we do, then let's see if it's one of our current channels
                        if (target != null)
                        {
                            if (target.StartsWith("http://")) // if our target is a command to get the latest link-able thing, let's grab that
                            {
                                System.Diagnostics.Process.Start(target);
                                return;
                            }

                            if (kind != null && kind.Equals("report"))
                            {
                                command.Clear();
                                command["name"] = target;
                            }

                            var guess = _model.CurrentPMs.FirstByIdOrDefault(target);
                            if (guess != null)
                            {
                                RequestNavigate(target); // join the PM tab
                                Dispatcher.Invoke(showMyDamnWindow);
                                return;
                            }

                            else
                            {
                                var secondGuess = _model.CurrentChannels.FirstByIdOrDefault(target);

                                if (secondGuess != null)
                                {
                                    RequestNavigate(target); // if our second guess is accurate, join the channel
                                    Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }
                            }
                        }

                        var latest = _model.Notifications.LastOrDefault();

                        // if we got to this point our notification is doesn't involve an active tab
                        if (latest != null)
                        {
                            if (latest is CharacterUpdateModel) // so tell our system to join the PM Tab
                            {
                                var doStuffWith = (CharacterUpdateModel)latest;
                                JoinChannel(ChannelType.pm, doStuffWith.TargetCharacter.Name);

                                Dispatcher.Invoke(showMyDamnWindow);
                                return;
                            }

                            if (latest is ChannelUpdateModel) // or the channel tab
                            {
                                // I'm not really sure how we can get a notification on a channel we're not in,
                                // but there's no reason to crash if that is the case
                                var doStuffWith = (ChannelUpdateModel)latest;
                                var channel = _model.AllChannels.FirstByIdOrDefault(doStuffWith.ChannelID);

                                if (channel == null)
                                { // assume it's an invite
                                    var toSend = new { channel = doStuffWith.ChannelID };
                                    _connection.SendMessage(toSend, "JCH"); // tell the server to jump on that shit

                                    Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }

                                var chanType = channel.Type; 
                                JoinChannel(chanType, doStuffWith.ChannelID);
                                Dispatcher.Invoke(showMyDamnWindow);
                                return;
                            }
                        }
                        return;
                    }
                    #endregion

                    #region Comic invite error message
                    case "CIU":
                    {
                        if (command.ContainsKey("character") && (command["character"] as string).Equals(_model.SelectedCharacter.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            _events.GetEvent<ErrorEvent>().Publish("For the record, no, inviting yourself does not a party make.");
                            return;
                        }
                        break;
                    }
                    #endregion

                    #region Comic who message
                    case "who":
                    {
                        _events.GetEvent<ErrorEvent>().Publish(
                            "Server, server, across the sea,\nWho is connected, most to thee?\nWhy, " + _model.SelectedCharacter.Name + " is!");
                        return;
                    }
                    #endregion

                    #region GetDescription command
                    case "getdescription":
                    {
                        if (_model.SelectedChannel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase))
                        {
                            _events.GetEvent<ErrorEvent>().Publish("Poor home channel, with no description to speak of...");
                            return;
                        }

                        if (_model.SelectedChannel is GeneralChannelModel)
                        {
                            System.Windows.Forms.Clipboard.SetData(System.Windows.Forms.DataFormats.Text, (_model.SelectedChannel as GeneralChannelModel).MOTD);
                            _events.GetEvent<ErrorEvent>().Publish("Channel's description copied to clipboard.");
                        }
                        else
                            _events.GetEvent<ErrorEvent>().Publish("Hey! That's not a channel.");
                        return;
                    }
                    #endregion

                    #region Interesting/Uninteresting command
                    case "interesting":
                    {
                        var args = command["character"] as string;
                        var isAdd = true;

                        if (_model.Interested.Contains(args))
                            isAdd = false;

                        _model.ToggleInterestedMark(args);

                        _events.GetEvent<NewUpdateEvent>().Publish(
                            new CharacterUpdateModel(
                                _model.FindCharacter(args),
                                new CharacterUpdateModel.ListChangedEventArgs()
                                {
                                    IsAdded = isAdd,
                                    ListArgument = CharacterUpdateModel.ListChangedEventArgs.ListType.interested
                                }));
                        return;
                    }

                    case "notinteresting":
                    {
                        var args = command["character"] as string;

                        var isAdd = true;

                        if (_model.NotInterested.Contains(args))
                            isAdd = false;

                        _model.ToggleNotInterestedMark(args);

                        _events.GetEvent<NewUpdateEvent>().Publish(
                            new CharacterUpdateModel(
                                _model.FindCharacter(args),
                                new CharacterUpdateModel.ListChangedEventArgs()
                                {
                                    IsAdded = isAdd,
                                    ListArgument = CharacterUpdateModel.ListChangedEventArgs.ListType.notinterested
                                }));
                        return;
                    }
                    #endregion

                    #region Report command
                    case "SFC":
                    {
                        if (!command.ContainsKey("report"))
                            command.Add("report", string.Empty);

                        //  report format: "Current Tab/Channel: <channel> | Reporting User: <reported user> | <report body>
                        var reportText = string.Format("Current Tab/Channel: {0} | Reporting User: {1} | {2}",
                                                 command["channel"] as string,
                                                 command["name"] as string,
                                                 command["report"] as string);
                        command.Remove("name");
                        command["report"] = reportText;
                        command["logid"] = -1; // no log
                        if (!command.ContainsKey("action"))
                            command["action"] = "report";
                        //TODO: log uploads
                        _events.GetEvent<ErrorEvent>().Publish("Your report has been sent. Staff will be with you shortly.");
                        break;
                    }
                    #endregion

                    #region Temp ignore/unignore
                    case "tempignore":
                    case "tempunignore":
                    {
                        var character = (command["character"] as string).ToLower().Trim();
                        bool add = type == "tempignore";

                        if (add && !_model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                            _model.Ignored.Add(character);
                        if (!add && _model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                            _model.Ignored.Remove(character);
                        return;
                    }
                    #endregion

                    #region Latest report
                    case "handlelatest": 
                    {
                        command.Clear();
                        var latest = (from n in _model.Notifications
                                     where n is CharacterUpdateModel
                                     && (n as CharacterUpdateModel).Arguments is CharacterUpdateModel.ReportFiledEventArgs
                                     select n as CharacterUpdateModel)
                                     .FirstOrDefault();

                        if (latest == null)
                            return;

                        var args = latest.Arguments as CharacterUpdateModel.ReportFiledEventArgs;

                        command.Add("type", "SFC");
                        command.Add("callid", args.CallId);
                        command.Add("action", "confirm");
                        break;
                    }

                    case "handlereport":
                    {
                        if (command.ContainsKey("name"))
                        {
                            var latest = (from n in _model.Notifications
                                          where n is CharacterUpdateModel
                                          && (n as CharacterUpdateModel).Arguments is CharacterUpdateModel.ReportFiledEventArgs
                                          select n as CharacterUpdateModel)
                                          .FirstOrDefault(n => n.TargetCharacter.NameContains(command["name"] as string));

                            if (latest == null)
                                return;

                            var args = latest.Arguments as CharacterUpdateModel.ReportFiledEventArgs;

                            command["type"] = "SFC";
                            command.Add("callid", args.CallId);
                            if (!command.ContainsKey("action"))
                                command["action"] = "confirm";
                        }
                        else
                            goto case "handlelatest";

                        break;
                    }
                    #endregion

                    default: break;
                }

                _connection.SendMessage(command);
            }

            catch (Exception ex)
            {
                ex.Source = "Message Daemon, command received";
                Exceptions.HandleException(ex);
            }

        }
        #endregion

        #region Methods
        public void JoinChannel(ChannelType type, string ID, string name = "")
        {
            var pm = _model.CurrentPMs.FirstByIdOrDefault(ID);
            if (pm == null)
            {
                var channel = _model.CurrentChannels.FirstByIdOrDefault(ID);
                if (channel == null)
                    AddChannel(type, ID, name);
            }
            RequestNavigate(ID);
        }

        public void AddChannel(ChannelType type, string ID, string name)
        {
            if (type == ChannelType.pm)
            {
                _model.FindCharacter(ID).GetAvatar(); // make sure we have their picture

                // model doesn't have a reference to pm channels, build it manually
                var temp = new PMChannelModel(_model.FindCharacter(ID));
                _container.RegisterInstance<PMChannelModel>(temp.ID, temp);
                PMChannelViewModel pmChan = _container.Resolve<PMChannelViewModel>(new ParameterOverride("name", temp.ID));

                Dispatcher.Invoke(
                    (Action)delegate
                    {
                        _model.CurrentPMs.Add(temp);
                    }
                ); // then add it to the model's data
            }

            else
            {
                GeneralChannelModel temp;
                if (type == ChannelType.utility)
                {
                    // our model won't have a reference to home, so we build it manually
                    temp = new GeneralChannelModel(ID, ChannelType.utility);
                    _container.RegisterInstance<GeneralChannelModel>(ID, temp);
                    _container.Resolve<UtilityChannelViewModel>(new ParameterOverride("name", ID));
                }

                else
                {
                    // our model should have a reference to other channels though
                    try
                    {
                        temp = _model.AllChannels.First(param => param.ID == ID);
                    }

                    catch
                    {
                        temp = new GeneralChannelModel(ID, ChannelType.closed) { Title = name };
                        Dispatcher.Invoke(
                            (Action)delegate{
                                _model.CurrentChannels.Add(temp);
                            });
                    }
                    _container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", ID));
                }

                if (!_model.CurrentChannels.Contains(temp))
                    Dispatcher.Invoke(
                            (Action)delegate{
                        _model.CurrentChannels.Add(temp);
                            });
            }
        }

        public void RemoveChannel(string name)
        {
            RequestNavigate("Home");

            if (_model.CurrentChannels.Any(param => param.ID == name))
            {
                var temp = _model.CurrentChannels.First(param => param.ID == name);

                var vm = _container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", temp.ID));
                vm.Dispose();

                Dispatcher.Invoke(
                    (Action)delegate
                    {
                        _model.CurrentChannels.Remove(temp);
                        temp.Dispose();
                    });

                object toSend = new { channel = name };
                _connection.SendMessage(toSend, "LCH");
            }

            else if (_model.CurrentPMs.Any(param => param.ID == name))
            {
                var temp = _model.CurrentPMs.First(param => param.ID == name);

                var vm = _container.Resolve<PMChannelViewModel>(new ParameterOverride("name", temp.ID));
                vm.Dispose();

                _model.CurrentPMs.Remove(temp);
                temp.Dispose();
            }

            else throw new ArgumentOutOfRangeException("name", "Could not find the channel requested to remove");

        }

        public void AddMessage(string message, string channelName, string poster, MessageType messageType = MessageType.normal)
        {
            ChannelModel channel;
            ICharacter sender = (poster != "_thisCharacter" ? _model.FindCharacter(poster) : _model.FindCharacter(_model.SelectedCharacter.Name));
            InstanceLogger();

            channel = _model.CurrentChannels.FirstByIdOrDefault(channelName);

            if (channel == null)
                channel = _model.CurrentPMs.FirstByIdOrDefault(channelName);

            if (channel == null)
                return; // exception circumstance, swallow message

            Dispatcher.Invoke(
                (Action)delegate
                {
                    var thisMessage = new MessageModel(sender, message, messageType);

                    channel.AddMessage(thisMessage, _model.IsOfInterest(sender.Name));

                    if (channel.Settings.LoggingEnabled && ApplicationSettings.AllowLogging) // check if the user wants logging for this channel
                        _logger.LogMessage(channel.Title, channel.ID, thisMessage);

                    if (poster != "_thisCharacter") // don't push events for our own messages
                    {
                        if (channel is GeneralChannelModel)
                            _events.GetEvent<NewMessageEvent>().Publish(new Dictionary<string, object>() {{"message", thisMessage}, {"channel", channel}});
                        else
                            _events.GetEvent<NewPMEvent>().Publish(thisMessage);
                    }
                });
        }

        private void BuildHomeChannel(bool? payload)
        {
            _events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(BuildHomeChannel); // we shouldn't need to know about this anymore

            JoinChannel(ChannelType.utility, "Home");
        }

        public void RequestNavigate(string ChannelID)
        {
            if (_lastSelected != null)
            {
                if (_lastSelected.ID.Equals(ChannelID, StringComparison.OrdinalIgnoreCase)) return;
                Dispatcher.Invoke(
                    (Action)delegate
                    {
                        if (_model.CurrentChannels.Any(param => param.ID == _lastSelected.ID))
                        {
                            var temp = _model.CurrentChannels.First(param => param.ID == _lastSelected.ID);
                            temp.IsSelected = false;
                        }

                        else if (_model.CurrentPMs.Any(param => param.ID == _lastSelected.ID))
                        {
                            var temp = _model.CurrentPMs.First(param => param.ID == _lastSelected.ID);
                            temp.IsSelected = false;
                        }

                        else throw new ArgumentOutOfRangeException("ChannelID", "Cannot update unknown channel");
                    });
            }

            ChannelModel ChannelModel;
            // get the reference to our channel model
            if (_model.CurrentChannels.Any(param => param.ID == ChannelID))
                ChannelModel = _model.CurrentChannels.First(param => param.ID == ChannelID);

            else if (_model.CurrentPMs.Any(param => param.ID == ChannelID))
            {
                ChannelModel = _model.CurrentPMs.First(param => param.ID == ChannelID);
                ChannelModel = ChannelModel as PMChannelModel;
            }

            else throw new ArgumentOutOfRangeException("ChannelID", "Cannot navigate to unknown channel");

            ChannelModel.IsSelected = true;
            _model.SelectedChannel = ChannelModel;

            Dispatcher.Invoke(
                (Action)delegate
                {
                    foreach (var region in _region.Regions[ChatWrapperView.ConversationRegion].Views)
                    {
                        DisposableView toDispose;

                        if (region is DisposableView)
                        {
                            toDispose = (DisposableView)region;
                            toDispose.Dispose();
                            _region.Regions[ChatWrapperView.ConversationRegion].Remove(toDispose);
                        }
                        else
                            _region.Regions[ChatWrapperView.ConversationRegion].Remove(region);
                    }
                    _region.Regions[ChatWrapperView.ConversationRegion].RequestNavigate(HelperConverter.EscapeSpaces(ChannelModel.ID));
                });

            _lastSelected = ChannelModel;
        }

        // ensure that our logger has a proper instance
        private void InstanceLogger()
        {
            if (_logger == null)
                _logger = new LoggingDaemon(_model.SelectedCharacter.Name);
        }
        #endregion
    }

    public interface IChannelManager
    {
        /// <summary>
        /// Used to join or switch to a channel
        /// </summary>
        void JoinChannel(ChannelType type, string ID, string name = "");

        /// <summary>
        /// Used to join a channel but not switch to it automatically
        /// </summary>
        void AddChannel(ChannelType type, string ID, string name = "");

        /// <summary>
        /// Used to leave a channel
        /// </summary>
        void RemoveChannel(string name);

        /// <summary>
        /// Used to add a message to a given channel
        /// </summary>
        void AddMessage(string message, string channelName, string poster, MessageType messageType = MessageType.normal);
    }
}
