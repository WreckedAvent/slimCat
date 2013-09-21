// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageDaemon.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The message daemon is the service layer responsible for managing what the user sees and the commands the user sends.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Threading;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using slimCat;

    using ViewModels;

    using Views;

    using Application = System.Windows.Application;
    using Clipboard = System.Windows.Forms.Clipboard;
    using DataFormats = System.Windows.Forms.DataFormats;

    /// <summary>
    ///     The message daemon is the service layer responsible for managing what the user sees and the commands the user sends.
    /// </summary>
    public class MessageDaemon : DispatcherObject, IChannelManager
    {
        #region Fields

        private readonly IListConnection _api;

        private readonly IChatConnection _connection;

        private readonly IUnityContainer _container;

        private readonly IEventAggregator _events;

        private readonly IChatModel _model;

        private readonly IRegionManager _region;

        private ChannelModel _lastSelected;

        private ILogger _logger;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDaemon"/> class.
        /// </summary>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="connection">
        /// The connection.
        /// </param>
        /// <param name="api">
        /// The api.
        /// </param>
        public MessageDaemon(
            IRegionManager regman, 
            IUnityContainer contain, 
            IEventAggregator events, 
            IChatModel model, 
            IChatConnection connection, 
            IListConnection api)
        {
            try
            {
                this._region = regman.ThrowIfNull("regman");
                this._container = contain.ThrowIfNull("contain");
                this._events = events.ThrowIfNull("events");
                this._model = model.ThrowIfNull("model");
                this._connection = connection.ThrowIfNull("connection");
                this._api = api.ThrowIfNull("api");

                this._model.SelectedChannelChanged += (s, e) => this.RequestNavigate(this._model.SelectedChannel.ID);

                this._events.GetEvent<ChatOnDisplayEvent>()
                    .Subscribe(this.BuildHomeChannel, ThreadOption.UIThread, true);
                this._events.GetEvent<RequestChangeTabEvent>()
                    .Subscribe(this.RequestNavigate, ThreadOption.UIThread, true);
                this._events.GetEvent<UserCommandEvent>().Subscribe(this.CommandRecieved, ThreadOption.UIThread, true);
            }
            catch (Exception ex)
            {
                ex.Source = "Message Daemon, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The add channel.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="ID">
        /// The id.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public void AddChannel(ChannelType type, string ID, string name)
        {
            if (type == ChannelType.pm)
            {
                this._model.FindCharacter(ID).GetAvatar(); // make sure we have their picture

                // model doesn't have a reference to pm channels, build it manually
                var temp = new PMChannelModel(this._model.FindCharacter(ID));
                this._container.RegisterInstance(temp.ID, temp);
                var pmChan = this._container.Resolve<PMChannelViewModel>(new ParameterOverride("name", temp.ID));

                this.Dispatcher.Invoke((Action)delegate { this._model.CurrentPMs.Add(temp); });

                // then add it to the model's data
            }
            else
            {
                GeneralChannelModel temp;
                if (type == ChannelType.utility)
                {
                    // our model won't have a reference to home, so we build it manually
                    temp = new GeneralChannelModel(ID, ChannelType.utility);
                    this._container.RegisterInstance(ID, temp);
                    this._container.Resolve<UtilityChannelViewModel>(new ParameterOverride("name", ID));
                }
                else
                {
                    // our model should have a reference to other channels though
                    try
                    {
                        temp = this._model.AllChannels.First(param => param.ID == ID);
                    }
                    catch
                    {
                        temp = new GeneralChannelModel(ID, ChannelType.closed) { Title = name };
                        this.Dispatcher.Invoke((Action)delegate { this._model.CurrentChannels.Add(temp); });
                    }

                    this._container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", ID));
                }

                if (!this._model.CurrentChannels.Contains(temp))
                {
                    this.Dispatcher.Invoke((Action)delegate { this._model.CurrentChannels.Add(temp); });
                }
            }
        }

        /// <summary>
        /// The add message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="channelName">
        /// The channel name.
        /// </param>
        /// <param name="poster">
        /// The poster.
        /// </param>
        /// <param name="messageType">
        /// The message type.
        /// </param>
        public void AddMessage(
            string message, string channelName, string poster, MessageType messageType = MessageType.normal)
        {
            ChannelModel channel;
            ICharacter sender = poster != "_thisCharacter"
                                    ? this._model.FindCharacter(poster)
                                    : this._model.FindCharacter(this._model.SelectedCharacter.Name);
            this.InstanceLogger();

            channel = this._model.CurrentChannels.FirstByIdOrDefault(channelName)
                      ?? (ChannelModel)this._model.CurrentPMs.FirstByIdOrDefault(channelName);

            if (channel == null)
            {
                return; // exception circumstance, swallow message
            }

            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        var thisMessage = new MessageModel(sender, message, messageType);

                        channel.AddMessage(thisMessage, this._model.IsOfInterest(sender.Name));

                        if (channel.Settings.LoggingEnabled && ApplicationSettings.AllowLogging)
                        {
                            // check if the user wants logging for this channel
                            this._logger.LogMessage(channel.Title, channel.ID, thisMessage);
                        }

                        if (poster == "_thisCharacter")
                        {
                            return;
                        }

                        // don't push events for our own messages
                        if (channel is GeneralChannelModel)
                        {
                            this._events.GetEvent<NewMessageEvent>()
                                .Publish(
                                    new Dictionary<string, object>
                                        {
                                            { "message", thisMessage }, 
                                            { "channel", channel }
                                        });
                        }
                        else
                        {
                            this._events.GetEvent<NewPMEvent>().Publish(thisMessage);
                        }
                    });
        }

        /// <summary>
        /// The command recieved.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        public void CommandRecieved(IDictionary<string, object> command)
        {
            var type = command["type"] as string;

            try
            {
                switch (type)
                {
                    case "priv":
                        {
                            var args = command["character"] as string;
                            if (args.Equals(this._model.SelectedCharacter.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                this._events.GetEvent<ErrorEvent>().Publish("Hmmm... talking to yourself?");
                            }
                            else
                            {
                                // orderby ensures that our search string won't produce a premature
                                ICharacter guess =
                                    this._model.OnlineCharacters.OrderBy(character => character.Name)
                                        .FirstOrDefault(
                                            character => character.Name.ToLower().StartsWith(args.ToLower()));

                                if (guess == null)
                                {
                                    this.JoinChannel(ChannelType.pm, args);
                                }
                                else
                                {
                                    this.JoinChannel(ChannelType.pm, guess.Name);
                                }
                            }

                            return;
                        }

                        

                    case "PRI":
                        {
                            this.AddMessage(
                                command["message"] as string, command["recipient"] as string, "_thisCharacter");
                            break;
                        }

                        

                        #region Send Channel Message command

                    case "MSG":
                        {
                            this.AddMessage(
                                command["message"] as string, command["channel"] as string, "_thisCharacter");
                            break;
                        }

                        #endregion

                        #region Send Channel Ad command

                    case "LRP":
                        {
                            this.AddMessage(
                                command["message"] as string, 
                                command["channel"] as string, 
                                "_thisCharacter", 
                                MessageType.ad);
                            break;
                        }

                        #endregion

                        #region Status Command

                    case "STA":
                        {
                            var statusmsg = command["statusmsg"] as string;
                            var status = (StatusType)Enum.Parse(typeof(StatusType), command["status"] as string);

                            this._model.SelectedCharacter.Status = status;
                            this._model.SelectedCharacter.StatusMessage = statusmsg;
                            break;
                        }

                        #endregion

                        #region Close Channel command

                    case "close":
                        {
                            var args = (string)command["channel"];
                            this.RemoveChannel(args);
                            return;
                        }

                        #endregion

                        #region Join Channel Command

                    case "join":
                        {
                            var args = (string)command["channel"];

                            if (this._model.CurrentChannels.FirstByIdOrDefault(args) != null)
                            {
                                this.RequestNavigate(args);
                                return;
                            }

                            // orderby ensures that our search string won't produce a premature
                            GeneralChannelModel guess =
                                this._model.AllChannels.OrderBy(channel => channel.Title)
                                    .FirstOrDefault(channel => channel.Title.ToLower().StartsWith(args.ToLower()));
                            if (guess != null)
                            {
                                var toSend = new { channel = guess.ID };
                                this._connection.SendMessage(toSend, "JCH");
                            }
                            else
                            {
                                var toSend = new { channel = args };
                                this._connection.SendMessage(toSend, "JCH");
                            }

                            return;
                        }

                        #endregion

                        #region Un/Ignore Command

                    case "IGN":
                        {
                            var args = command["character"] as string;

                            if ((string)command["action"] == "add")
                            {
                                this._model.Ignored.Add(args);
                            }
                            else if ((string)command["action"] == "delete")
                            {
                                this._model.Ignored.Remove(args);
                            }
                            else
                            {
                                break;
                            }

                            this._events.GetEvent<NewUpdateEvent>()
                                .Publish(
                                    new CharacterUpdateModel(
                                        this._model.FindCharacter(args), 
                                        new CharacterUpdateModel.ListChangedEventArgs
                                            {
                                                IsAdded =
                                                    this._model.Ignored
                                                        .Contains(args), 
                                                ListArgument =
                                                    CharacterUpdateModel
                                                    .ListChangedEventArgs
                                                    .ListType.ignored
                                            }));
                            break;
                        }

                        #endregion

                        #region Clear Commands

                    case "clear":
                        {
                            foreach (IMessage item in this._model.SelectedChannel.Messages)
                            {
                                item.Dispose();
                            }

                            foreach (IMessage item in this._model.SelectedChannel.Ads)
                            {
                                item.Dispose();
                            }

                            this._model.SelectedChannel.History.Clear();
                            this._model.SelectedChannel.Messages.Clear();
                            this._model.SelectedChannel.Ads.Clear();
                            return;
                        }

                    case "clearall":
                        {
                            foreach (GeneralChannelModel channel in this._model.CurrentChannels)
                            {
                                foreach (IMessage item in channel.Messages)
                                {
                                    item.Dispose();
                                }

                                foreach (IMessage item in channel.Ads)
                                {
                                    item.Dispose();
                                }

                                channel.History.Clear();
                                channel.Messages.Clear();
                                channel.Ads.Clear();
                            }

                            foreach (PMChannelModel pm in this._model.CurrentPMs)
                            {
                                foreach (IMessage item in pm.Messages)
                                {
                                    item.Dispose();
                                }

                                pm.History.Clear();
                                pm.Messages.Clear();
                            }

                            return;
                        }

                        #endregion

                        #region Logger Commands

                    case "_logger_new_line":
                        {
                            this.InstanceLogger();
                            this._logger.LogSpecial(
                                this._model.SelectedChannel.Title, 
                                this._model.SelectedChannel.ID, 
                                SpecialLogMessageKind.LineBreak, 
                                string.Empty);

                            this._events.GetEvent<ErrorEvent>().Publish("Logged a new line.");
                            return;
                        }

                    case "_logger_new_header":
                        {
                            this.InstanceLogger();
                            this._logger.LogSpecial(
                                this._model.SelectedChannel.Title, 
                                this._model.SelectedChannel.ID, 
                                SpecialLogMessageKind.Header, 
                                command["title"] as string);

                            this._events.GetEvent<ErrorEvent>()
                                .Publish("Logged a header of \'" + command["title"] + "\'");
                            return;
                        }

                    case "_logger_new_section":
                        {
                            this.InstanceLogger();
                            this._logger.LogSpecial(
                                this._model.SelectedChannel.Title, 
                                this._model.SelectedChannel.ID, 
                                SpecialLogMessageKind.Section, 
                                command["title"] as string);

                            this._events.GetEvent<ErrorEvent>()
                                .Publish("Logged a section of \'" + command["title"] + "\'");
                            return;
                        }

                    case "_logger_open_log":
                        {
                            this.InstanceLogger();

                            this._logger.OpenLog(
                                false, this._model.SelectedChannel.Title, this._model.SelectedChannel.ID);
                            return;
                        }

                    case "_logger_open_folder":
                        {
                            this.InstanceLogger();
                            if (command.ContainsKey("channel"))
                            {
                                var toOpen = command["channel"] as string;
                                if (!string.IsNullOrWhiteSpace(toOpen))
                                {
                                    this._logger.OpenLog(true, toOpen, toOpen);
                                }
                            }
                            else
                            {
                                this._logger.OpenLog(
                                    true, this._model.SelectedChannel.Title, this._model.SelectedChannel.ID);
                            }

                            return;
                        }

                        #endregion

                        #region Code Command

                    case "code":
                        {
                            if (this._model.SelectedChannel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase))
                            {
                                this._events.GetEvent<ErrorEvent>().Publish("Home channel does not have a code.");
                                return;
                            }

                            string toCopy = string.Format(
                                "[session={0}]{1}[/session]", 
                                this._model.SelectedChannel.Title, 
                                this._model.SelectedChannel.ID);
                            Clipboard.SetData(DataFormats.Text, toCopy);
                            this._events.GetEvent<ErrorEvent>().Publish("Channel's code copied to clipboard.");
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
                                    {
                                        Application.Current.MainWindow.WindowState = WindowState.Normal;
                                    }

                                    Application.Current.MainWindow.Focus();
                                };

                            if (command.ContainsKey("target"))
                            {
                                target = command["target"] as string;
                            }

                            if (command.ContainsKey("kind"))
                            {
                                kind = command["kind"] as string;
                            }

                            // first off, see if we have a target defined. If we do, then let's see if it's one of our current channels
                            if (target != null)
                            {
                                if (target.StartsWith("http://"))
                                {
                                    // if our target is a command to get the latest link-able thing, let's grab that
                                    Process.Start(target);
                                    return;
                                }

                                if (kind != null && kind.Equals("report"))
                                {
                                    command.Clear();
                                    command["name"] = target;

                                    goto case "handlereport";
                                }

                                PMChannelModel guess = this._model.CurrentPMs.FirstByIdOrDefault(target);
                                if (guess != null)
                                {
                                    this.RequestNavigate(target); // join the PM tab
                                    this.Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }
                                else
                                {
                                    GeneralChannelModel secondGuess =
                                        this._model.CurrentChannels.FirstByIdOrDefault(target);

                                    if (secondGuess != null)
                                    {
                                        this.RequestNavigate(target);

                                        // if our second guess is accurate, join the channel
                                        this.Dispatcher.Invoke(showMyDamnWindow);
                                        return;
                                    }
                                }
                            }

                            NotificationModel latest = this._model.Notifications.LastOrDefault();

                            // if we got to this point our notification is doesn't involve an active tab
                            if (latest != null)
                            {
                                if (latest is CharacterUpdateModel)
                                {
                                    // so tell our system to join the PM Tab
                                    var doStuffWith = (CharacterUpdateModel)latest;
                                    this.JoinChannel(ChannelType.pm, doStuffWith.TargetCharacter.Name);

                                    this.Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }

                                if (latest is ChannelUpdateModel)
                                {
                                    // or the channel tab
                                    // I'm not really sure how we can get a notification on a channel we're not in,
                                    // but there's no reason to crash if that is the case
                                    var doStuffWith = (ChannelUpdateModel)latest;
                                    GeneralChannelModel channel =
                                        this._model.AllChannels.FirstByIdOrDefault(doStuffWith.ChannelID);

                                    if (channel == null)
                                    {
                                        // assume it's an invite
                                        var toSend = new { channel = doStuffWith.ChannelID };
                                        this._connection.SendMessage(toSend, "JCH");

                                        // tell the server to jump on that shit
                                        this.Dispatcher.Invoke(showMyDamnWindow);
                                        return;
                                    }

                                    ChannelType chanType = channel.Type;
                                    this.JoinChannel(chanType, doStuffWith.ChannelID);
                                    this.Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }
                            }

                            return;
                        }

                        #endregion

                        #region Comic invite error message

                    case "CIU":
                        {
                            if (command.ContainsKey("character")
                                && (command["character"] as string).Equals(
                                    this._model.SelectedCharacter.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                this._events.GetEvent<ErrorEvent>()
                                    .Publish("For the record, no, inviting yourself does not a party make.");
                                return;
                            }

                            break;
                        }

                        #endregion

                        #region Comic who message

                    case "who":
                        {
                            this._events.GetEvent<ErrorEvent>()
                                .Publish(
                                    "Server, server, across the sea,\nWho is connected, most to thee?\nWhy, "
                                    + this._model.SelectedCharacter.Name + " is!");
                            return;
                        }

                        #endregion

                        #region GetDescription command

                    case "getdescription":
                        {
                            if (this._model.SelectedChannel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase))
                            {
                                this._events.GetEvent<ErrorEvent>()
                                    .Publish("Poor home channel, with no description to speak of...");
                                return;
                            }

                            if (this._model.SelectedChannel is GeneralChannelModel)
                            {
                                Clipboard.SetData(
                                    DataFormats.Text, (this._model.SelectedChannel as GeneralChannelModel).MOTD);
                                this._events.GetEvent<ErrorEvent>()
                                    .Publish("Channel's description copied to clipboard.");
                            }
                            else
                            {
                                this._events.GetEvent<ErrorEvent>().Publish("Hey! That's not a channel.");
                            }

                            return;
                        }

                        #endregion

                        #region Interesting/Uninteresting command

                    case "interesting":
                        {
                            var args = command["character"] as string;
                            bool isAdd = true;

                            if (this._model.Interested.Contains(args))
                            {
                                isAdd = false;
                            }

                            this._model.ToggleInterestedMark(args);

                            this._events.GetEvent<NewUpdateEvent>()
                                .Publish(
                                    new CharacterUpdateModel(
                                        this._model.FindCharacter(args), 
                                        new CharacterUpdateModel.ListChangedEventArgs
                                            {
                                                IsAdded = isAdd, 
                                                ListArgument =
                                                    CharacterUpdateModel
                                                    .ListChangedEventArgs
                                                    .ListType.interested
                                            }));
                            return;
                        }

                    case "notinteresting":
                        {
                            var args = command["character"] as string;

                            bool isAdd = true;

                            if (this._model.NotInterested.Contains(args))
                            {
                                isAdd = false;
                            }

                            this._model.ToggleNotInterestedMark(args);

                            this._events.GetEvent<NewUpdateEvent>()
                                .Publish(
                                    new CharacterUpdateModel(
                                        this._model.FindCharacter(args), 
                                        new CharacterUpdateModel.ListChangedEventArgs
                                            {
                                                IsAdded = isAdd, 
                                                ListArgument =
                                                    CharacterUpdateModel
                                                    .ListChangedEventArgs
                                                    .ListType.notinterested
                                            }));
                            return;
                        }

                        #endregion

                        #region Report command

                    case "SFC":
                        {
                            if (!command.ContainsKey("report"))
                            {
                                command.Add("report", string.Empty);
                            }

                            int logId = -1; // no log

                            // report format: "Current Tab/Channel: <channel> | Reporting User: <reported user> | <report body>
                            string reportText = string.Format(
                                "Current Tab/Channel: {0} | Reporting User: {1} | {2}", 
                                command["channel"] as string, 
                                command["name"] as string, 
                                command["report"] as string);

                            // upload log
                            var channelText = command["channel"] as string;
                            if (!string.IsNullOrWhiteSpace(channelText) && !channelText.Equals("None"))
                            {
                                // we could just use _model.SelectedChannel, but the user might change tabs immediately after reporting, creating a race condition
                                ChannelModel channel;
                                if (channelText == command["name"] as string)
                                {
                                    channel = this._model.CurrentPMs.FirstByIdOrDefault(channelText);
                                }
                                else
                                {
                                    channel = this._model.CurrentChannels.FirstByIdOrDefault(channelText);
                                }

                                if (channel != null)
                                {
                                    var report = new ReportModel
                                                     {
                                                         Reporter = this._model.SelectedCharacter, 
                                                         Reported = command["name"] as string, 
                                                         Complaint = command["report"] as string, 
                                                         Tab = channelText
                                                     };

                                    logId = this._api.UploadLog(report, channel.Messages);
                                }
                            }

                            command.Remove("name");
                            command["report"] = reportText;
                            command["logid"] = logId;

                            if (!command.ContainsKey("action"))
                            {
                                command["action"] = "report";
                            }

                            break;
                        }

                        #endregion

                        #region Temp ignore/unignore

                    case "tempignore":
                    case "tempunignore":
                        {
                            string character = (command["character"] as string).ToLower().Trim();
                            bool add = type == "tempignore";

                            if (add && !this._model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                            {
                                this._model.Ignored.Add(character);
                            }

                            if (!add && this._model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                            {
                                this._model.Ignored.Remove(character);
                            }

                            return;
                        }

                        #endregion

                        #region Latest report

                    case "handlelatest":
                        {
                            command.Clear();
                            CharacterUpdateModel latest = (from n in this._model.Notifications
                                                           where
                                                               n is CharacterUpdateModel
                                                               && (n as CharacterUpdateModel).Arguments is
                                                                  CharacterUpdateModel.ReportFiledEventArgs
                                                           select n as CharacterUpdateModel).FirstOrDefault();

                            if (latest == null)
                            {
                                return;
                            }

                            var args = latest.Arguments as CharacterUpdateModel.ReportFiledEventArgs;

                            command.Add("type", "SFC");
                            command.Add("callid", args.CallId);
                            command.Add("action", "confirm");

                            this.JoinChannel(ChannelType.pm, latest.TargetCharacter.Name);

                            int logId = -1;
                            if (command.ContainsKey("logid"))
                            {
                                int.TryParse(command["logid"] as string, out logId);
                            }

                            if (logId != -1)
                            {
                                Process.Start(Constants.UrlConstants.READ_LOG + logId);
                            }

                            break;
                        }

                    case "handlereport":
                        {
                            if (command.ContainsKey("name"))
                            {
                                ICharacter target = this._model.FindCharacter(command["name"] as string);

                                if (!target.HasReport)
                                {
                                    this._events.GetEvent<ErrorEvent>()
                                        .Publish("Cannot find report for specified character!");
                                    return;
                                }

                                command["type"] = "SFC";
                                command.Add("callid", target.LastReport.CallId);
                                if (!command.ContainsKey("action"))
                                {
                                    command["action"] = "confirm";
                                }

                                this.JoinChannel(ChannelType.pm, target.Name);

                                int logId = -1;
                                if (command.ContainsKey("logid"))
                                {
                                    int.TryParse(command["logid"] as string, out logId);
                                }

                                if (logId != -1)
                                {
                                    Process.Start(Constants.UrlConstants.READ_LOG + logId);
                                }

                                break;
                            }
                            else
                            {
                                goto case "handlelatest";
                            }
                        }

                        #endregion

                    default:
                        break;
                }

                this._connection.SendMessage(command);
            }
            catch (Exception ex)
            {
                ex.Source = "Message Daemon, command received";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        /// The join channel.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="ID">
        /// The id.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public void JoinChannel(ChannelType type, string ID, string name = "")
        {
            this.InstanceLogger();

            IEnumerable<string> history = new List<string>();
            if (!ID.Equals("Home"))
            {
                history = this._logger.GetLogs(string.IsNullOrWhiteSpace(name) ? ID : name, ID).ToList();
            }

            var toJoin = this._model.CurrentPMs.FirstByIdOrDefault(ID)
                         ?? (ChannelModel)this._model.CurrentChannels.FirstByIdOrDefault(ID);

            if (toJoin == null)
            {
                this.AddChannel(type, ID, name);

                toJoin = this._model.CurrentPMs.FirstByIdOrDefault(ID)
                         ?? (ChannelModel)this._model.CurrentChannels.FirstByIdOrDefault(ID);
            }

            if (history.Any())
            {
                this.Dispatcher.BeginInvoke(
                    (Action)delegate
                        {
                            toJoin.History.Clear();
                            foreach (var item in history)
                            {
                                toJoin.History.Add(item);
                            }
                        });
            }

            this.RequestNavigate(ID);
        }

        /// <summary>
        /// The remove channel.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        public void RemoveChannel(string name)
        {
            this.RequestNavigate("Home");

            if (this._model.CurrentChannels.Any(param => param.ID == name))
            {
                GeneralChannelModel temp = this._model.CurrentChannels.First(param => param.ID == name);

                var vm = this._container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", temp.ID));
                vm.Dispose();

                this.Dispatcher.Invoke(
                    (Action)delegate
                        {
                            this._model.CurrentChannels.Remove(temp);
                            temp.Dispose();
                        });

                object toSend = new { channel = name };
                this._connection.SendMessage(toSend, "LCH");
            }
            else if (this._model.CurrentPMs.Any(param => param.ID == name))
            {
                PMChannelModel temp = this._model.CurrentPMs.First(param => param.ID == name);

                var vm = this._container.Resolve<PMChannelViewModel>(new ParameterOverride("name", temp.ID));
                vm.Dispose();

                this._model.CurrentPMs.Remove(temp);
                temp.Dispose();
            }
            else
            {
                throw new ArgumentOutOfRangeException("name", "Could not find the channel requested to remove");
            }
        }

        /// <summary>
        /// The request navigate.
        /// </summary>
        /// <param name="ChannelID">
        /// The channel id.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        public void RequestNavigate(string ChannelID)
        {
            if (this._lastSelected != null)
            {
                if (this._lastSelected.ID.Equals(ChannelID, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                this.Dispatcher.Invoke(
                    (Action)delegate
                        {
                            if (this._model.CurrentChannels.Any(param => param.ID == this._lastSelected.ID))
                            {
                                GeneralChannelModel temp =
                                    this._model.CurrentChannels.First(param => param.ID == this._lastSelected.ID);
                                temp.IsSelected = false;
                            }
                            else if (this._model.CurrentPMs.Any(param => param.ID == this._lastSelected.ID))
                            {
                                PMChannelModel temp =
                                    this._model.CurrentPMs.First(param => param.ID == this._lastSelected.ID);
                                temp.IsSelected = false;
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException("ChannelID", "Cannot update unknown channel");
                            }
                        });
            }

            ChannelModel ChannelModel;

            // get the reference to our channel model
            if (this._model.CurrentChannels.Any(param => param.ID == ChannelID))
            {
                ChannelModel = this._model.CurrentChannels.First(param => param.ID == ChannelID);
            }
            else if (this._model.CurrentPMs.Any(param => param.ID == ChannelID))
            {
                ChannelModel = this._model.CurrentPMs.First(param => param.ID == ChannelID);
                ChannelModel = ChannelModel as PMChannelModel;
            }
            else
            {
                throw new ArgumentOutOfRangeException("ChannelID", "Cannot navigate to unknown channel");
            }

            ChannelModel.IsSelected = true;
            this._model.SelectedChannel = ChannelModel;

            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        foreach (object region in this._region.Regions[ChatWrapperView.ConversationRegion].Views)
                        {
                            DisposableView toDispose;

                            if (region is DisposableView)
                            {
                                toDispose = (DisposableView)region;
                                toDispose.Dispose();
                                this._region.Regions[ChatWrapperView.ConversationRegion].Remove(toDispose);
                            }
                            else
                            {
                                this._region.Regions[ChatWrapperView.ConversationRegion].Remove(region);
                            }
                        }

                        this._region.Regions[ChatWrapperView.ConversationRegion].RequestNavigate(
                            HelperConverter.EscapeSpaces(ChannelModel.ID));
                    });

            this._lastSelected = ChannelModel;
        }

        #endregion

        #region Methods

        private void BuildHomeChannel(bool? payload)
        {
            this._events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(this.BuildHomeChannel);

            // we shouldn't need to know about this anymore
            this.JoinChannel(ChannelType.utility, "Home");
        }

        // ensure that our logger has a proper instance
        private void InstanceLogger()
        {
            if (this._logger == null)
            {
                this._logger = new LoggingDaemon(this._model.SelectedCharacter.Name);
            }
        }

        #endregion
    }

    /// <summary>
    ///     The ChannelManager interface.
    /// </summary>
    public interface IChannelManager
    {
        #region Public Methods and Operators

        /// <summary>
        /// Used to join a channel but not switch to it automatically
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        void AddChannel(ChannelType type, string ID, string name = "");

        /// <summary>
        /// Used to add a message to a given channel
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="channelName">
        /// The channel Name.
        /// </param>
        /// <param name="poster">
        /// The poster.
        /// </param>
        /// <param name="messageType">
        /// The message Type.
        /// </param>
        void AddMessage(string message, string channelName, string poster, MessageType messageType = MessageType.normal);

        /// <summary>
        /// Used to join or switch to a channel
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        void JoinChannel(ChannelType type, string ID, string name = "");

        /// <summary>
        /// Used to leave a channel
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        void RemoveChannel(string name);

        #endregion
    }
}