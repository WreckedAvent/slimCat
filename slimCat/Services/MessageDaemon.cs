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

namespace Slimcat.Services
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
    using Utilities;
    using ViewModels;
    using Views;

    /// <summary>
    ///     The message daemon is the service layer responsible for managing what the user sees and the commands the user sends.
    /// </summary>
    public class MessageDaemon : DispatcherObject, IChannelManager
    {
        #region Fields

        private readonly IListConnection api;

        private readonly IChatConnection connection;

        private readonly IUnityContainer container;

        private readonly IEventAggregator events;

        private readonly IChatModel model;

        private readonly IRegionManager region;

        private readonly IDictionary<string, CommandHandler> commands;

        private ChannelModel lastSelected;

        private ILogger logger;

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
                this.region = regman.ThrowIfNull("regman");
                this.container = contain.ThrowIfNull("contain");
                this.events = events.ThrowIfNull("events");
                this.model = model.ThrowIfNull("model");
                this.connection = connection.ThrowIfNull("connection");
                this.api = api.ThrowIfNull("api");

                this.model.SelectedChannelChanged += (s, e) => this.RequestNavigate(this.model.CurrentChannel.Id);

                this.events.GetEvent<ChatOnDisplayEvent>()
                    .Subscribe(this.BuildHomeChannel, ThreadOption.UIThread, true);
                this.events.GetEvent<RequestChangeTabEvent>()
                    .Subscribe(this.RequestNavigate, ThreadOption.UIThread, true);
                this.events.GetEvent<UserCommandEvent>().Subscribe(this.CommandRecieved, ThreadOption.UIThread, true);

                this.commands = new Dictionary<string, CommandHandler>
                {
                    { "priv", this.OnPrivRequested },
                    { "PRI", this.OnPriRequested },
                    { "MSG", this.OnMsgRequested },
                    { "LRP", this.OnLrpRequested },
                    { "STA", this.OnStatusChangeRequested },
                    { "close", this.OnCloseRequested },
                    { "join", this.OnJoinRequested },
                    { "IGN", this.OnIgnoreRequested },
                    { "clear", this.OnClearRequested },
                    { "clearall", this.OnClearAllRequested },
                    { "_logger_open_log", this.OnOpenLogRequested },
                    { "_logger_open_folder", this.OnOpenLogFolderRequested },
                    { "code", this.OnChannelCodeRequested },
                    { "_snap_to_last_update", this.OnNotificationFocusRequested },
                    { "CIU", this.OnInviteToChannelRequested },
                    { "who", this.OnWhoInformationRequested },
                    { "getdescription", this.OnChannelDescripionRequested },
                    { "interesting", this.OnMarkInterestedRequested },
                    { "notinteresting", this.OnMarkNotInterestedRequested },
                    { "SFC", this.OnReportRequested },
                    { "tempignore", this.OnTemporaryIgnoreRequested },
                    { "tempunignore", this.OnTemporaryIgnoreRequested },
                    { "handlelatest", this.OnHandleLatestReportRequested },
                    { "handlereport", this.OnHandleLatestReportByUserRequested }
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
            get
            {
                return this.logger ?? (this.logger = new LoggingDaemon(this.model.CurrentCharacter.Name));
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
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public void AddChannel(ChannelType type, string id, string name)
        {
            if (type == ChannelType.PrivateMessage)
            {
                this.model.FindCharacter(id).GetAvatar(); // make sure we have their picture

                // model doesn't have a reference to PrivateMessage channels, build it manually
                var temp = new PmChannelModel(this.model.FindCharacter(id));
                this.container.RegisterInstance(temp.Id, temp);
                this.container.Resolve<PmChannelViewModel>(new ParameterOverride("name", temp.Id));

                this.Dispatcher.Invoke((Action)(() => this.model.CurrentPms.Add(temp)));

                // then add it to the model's data
            }
            else
            {
                GeneralChannelModel temp;
                if (type == ChannelType.Utility)
                {
                    // our model won't have a reference to home, so we build it manually
                    temp = new GeneralChannelModel(id, ChannelType.Utility);
                    this.container.RegisterInstance(id, temp);
                    this.container.Resolve<UtilityChannelViewModel>(new ParameterOverride("name", id));
                }
                else
                {
                    // our model should have a reference to other channels though
                    try
                    {
                        temp = this.model.AllChannels.First(param => param.Id == id);
                    }
                    catch
                    {
                        temp = new GeneralChannelModel(id, ChannelType.InviteOnly) { Title = name };
                        this.Dispatcher.Invoke((Action)(() => this.model.CurrentChannels.Add(temp)));
                    }

                    this.container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", id));
                }

                if (!this.model.CurrentChannels.Contains(temp))
                {
                    this.Dispatcher.Invoke((Action)(() => this.model.CurrentChannels.Add(temp)));
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
            string message, string channelName, string poster, MessageType messageType = MessageType.Normal)
        {
            var sender = poster != "_thisCharacter"
                                    ? this.model.FindCharacter(poster)
                                    : this.model.FindCharacter(this.model.CurrentCharacter.Name);

            var channel = this.model.CurrentChannels.FirstByIdOrDefault(channelName)
                                   ?? (ChannelModel)this.model.CurrentPms.FirstByIdOrDefault(channelName);

            if (channel == null)
            {
                return; // exception circumstance, swallow message
            }

            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        var thisMessage = new MessageModel(sender, message, messageType);

                        channel.AddMessage(thisMessage, this.model.IsOfInterest(sender.Name));

                        if (channel.Settings.LoggingEnabled && ApplicationSettings.AllowLogging)
                        {
                            // check if the user wants logging for this channel
                            this.Logger.LogMessage(channel.Title, channel.Id, thisMessage);
                        }

                        if (poster == "_thisCharacter")
                        {
                            return;
                        }

                        // don't push events for our own messages
                        if (channel is GeneralChannelModel)
                        {
                            this.events.GetEvent<NewMessageEvent>()
                                .Publish(
                                    new Dictionary<string, object>
                                        {
                                            { "message", thisMessage }, 
                                            { "channel", channel }
                                        });
                        }
                        else
                        {
                            this.events.GetEvent<NewPmEvent>().Publish(thisMessage);
                        }
                    });
        }

        /// <summary>
        /// The join channel.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public void JoinChannel(ChannelType type, string id, string name = "")
        {
            IEnumerable<string> history = new List<string>();
            if (!id.Equals("Home"))
            {
                history = this.Logger.GetLogs(string.IsNullOrWhiteSpace(name) ? id : name, id);
            }

            var toJoin = this.model.CurrentPms.FirstByIdOrDefault(id)
                         ?? (ChannelModel)this.model.CurrentChannels.FirstByIdOrDefault(id);

            if (toJoin == null)
            {
                this.AddChannel(type, id, name);

                toJoin = this.model.CurrentPms.FirstByIdOrDefault(id)
                         ?? (ChannelModel)this.model.CurrentChannels.FirstByIdOrDefault(id);
            }

            if (history.Any())
            {
                this.Dispatcher.BeginInvoke(
                    (Action)(() => history.Select(item => new MessageModel(item)).Each(
                        item =>
                            {
                                if (item.Type != MessageType.Normal)
                                {
                                    toJoin.Ads.Add(item);
                                }
                                else
                                {
                                    toJoin.Messages.Add(item);
                                }
                            })));
            }

            this.RequestNavigate(id);
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

            if (this.model.CurrentChannels.Any(param => param.Id == name))
            {
                var temp = this.model.CurrentChannels.First(param => param.Id == name);

                var vm = this.container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", temp.Id));
                vm.Dispose();

                this.Dispatcher.Invoke(
                    (Action)delegate
                        {
                            this.model.CurrentChannels.Remove(temp);
                            temp.Dispose();
                        });

                object toSend = new { channel = name };
                this.connection.SendMessage(toSend, "LCH");
            }
            else if (this.model.CurrentPms.Any(param => param.Id == name))
            {
                var temp = this.model.CurrentPms.First(param => param.Id == name);

                var vm = this.container.Resolve<PmChannelViewModel>(new ParameterOverride("name", temp.Id));
                vm.Dispose();

                this.model.CurrentPms.Remove(temp);
                temp.Dispose();
            }
            else
            {
                throw new ArgumentOutOfRangeException("name", "Could not find the channel requested to remove");
            }
        }
        #endregion

        #region Methods
        private static void ClearChannel(ChannelModel channel)
        {
            foreach (var item in channel.Messages)
            {
                item.Dispose();
            }

            foreach (var item in channel.Ads)
            {
                item.Dispose();
            }

            channel.Messages.Clear();
            channel.Ads.Clear();
        }

        private void BuildHomeChannel(bool? payload)
        {
            this.events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(this.BuildHomeChannel);

            // we shouldn't need to know about this anymore
            this.JoinChannel(ChannelType.Utility, "Home");
        }

        private void OnPrivRequested(IDictionary<string, object> command)
        {
            var characterName = (string)command["character"];
            if (characterName.Equals(this.model.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
            {
                this.events.GetEvent<ErrorEvent>().Publish("Hmmm... talking to yourself?");
            }
            else
            {
                ICharacter guess;
                lock (this.model.OnlineCharacters)
                {
                    guess =
                        this.model.OnlineCharacters.OrderBy(c => c.Name)
                            .FirstOrDefault(c => c.Name.StartsWith(characterName, true, null));
                }

                this.JoinChannel(ChannelType.PrivateMessage, guess == null ? characterName : guess.Name);
            }
        }

        private void OnPriRequested(IDictionary<string, object> command)
        {
            this.AddMessage(command["message"] as string, command["recipient"] as string, "_thisCharacter");
            this.connection.SendMessage(command);
        }

        private void OnMsgRequested(IDictionary<string, object> command)
        {
            this.AddMessage(command["message"] as string, command["channel"] as string, "_thisCharacter");
            this.connection.SendMessage(command);
        }

        private void OnLrpRequested(IDictionary<string, object> command)
        {
            this.AddMessage(command["message"] as string, command["channel"] as string, "_thisCharacter", MessageType.Ad);
            this.connection.SendMessage(command);
        }

        private void OnStatusChangeRequested(IDictionary<string, object> command)
        {
            var statusmsg = command["statusmsg"] as string;
            var status = (StatusType)Enum.Parse(typeof(StatusType), (string)command["status"], true);

            this.model.CurrentCharacter.Status = status;
            this.model.CurrentCharacter.StatusMessage = statusmsg;
            this.connection.SendMessage(command);
        }

        private void OnCloseRequested(IDictionary<string, object> command)
        {
            var args = (string)command["channel"];
            this.RemoveChannel(args);
        }

        private void OnJoinRequested(IDictionary<string, object> command)
        {
            var args = (string)command["channel"];

            if (this.model.CurrentChannels.FirstByIdOrDefault(args) != null)
            {
                this.RequestNavigate(args);
                return;
            }

            var guess =
                this.model.AllChannels.OrderBy(channel => channel.Title)
                    .FirstOrDefault(channel => channel.Title.StartsWith(args, true, null));

            if (guess != null)
            {
                var toSend = new { channel = guess.Id };
                this.connection.SendMessage(toSend, "JCH");
            }
            else
            {
                var toSend = new { channel = args };
                this.connection.SendMessage(toSend, "JCH");
            }
        }

        private void OnIgnoreRequested(IDictionary<string, object> command)
        {
            var args = command["character"] as string;

            lock (this.model.Ignored)
            {
                switch ((string)command["action"])
                {
                    case "add":
                        this.model.Ignored.Add(args);
                        break;
                    case "delete":
                        this.model.Ignored.Remove(args);
                        break;
                    default:
                        return;
                }

                this.events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            this.model.FindCharacter(args),
                            new CharacterUpdateModel.ListChangedEventArgs
                                {
                                    IsAdded = this.model.Ignored.Contains(args),
                                    ListArgument = CharacterUpdateModel.ListChangedEventArgs.ListType.Ignored
                                }));
            }

            this.connection.SendMessage(command);
        }

        private void OnClearRequested(IDictionary<string, object> command)
        {
            ClearChannel(this.model.CurrentChannel);
        }

        private void OnClearAllRequested(IDictionary<string, object> command)
        {
            this.model.CurrentChannels
                .Cast<ChannelModel>()
                .Union(this.model.CurrentPms)
                .Each(ClearChannel);
        }

        private void OnOpenLogRequested(IDictionary<string, object> command)
        {
            this.Logger.OpenLog(false, this.model.CurrentChannel.Title, this.model.CurrentChannel.Id);
        }

        private void OnOpenLogFolderRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey("channel"))
            {
                var toOpen = command["channel"] as string;
                if (!string.IsNullOrWhiteSpace(toOpen))
                {
                    this.Logger.OpenLog(true, toOpen, toOpen);
                }
            }
            else
            {
                this.Logger.OpenLog(true, this.model.CurrentChannel.Title, this.model.CurrentChannel.Id);
            }
        }

        private void OnChannelCodeRequested(IDictionary<string, object> command)
        {
            if (this.model.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
            {
                this.events.GetEvent<ErrorEvent>().Publish("Home channel does not have a code.");
                return;
            }

            var toCopy = "[session={0}]{1}[/session]".FormatWith(
                this.model.CurrentChannel.Title,
                this.model.CurrentChannel.Id);

            Clipboard.SetData(DataFormats.Text, toCopy);
            this.events.GetEvent<ErrorEvent>().Publish("Channel's code copied to clipboard.");
        }

        private void OnNotificationFocusRequested(IDictionary<string, object> command)
        {
            string target = null;
            string kind = null;

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
                    this.OnHandleLatestReportByUserRequested(command);
                }

                var channel = (ChannelModel)this.model.CurrentPms.FirstByIdOrDefault(target)
                              ?? this.model.CurrentChannels.FirstByIdOrDefault(target);

                if (channel != null)
                {
                    this.RequestNavigate(target);
                    this.Dispatcher.Invoke((Action)NotificationsDaemon.ShowWindow);
                    return;
                }
            }

            var latest = this.model.Notifications.LastOrDefault();

            // if we got to this point our notification is doesn't involve an active tab
            if (latest == null)
            {
                return;
            }

            var newCharacterUpdate = latest as CharacterUpdateModel;
            if (newCharacterUpdate != null)
            {
                // so tell our system to join the Pm Tab
                this.JoinChannel(ChannelType.PrivateMessage, newCharacterUpdate.TargetCharacter.Name);

                this.Dispatcher.Invoke((Action)NotificationsDaemon.ShowWindow);
                return;
            }

            var stuffWith = latest as ChannelUpdateModel;
            if (stuffWith == null)
            {
                return;
            }

            var doStuffWith = stuffWith;
            var newChannel = this.model.AllChannels.FirstByIdOrDefault(doStuffWith.TargetChannel.Id);

            if (newChannel == null)
            {
                // if it's null, then we've got an invite to a new channel
                var toSend = new { channel = doStuffWith.TargetChannel.Id };
                this.connection.SendMessage(toSend, "JCH");
                this.Dispatcher.Invoke((Action)NotificationsDaemon.ShowWindow);
                return;
            }

            var chanType = newChannel.Type;
            this.JoinChannel(chanType, doStuffWith.TargetChannel.Id);
            this.Dispatcher.Invoke((Action)NotificationsDaemon.ShowWindow);
        }

        private void OnInviteToChannelRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey("character") && ((string)command["character"]).Equals(
                    this.model.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
            {
                this.events.GetEvent<ErrorEvent>().Publish("You don't need my help to talk to yourself.");
                return;
            }

            this.connection.SendMessage(command);
        }

        private void OnWhoInformationRequested(IDictionary<string, object> command)
        {
            this.events.GetEvent<ErrorEvent>()
                .Publish(
                    "Server, server, across the sea,\nWho is connected, most to thee?\nWhy, "
                    + this.model.CurrentCharacter.Name + " is!");
        }

        private void OnChannelDescripionRequested(IDictionary<string, object> command)
        {
            if (this.model.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
            {
                this.events.GetEvent<ErrorEvent>()
                    .Publish("Poor home channel, with no description to speak of...");
                return;
            }

            if (this.model.CurrentChannel is GeneralChannelModel)
            {
                Clipboard.SetData(
                    DataFormats.Text, (this.model.CurrentChannel as GeneralChannelModel).Description);
                this.events.GetEvent<ErrorEvent>()
                    .Publish("Channel's description copied to clipboard.");
            }
            else
            {
                this.events.GetEvent<ErrorEvent>().Publish("Hey! That's not a channel.");
            }
        }

        private void OnMarkInterestedRequested(IDictionary<string, object> command)
        {
            var args = command["character"] as string;
            var isAdd = !this.model.Interested.Contains(args);

            this.model.ToggleInterestedMark(args);

            this.events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        this.model.FindCharacter(args),
                        new CharacterUpdateModel.ListChangedEventArgs
                        {
                            IsAdded = isAdd,
                            ListArgument =
                                CharacterUpdateModel
                                .ListChangedEventArgs
                                .ListType.Interested
                        }));
        }

        private void OnMarkNotInterestedRequested(IDictionary<string, object> command)
        {
            var args = command["character"] as string;

            var isAdd = !this.model.NotInterested.Contains(args);

            this.model.ToggleNotInterestedMark(args);

            this.events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        this.model.FindCharacter(args),
                        new CharacterUpdateModel.ListChangedEventArgs
                        {
                            IsAdded = isAdd,
                            ListArgument =
                                CharacterUpdateModel
                                .ListChangedEventArgs
                                .ListType.NotInterested
                        }));
        }

        private void OnReportRequested(IDictionary<string, object> command)
        {
            if (!command.ContainsKey("report"))
            {
                command.Add("report", string.Empty);
            }

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
                {
                    channel = this.model.CurrentPms.FirstByIdOrDefault(channelText);
                }
                else
                {
                    channel = this.model.CurrentChannels.FirstByIdOrDefault(channelText);
                }

                if (channel != null)
                {
                    var report = new ReportModel
                    {
                        Reporter = this.model.CurrentCharacter,
                        Reported = command["name"] as string,
                        Complaint = command["report"] as string,
                        Tab = channelText
                    };

                    logId = this.api.UploadLog(report, channel.Messages);
                }
            }

            command.Remove("name");
            command["report"] = reportText;
            command["logid"] = logId;

            if (!command.ContainsKey("action"))
            {
                command["action"] = "report";
            }

            this.connection.SendMessage(command);
        }

        private void OnTemporaryIgnoreRequested(IDictionary<string, object> command)
        {
            lock (this.model.Ignored)
            {
                var character = ((string)command["character"]).ToLower().Trim();
                var add = (string)command["type"] == "tempignore";

                if (add && !this.model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                {
                    this.model.Ignored.Add(character);
                }

                if (!add && this.model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                {
                    this.model.Ignored.Remove(character);
                }
            }
        }

        private void OnHandleLatestReportRequested(IDictionary<string, object> command)
        {
            command.Clear();
            var latest = (from n in this.model.Notifications
                          let update = n as CharacterUpdateModel
                          where update != null
                              && update.Arguments is CharacterUpdateModel.ReportFiledEventArgs
                          select update).FirstOrDefault();

            if (latest == null)
            {
                return;
            }

            var args = latest.Arguments as CharacterUpdateModel.ReportFiledEventArgs;

            command.Add("type", "SFC");
            if (args != null) command.Add("callid", args.CallId);
            command.Add("action", "confirm");

            this.JoinChannel(ChannelType.PrivateMessage, latest.TargetCharacter.Name);

            var logId = -1;
            if (command.ContainsKey("logid"))
            {
                int.TryParse(command["logid"] as string, out logId);
            }

            if (logId != -1)
            {
                Process.Start(Constants.UrlConstants.ReadLog + logId);
            }

            this.connection.SendMessage(command);
        }

        private void OnHandleLatestReportByUserRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey("name"))
            {
                var target = this.model.FindCharacter(command["name"] as string);

                if (!target.HasReport)
                {
                    this.events.GetEvent<ErrorEvent>()
                        .Publish("Cannot find report for specified character!");
                    return;
                }

                command["type"] = "SFC";
                command.Add("callid", target.LastReport.CallId);
                if (!command.ContainsKey("action"))
                {
                    command["action"] = "confirm";
                }

                this.JoinChannel(ChannelType.PrivateMessage, target.Name);

                var logId = -1;
                if (command.ContainsKey("logid"))
                {
                    int.TryParse(command["logid"] as string, out logId);
                }

                if (logId != -1)
                {
                    Process.Start(Constants.UrlConstants.ReadLog + logId);
                }

                this.connection.SendMessage(command);
            }

            this.OnHandleLatestReportRequested(command);
        }

        private void CommandRecieved(IDictionary<string, object> command)
        {
            var type = command["type"] as string;

            if (type == null)
            {
                return;
            }

            try
            {
                CommandHandler handler;
                this.commands.TryGetValue(type, out handler);
                if (handler == null)
                {
                    this.connection.SendMessage(command);
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
            if (this.lastSelected != null)
            {
                if (this.lastSelected.Id.Equals(channelId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                this.Dispatcher.Invoke(
                    (Action)delegate
                    {
                        var toUpdate = this.model.CurrentChannels.FirstByIdOrDefault(this.lastSelected.Id)
                                       ?? (ChannelModel)this.model.CurrentPms.FirstByIdOrDefault(this.lastSelected.Id);

                        if (toUpdate == null)
                        {
                            throw new ArgumentOutOfRangeException("channelId", "Cannot update unknown channel");
                        }

                        toUpdate.IsSelected = false;
                    });
            }

            var channelModel = this.model.CurrentChannels.FirstByIdOrDefault(channelId)
                               ?? (ChannelModel)this.model.CurrentPms.FirstByIdOrDefault(channelId);

            if (channelModel == null)
            {
                throw new ArgumentOutOfRangeException("channelId", "Cannot navigate to unknown channel");
            }

            channelModel.IsSelected = true;
            this.model.CurrentChannel = channelModel;

            this.Dispatcher.Invoke(
                (Action)delegate
                {
                    foreach (var r in this.region.Regions[ChatWrapperView.ConversationRegion].Views)
                    {
                        var view = r as DisposableView;
                        if (view != null)
                        {
                            var toDispose = view;
                            toDispose.Dispose();
                            this.region.Regions[ChatWrapperView.ConversationRegion].Remove(toDispose);
                        }
                        else
                        {
                            this.region.Regions[ChatWrapperView.ConversationRegion].Remove(r);
                        }
                    }

                    this.region.Regions[ChatWrapperView.ConversationRegion].RequestNavigate(
                        HelperConverter.EscapeSpaces(channelModel.Id));
                });

            this.lastSelected = channelModel;
        }

        #endregion
    }
}