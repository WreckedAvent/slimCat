#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageDaemon.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace Slimcat.Services
{
    #region Usings

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
    using Utilities;
    using ViewModels;
    using Views;

    #endregion

    /// <summary>
    ///     The message daemon is the service layer responsible for managing what the user sees and the commands the user
    ///     sends.
    /// </summary>
    public class MessageDaemon : DispatcherObject, IChannelManager
    {
        #region Fields

        private readonly IListConnection api;
        private readonly IDictionary<string, CommandHandler> commands;

        private readonly IChatConnection connection;

        private readonly IUnityContainer container;

        private readonly IEventAggregator events;

        private readonly IChatModel model;

        private readonly IRegionManager region;

        private ChannelModel lastSelected;

        private ILogger logger;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageDaemon" /> class.
        /// </summary>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="events">
        ///     The events.
        /// </param>
        /// <param name="model">
        ///     The model.
        /// </param>
        /// <param name="connection">
        ///     The connection.
        /// </param>
        /// <param name="api">
        ///     The api.
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
                region = regman.ThrowIfNull("regman");
                container = contain.ThrowIfNull("contain");
                this.events = events.ThrowIfNull("events");
                this.model = model.ThrowIfNull("model");
                this.connection = connection.ThrowIfNull("connection");
                this.api = api.ThrowIfNull("api");

                this.model.SelectedChannelChanged += (s, e) => RequestNavigate(this.model.CurrentChannel.Id);

                this.events.GetEvent<ChatOnDisplayEvent>()
                    .Subscribe(BuildHomeChannel, ThreadOption.UIThread, true);
                this.events.GetEvent<RequestChangeTabEvent>()
                    .Subscribe(RequestNavigate, ThreadOption.UIThread, true);
                this.events.GetEvent<UserCommandEvent>().Subscribe(CommandRecieved, ThreadOption.UIThread, true);

                commands = new Dictionary<string, CommandHandler>
                    {
                        {"priv", OnPrivRequested},
                        {"PRI", OnPriRequested},
                        {"MSG", OnMsgRequested},
                        {"LRP", OnLrpRequested},
                        {"STA", OnStatusChangeRequested},
                        {"close", OnCloseRequested},
                        {"join", OnJoinRequested},
                        {"IGN", OnIgnoreRequested},
                        {"clear", OnClearRequested},
                        {"clearall", OnClearAllRequested},
                        {"_logger_open_log", OnOpenLogRequested},
                        {"_logger_open_folder", OnOpenLogFolderRequested},
                        {"code", OnChannelCodeRequested},
                        {"_snap_to_last_update", OnNotificationFocusRequested},
                        {"CIU", OnInviteToChannelRequested},
                        {"who", OnWhoInformationRequested},
                        {"getdescription", OnChannelDescripionRequested},
                        {"interesting", OnMarkInterestedRequested},
                        {"notinteresting", OnMarkNotInterestedRequested},
                        {"SFC", OnReportRequested},
                        {"tempignore", OnTemporaryIgnoreRequested},
                        {"tempunignore", OnTemporaryIgnoreRequested},
                        {"handlelatest", OnHandleLatestReportRequested},
                        {"handlereport", OnHandleLatestReportByUserRequested}
                    };
            }
            catch (Exception ex)
            {
                ex.Source = "Message Daemon, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Delegates

        private delegate void CommandHandler(IDictionary<string, object> command);

        #endregion

        #region Properties

        private ILogger Logger
        {
            get { return logger ?? (logger = new LoggingDaemon(model.CurrentCharacter.Name)); }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The add channel.
        /// </summary>
        /// <param name="type">
        ///     The type.
        /// </param>
        /// <param name="id">
        ///     The id.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        public void AddChannel(ChannelType type, string id, string name)
        {
            if (type == ChannelType.PrivateMessage)
            {
                model.FindCharacter(id).GetAvatar(); // make sure we have their picture

                // model doesn't have a reference to PrivateMessage channels, build it manually
                var temp = new PmChannelModel(model.FindCharacter(id));
                container.RegisterInstance(temp.Id, temp);
                container.Resolve<PmChannelViewModel>(new ParameterOverride("name", temp.Id));

                Dispatcher.Invoke((Action) (() => model.CurrentPms.Add(temp)));

                // then add it to the model's data
            }
            else
            {
                GeneralChannelModel temp;
                if (type == ChannelType.Utility)
                {
                    // our model won't have a reference to home, so we build it manually
                    temp = new GeneralChannelModel(id, ChannelType.Utility);
                    container.RegisterInstance(id, temp);
                    container.Resolve<UtilityChannelViewModel>(new ParameterOverride("name", id));
                }
                else
                {
                    // our model should have a reference to other channels though
                    try
                    {
                        temp = model.AllChannels.First(param => param.Id == id);
                    }
                    catch
                    {
                        temp = new GeneralChannelModel(id, ChannelType.InviteOnly) {Title = name};
                        Dispatcher.Invoke((Action) (() => model.CurrentChannels.Add(temp)));
                    }

                    container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", id));
                }

                if (!model.CurrentChannels.Contains(temp))
                    Dispatcher.Invoke((Action) (() => model.CurrentChannels.Add(temp)));
            }
        }

        /// <summary>
        ///     The add message.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="channelName">
        ///     The channel name.
        /// </param>
        /// <param name="poster">
        ///     The poster.
        /// </param>
        /// <param name="messageType">
        ///     The message type.
        /// </param>
        public void AddMessage(
            string message, string channelName, string poster, MessageType messageType = MessageType.Normal)
        {
            var sender = poster != "_thisCharacter"
                ? model.FindCharacter(poster)
                : model.FindCharacter(model.CurrentCharacter.Name);

            var channel = model.CurrentChannels.FirstByIdOrDefault(channelName)
                          ?? (ChannelModel) model.CurrentPms.FirstByIdOrDefault(channelName);

            if (channel == null)
                return; // exception circumstance, swallow message

            Dispatcher.Invoke(
                (Action) delegate
                    {
                        var thisMessage = new MessageModel(sender, message, messageType);

                        channel.AddMessage(thisMessage, model.IsOfInterest(sender.Name));

                        if (channel.Settings.LoggingEnabled && ApplicationSettings.AllowLogging)
                        {
                            // check if the user wants logging for this channel
                            Logger.LogMessage(channel.Title, channel.Id, thisMessage);
                        }

                        if (poster == "_thisCharacter")
                            return;

                        // don't push events for our own messages
                        if (channel is GeneralChannelModel)
                        {
                            events.GetEvent<NewMessageEvent>()
                                .Publish(
                                    new Dictionary<string, object>
                                        {
                                            {"message", thisMessage},
                                            {"channel", channel}
                                        });
                        }
                        else
                            events.GetEvent<NewPmEvent>().Publish(thisMessage);
                    });
        }

        /// <summary>
        ///     The join channel.
        /// </summary>
        /// <param name="type">
        ///     The type.
        /// </param>
        /// <param name="id">
        ///     The id.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        public void JoinChannel(ChannelType type, string id, string name = "")
        {
            IEnumerable<string> history = new List<string>();
            if (!id.Equals("Home"))
                history = Logger.GetLogs(string.IsNullOrWhiteSpace(name) ? id : name, id);

            var toJoin = model.CurrentPms.FirstByIdOrDefault(id)
                         ?? (ChannelModel) model.CurrentChannels.FirstByIdOrDefault(id);

            if (toJoin == null)
            {
                AddChannel(type, id, name);

                toJoin = model.CurrentPms.FirstByIdOrDefault(id)
                         ?? (ChannelModel) model.CurrentChannels.FirstByIdOrDefault(id);
            }

            if (history.Any())
            {
                Dispatcher.BeginInvoke(
                    (Action) (() => history.Select(item => new MessageModel(item)).Each(
                        item =>
                            {
                                if (item.Type != MessageType.Normal)
                                    toJoin.Ads.Add(item);
                                else
                                    toJoin.Messages.Add(item);
                            })));
            }

            RequestNavigate(id);
        }

        /// <summary>
        ///     The remove channel.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        public void RemoveChannel(string name)
        {
            RequestNavigate("Home");

            if (model.CurrentChannels.Any(param => param.Id == name))
            {
                var temp = model.CurrentChannels.First(param => param.Id == name);

                var vm = container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", temp.Id));
                vm.Dispose();

                Dispatcher.Invoke(
                    (Action) delegate
                        {
                            model.CurrentChannels.Remove(temp);
                            temp.Dispose();
                        });

                object toSend = new {channel = name};
                connection.SendMessage(toSend, "LCH");
            }
            else if (model.CurrentPms.Any(param => param.Id == name))
            {
                var temp = model.CurrentPms.First(param => param.Id == name);

                var vm = container.Resolve<PmChannelViewModel>(new ParameterOverride("name", temp.Id));
                vm.Dispose();

                model.CurrentPms.Remove(temp);
                temp.Dispose();
            }
            else
                throw new ArgumentOutOfRangeException("name", "Could not find the channel requested to remove");
        }

        #endregion

        #region Methods

        private static void ClearChannel(ChannelModel channel)
        {
            foreach (var item in channel.Messages)
                item.Dispose();

            foreach (var item in channel.Ads)
                item.Dispose();

            channel.Messages.Clear();
            channel.Ads.Clear();
        }

        private void BuildHomeChannel(bool? payload)
        {
            events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(BuildHomeChannel);

            // we shouldn't need to know about this anymore
            JoinChannel(ChannelType.Utility, "Home");
        }

        private void OnPrivRequested(IDictionary<string, object> command)
        {
            var characterName = (string) command["character"];
            if (characterName.Equals(model.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
                events.GetEvent<ErrorEvent>().Publish("Hmmm... talking to yourself?");
            else
            {
                var guess = model.OnlineCharacters.OrderBy(c => c.Name)
                    .FirstOrDefault(c => c.Name.StartsWith(characterName, true, null));

                JoinChannel(ChannelType.PrivateMessage, guess == null ? characterName : guess.Name);
            }
        }

        private void OnPriRequested(IDictionary<string, object> command)
        {
            AddMessage(command["message"] as string, command["recipient"] as string, "_thisCharacter");
            connection.SendMessage(command);
        }

        private void OnMsgRequested(IDictionary<string, object> command)
        {
            AddMessage(command["message"] as string, command["channel"] as string, "_thisCharacter");
            connection.SendMessage(command);
        }

        private void OnLrpRequested(IDictionary<string, object> command)
        {
            AddMessage(command["message"] as string, command["channel"] as string, "_thisCharacter", MessageType.Ad);
            connection.SendMessage(command);
        }

        private void OnStatusChangeRequested(IDictionary<string, object> command)
        {
            var statusmsg = command["statusmsg"] as string;
            var status = (StatusType) Enum.Parse(typeof (StatusType), (string) command["status"], true);

            model.CurrentCharacter.Status = status;
            model.CurrentCharacter.StatusMessage = statusmsg;
            connection.SendMessage(command);
        }

        private void OnCloseRequested(IDictionary<string, object> command)
        {
            var args = (string) command["channel"];
            RemoveChannel(args);
        }

        private void OnJoinRequested(IDictionary<string, object> command)
        {
            var args = (string) command["channel"];

            if (model.CurrentChannels.FirstByIdOrDefault(args) != null)
            {
                RequestNavigate(args);
                return;
            }

            var guess =
                model.AllChannels.OrderBy(channel => channel.Title)
                    .FirstOrDefault(channel => channel.Title.StartsWith(args, true, null));

            if (guess != null)
            {
                var toSend = new {channel = guess.Id};
                connection.SendMessage(toSend, "JCH");
            }
            else
            {
                var toSend = new {channel = args};
                connection.SendMessage(toSend, "JCH");
            }
        }

        private void OnIgnoreRequested(IDictionary<string, object> command)
        {
            var args = command["character"] as string;

            lock (model.Ignored)
            {
                switch ((string) command["action"])
                {
                    case "add":
                        model.Ignored.Add(args);
                        break;
                    case "delete":
                        model.Ignored.Remove(args);
                        break;
                    default:
                        return;
                }

                events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            model.FindCharacter(args),
                            new CharacterUpdateModel.ListChangedEventArgs
                                {
                                    IsAdded = model.Ignored.Contains(args),
                                    ListArgument = CharacterUpdateModel.ListChangedEventArgs.ListType.Ignored
                                }));
            }

            connection.SendMessage(command);
        }

        private void OnClearRequested(IDictionary<string, object> command)
        {
            ClearChannel(model.CurrentChannel);
        }

        private void OnClearAllRequested(IDictionary<string, object> command)
        {
            model.CurrentChannels
                .Cast<ChannelModel>()
                .Union(model.CurrentPms)
                .Each(ClearChannel);
        }

        private void OnOpenLogRequested(IDictionary<string, object> command)
        {
            Logger.OpenLog(false, model.CurrentChannel.Title, model.CurrentChannel.Id);
        }

        private void OnOpenLogFolderRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey("channel"))
            {
                var toOpen = command["channel"] as string;
                if (!string.IsNullOrWhiteSpace(toOpen))
                    Logger.OpenLog(true, toOpen, toOpen);
            }
            else
                Logger.OpenLog(true, model.CurrentChannel.Title, model.CurrentChannel.Id);
        }

        private void OnChannelCodeRequested(IDictionary<string, object> command)
        {
            if (model.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
            {
                events.GetEvent<ErrorEvent>().Publish("Home channel does not have a code.");
                return;
            }

            var toCopy = "[session={0}]{1}[/session]".FormatWith(
                model.CurrentChannel.Title,
                model.CurrentChannel.Id);

            Clipboard.SetData(DataFormats.Text, toCopy);
            events.GetEvent<ErrorEvent>().Publish("Channel's code copied to clipboard.");
        }

        private void OnNotificationFocusRequested(IDictionary<string, object> command)
        {
            string target = null;
            string kind = null;

            if (command.ContainsKey("target"))
                target = command["target"] as string;

            if (command.ContainsKey("kind"))
                kind = command["kind"] as string;

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
                    OnHandleLatestReportByUserRequested(command);
                }

                var channel = (ChannelModel) model.CurrentPms.FirstByIdOrDefault(target)
                              ?? model.CurrentChannels.FirstByIdOrDefault(target);

                if (channel != null)
                {
                    RequestNavigate(target);
                    Dispatcher.Invoke((Action) NotificationsDaemon.ShowWindow);
                    return;
                }
            }

            var latest = model.Notifications.LastOrDefault();

            // if we got to this point our notification is doesn't involve an active tab
            if (latest == null)
                return;

            var newCharacterUpdate = latest as CharacterUpdateModel;
            if (newCharacterUpdate != null)
            {
                // so tell our system to join the Pm Tab
                JoinChannel(ChannelType.PrivateMessage, newCharacterUpdate.TargetCharacter.Name);

                Dispatcher.Invoke((Action) NotificationsDaemon.ShowWindow);
                return;
            }

            var stuffWith = latest as ChannelUpdateModel;
            if (stuffWith == null)
                return;

            var doStuffWith = stuffWith;
            var newChannel = model.AllChannels.FirstByIdOrDefault(doStuffWith.TargetChannel.Id);

            if (newChannel == null)
            {
                // if it's null, then we've got an invite to a new channel
                var toSend = new {channel = doStuffWith.TargetChannel.Id};
                connection.SendMessage(toSend, "JCH");
                Dispatcher.Invoke((Action) NotificationsDaemon.ShowWindow);
                return;
            }

            var chanType = newChannel.Type;
            JoinChannel(chanType, doStuffWith.TargetChannel.Id);
            Dispatcher.Invoke((Action) NotificationsDaemon.ShowWindow);
        }

        private void OnInviteToChannelRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey("character") && ((string) command["character"]).Equals(
                model.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
            {
                events.GetEvent<ErrorEvent>().Publish("You don't need my help to talk to yourself.");
                return;
            }

            connection.SendMessage(command);
        }

        private void OnWhoInformationRequested(IDictionary<string, object> command)
        {
            events.GetEvent<ErrorEvent>()
                .Publish(
                    "Server, server, across the sea,\nWho is connected, most to thee?\nWhy, "
                    + model.CurrentCharacter.Name + " be!");
        }

        private void OnChannelDescripionRequested(IDictionary<string, object> command)
        {
            if (model.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
            {
                events.GetEvent<ErrorEvent>()
                    .Publish("Poor home channel, with no description to speak of...");
                return;
            }

            if (model.CurrentChannel is GeneralChannelModel)
            {
                Clipboard.SetData(
                    DataFormats.Text, (model.CurrentChannel as GeneralChannelModel).Description);
                events.GetEvent<ErrorEvent>()
                    .Publish("Channel's description copied to clipboard.");
            }
            else
                events.GetEvent<ErrorEvent>().Publish("Hey! That's not a channel.");
        }

        private void OnMarkInterestedRequested(IDictionary<string, object> command)
        {
            var args = command["character"] as string;
            var isAdd = !model.Interested.Contains(args);

            model.ToggleInterestedMark(args);

            events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        model.FindCharacter(args),
                        new CharacterUpdateModel.ListChangedEventArgs
                            {
                                IsAdded = isAdd,
                                ListArgument =
                                    CharacterUpdateModel.ListChangedEventArgs.ListType.Interested
                            }));
        }

        private void OnMarkNotInterestedRequested(IDictionary<string, object> command)
        {
            var args = command["character"] as string;

            var isAdd = !model.NotInterested.Contains(args);

            model.ToggleNotInterestedMark(args);

            events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        model.FindCharacter(args),
                        new CharacterUpdateModel.ListChangedEventArgs
                            {
                                IsAdded = isAdd,
                                ListArgument =
                                    CharacterUpdateModel.ListChangedEventArgs.ListType.NotInterested
                            }));
        }

        private void OnReportRequested(IDictionary<string, object> command)
        {
            if (!command.ContainsKey("report"))
                command.Add("report", string.Empty);

            var logId = -1; // no log

            // report format: "Current Tab/Channel: <channel> | Reporting User: <reported user> | <report body>
            var reportText = string.Format(
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
                    channel = model.CurrentPms.FirstByIdOrDefault(channelText);
                else
                    channel = model.CurrentChannels.FirstByIdOrDefault(channelText);

                if (channel != null)
                {
                    var report = new ReportModel
                        {
                            Reporter = model.CurrentCharacter,
                            Reported = command["name"] as string,
                            Complaint = command["report"] as string,
                            Tab = channelText
                        };

                    logId = api.UploadLog(report, channel.Messages);
                }
            }

            command.Remove("name");
            command["report"] = reportText;
            command["logid"] = logId;

            if (!command.ContainsKey("action"))
                command["action"] = "report";

            connection.SendMessage(command);
        }

        private void OnTemporaryIgnoreRequested(IDictionary<string, object> command)
        {
            lock (model.Ignored)
            {
                var character = ((string) command["character"]).ToLower().Trim();
                var add = (string) command["type"] == "tempignore";

                if (add && !model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                    model.Ignored.Add(character);

                if (!add && model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                    model.Ignored.Remove(character);
            }
        }

        private void OnHandleLatestReportRequested(IDictionary<string, object> command)
        {
            command.Clear();
            var latest = (from n in model.Notifications
                let update = n as CharacterUpdateModel
                where update != null
                      && update.Arguments is CharacterUpdateModel.ReportFiledEventArgs
                select update).FirstOrDefault();

            if (latest == null)
                return;

            var args = latest.Arguments as CharacterUpdateModel.ReportFiledEventArgs;

            command.Add("type", "SFC");
            if (args != null) command.Add("callid", args.CallId);
            command.Add("action", "confirm");

            JoinChannel(ChannelType.PrivateMessage, latest.TargetCharacter.Name);

            var logId = -1;
            if (command.ContainsKey("logid"))
                int.TryParse(command["logid"] as string, out logId);

            if (logId != -1)
                Process.Start(Constants.UrlConstants.ReadLog + logId);

            connection.SendMessage(command);
        }

        private void OnHandleLatestReportByUserRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey("name"))
            {
                var target = model.FindCharacter(command["name"] as string);

                if (!target.HasReport)
                {
                    events.GetEvent<ErrorEvent>()
                        .Publish("Cannot find report for specified character!");
                    return;
                }

                command["type"] = "SFC";
                command.Add("callid", target.LastReport.CallId);
                if (!command.ContainsKey("action"))
                    command["action"] = "confirm";

                JoinChannel(ChannelType.PrivateMessage, target.Name);

                var logId = -1;
                if (command.ContainsKey("logid"))
                    int.TryParse(command["logid"] as string, out logId);

                if (logId != -1)
                    Process.Start(Constants.UrlConstants.ReadLog + logId);

                connection.SendMessage(command);
            }

            OnHandleLatestReportRequested(command);
        }

        private void CommandRecieved(IDictionary<string, object> command)
        {
            var type = command["type"] as string;

            if (type == null)
                return;

            try
            {
                CommandHandler handler;
                commands.TryGetValue(type, out handler);
                if (handler == null)
                {
                    connection.SendMessage(command);
                    return;
                }

                handler(command);
            }
            catch (Exception ex)
            {
                ex.Source = "Message Daemon, command received";
                Exceptions.HandleException(ex);
            }
        }

        private void RequestNavigate(string channelId)
        {
            if (lastSelected != null)
            {
                if (lastSelected.Id.Equals(channelId, StringComparison.OrdinalIgnoreCase))
                    return;

                Dispatcher.Invoke(
                    (Action) delegate
                        {
                            var toUpdate = model.CurrentChannels.FirstByIdOrDefault(lastSelected.Id)
                                           ??
                                           (ChannelModel) model.CurrentPms.FirstByIdOrDefault(lastSelected.Id);

                            if (toUpdate == null)
                                throw new ArgumentOutOfRangeException("channelId", "Cannot update unknown channel");

                            toUpdate.IsSelected = false;
                        });
            }

            var channelModel = model.CurrentChannels.FirstByIdOrDefault(channelId)
                               ?? (ChannelModel) model.CurrentPms.FirstByIdOrDefault(channelId);

            if (channelModel == null)
                throw new ArgumentOutOfRangeException("channelId", "Cannot navigate to unknown channel");

            channelModel.IsSelected = true;
            model.CurrentChannel = channelModel;

            Dispatcher.Invoke(
                (Action) delegate
                    {
                        foreach (var r in region.Regions[ChatWrapperView.ConversationRegion].Views)
                        {
                            var view = r as DisposableView;
                            if (view != null)
                            {
                                var toDispose = view;
                                toDispose.Dispose();
                                region.Regions[ChatWrapperView.ConversationRegion].Remove(toDispose);
                            }
                            else
                                region.Regions[ChatWrapperView.ConversationRegion].Remove(r);
                        }

                        region.Regions[ChatWrapperView.ConversationRegion].RequestNavigate(
                            HelperConverter.EscapeSpaces(channelModel.Id));
                    });

            lastSelected = channelModel;
        }

        #endregion
    }
}