using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using lib;

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

        private readonly List<string> applicableCommands = new List<string>
        { "priv", "pm", "_send_private_message", "_send_channel_ad",
            "status", "report", "ignore", "unignore","_send_channel_message", 
            "close", "join", "_logger_new_line", "_logger_new_header", "_logger_new_section",
          "clear", "clearall", "code"};

        private readonly IDictionary<string, string> genericCommands = new Dictionary<string, string>
        { {"makeroom", "CCR"}, {"setdescription", "CDS"}, {"invite", "CIU"},
        {"setmode", "RST" }, {"kick", "CKU"}, {"openroom", "RAN"},
        {"roll", "RLL"}, {"_typing_changed", "TPN"},
        {"ban", "CBU"}, {"promote", "COA"}, {"demote", "COR"}};

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
                                JoinChannel(ChannelType.pm, args);
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
                            string interpretedChannel;

                            if (_model.AllChannels.Any(param => param.ID.Equals(args, StringComparison.OrdinalIgnoreCase)))
                            {
                                interpretedChannel =
                                    _model
                                    .AllChannels
                                    .First(param => param.ID.Equals(args, StringComparison.OrdinalIgnoreCase))
                                    .ID;

                                object toSend = new { channel = interpretedChannel };
                                _connection.SendMessage(toSend, "JCH");
                            }
                            else
                            {
                                object toSend = new { channel = args };
                                _connection.SendMessage(toSend, "JCH");
                            }
                            return;
                        }
                    #endregion

                    #region Un/Ignore Command
                    case "IGN":
                        {
                            if ((string)command["action"] == "add")
                                _model.Ignored.Add(command["character"] as string);
                            else if ((string)command["action"] == "remove")
                                _model.Ignored.Remove(command["character"] as string);
                            break;
                        }
                    #endregion

                    #region Clear Commands
                    case "clear":
                        {
                            _model.SelectedChannel.Messages.Clear();
                            _model.SelectedChannel.Ads.Clear();
                            return;
                        }

                    case "clearall":
                        {
                            foreach (var channel in _model.CurrentChannels)
                            {
                                channel.Messages.Clear();
                                channel.Ads.Clear();
                            }

                            foreach (var pm in _model.CurrentPMs)
                                pm.Messages.Clear();

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
                    #endregion

                    #region Code Command
                    case "code":
                        {
                            if (_model.SelectedChannel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase))
                            {
                                _events.GetEvent<ErrorEvent>().Publish("Home channel does not have a code.");
                                break;
                            }

                            System.Windows.Forms.Clipboard.SetData(System.Windows.Forms.DataFormats.Text, _model.SelectedChannel.ID);
                            _events.GetEvent<ErrorEvent>().Publish("Channel's code copied to clipboard.");
                            return;
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
            if (!_model.CurrentChannels.Any(param => param.ID == ID)
                && !_model.CurrentPMs.Any(param => param.ID == ID))
                AddChannel(type, ID, name);
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
            ICharacter sender = (poster != "_thisCharacter" ? _model.FindCharacter(poster) : _model.SelectedCharacter);
            InstanceLogger();

            try
            {
                if (_model.CurrentChannels.Any(chan => chan.ID.Equals(channelName, StringComparison.OrdinalIgnoreCase)))
                    channel = _model.CurrentChannels.First(chan => chan.ID.Equals(channelName, StringComparison.OrdinalIgnoreCase));
                else
                    channel = _model.CurrentPMs.First(chan => chan.ID.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            }
            catch { throw new InvalidOperationException("Unknown Channel"); }

            Dispatcher.Invoke(
                (Action)delegate
                {
                    var thisMessage = new MessageModel(sender, message, messageType);
                    channel.AddMessage(thisMessage);

                    _logger.LogMessage(channel.Title, channel.ID, thisMessage);

                    if (poster != "_thisCharacter")
                    {
                        if (channel is GeneralChannelModel)
                        {
                            if (!channel.IsSelected)
                                _events.GetEvent<NewMessageEvent>().Publish(thisMessage);
                        }
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

            ClearViews();
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
                    #region Resolve View
                    if (ChannelModel is PMChannelModel)
                    {
                        _region.Regions[ChatWrapperView.ConversationRegion]
                            .Add(_container.Resolve<PMChannelView>(ChannelModel.ID));
                    }

                    else if (ChannelModel.Type == ChannelType.utility)
                    {
                        _region.Regions[ChatWrapperView.ConversationRegion]
                            .Add(_container.Resolve<UtilityChannelView>(ChannelModel.ID));
                    }

                    else if (ChannelModel is GeneralChannelModel)
                    {
                        _region.Regions[ChatWrapperView.ConversationRegion]
                            .Add(_container.Resolve<GeneralChannelView>(ChannelModel.ID));
                    }
                    #endregion
                });

            _lastSelected = ChannelModel;
        }

        // this is a semi-ugly hack which forcefully removes the 'channel' view and forcefully replaces it with the one selected
        private void ClearViews()
        {
            Dispatcher.Invoke(
                (Action)delegate
                {
                    foreach (object view in _region.Regions[ChatWrapperView.ConversationRegion].Views)
                        _region.Regions[ChatWrapperView.ConversationRegion].Remove(view);
                });
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
