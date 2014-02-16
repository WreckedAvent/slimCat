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

namespace slimCat.Services
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
    using Commands = Utilities.Constants.ClientCommands;
    using Arguments = Utilities.Constants.Arguments;

    #endregion

    /// <summary>
    ///     The message daemon is the service layer responsible for managing what the user sees and the commands the user
    ///     sends.
    /// </summary>
    public class MessageDaemon : DispatcherObject, IChannelManager
    {
        #region Fields

        private readonly IListConnection api;
        private readonly ICharacterManager characterManager;

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

        public MessageDaemon(
            IRegionManager regman,
            IUnityContainer contain,
            IEventAggregator events,
            IChatModel model,
            IChatConnection connection,
            IListConnection api,
            ICharacterManager manager)
        {
            try
            {
                region = regman.ThrowIfNull("regman");
                container = contain.ThrowIfNull("contain");
                this.events = events.ThrowIfNull("events");
                this.model = model.ThrowIfNull("model");
                this.connection = connection.ThrowIfNull("connection");
                this.api = api.ThrowIfNull("api");
                characterManager = manager.ThrowIfNull("characterManager");

                this.model.SelectedChannelChanged += (s, e) => RequestNavigate(this.model.CurrentChannel.Id);

                this.events.GetEvent<ChatOnDisplayEvent>()
                    .Subscribe(BuildHomeChannel, ThreadOption.UIThread, true);
                this.events.GetEvent<RequestChangeTabEvent>()
                    .Subscribe(RequestNavigate, ThreadOption.UIThread, true);
                this.events.GetEvent<UserCommandEvent>().Subscribe(CommandRecieved, ThreadOption.UIThread, true);

                commands = new Dictionary<string, CommandHandler>
                    {
                        {"priv", OnPrivRequested},
                        {Commands.UserMessage, OnPriRequested},
                        {Commands.ChannelMessage, OnMsgRequested},
                        {Commands.ChannelAd, OnLrpRequested},
                        {Commands.UserStatus, OnStatusChangeRequested},
                        {"close", OnCloseRequested},
                        {"join", OnJoinRequested},
                        {Commands.UserIgnore, OnIgnoreRequested},
                        {"clear", OnClearRequested},
                        {"clearall", OnClearAllRequested},
                        {"_logger_open_log", OnOpenLogRequested},
                        {"_logger_open_folder", OnOpenLogFolderRequested},
                        {"code", OnChannelCodeRequested},
                        {"_snap_to_last_update", OnNotificationFocusRequested},
                        {Commands.UserInvite, OnInviteToChannelRequested},
                        {"who", OnWhoInformationRequested},
                        {"getdescription", OnChannelDescripionRequested},
                        {"interesting", OnMarkInterestedRequested},
                        {"notinteresting", OnMarkNotInterestedRequested},
                        {"ignoreUpdates", OnIgnoreUpdatesRequested},
                        {Commands.AdminAlert, OnReportRequested},
                        {"tempignore", OnTemporaryIgnoreRequested},
                        {"tempunignore", OnTemporaryIgnoreRequested},
                        {"tempinteresting", OnTemporaryInterestingRequested},
                        {"tempnotinteresting", OnTemporaryInterestingRequested},
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

        public void AddChannel(ChannelType type, string id, string name)
        {
            if (type == ChannelType.PrivateMessage)
            {
                var character = characterManager.Find(id);

                character.GetAvatar(); // make sure we have their picture

                // model doesn't have a reference to PrivateMessage channels, build it manually
                var temp = new PmChannelModel(character);
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
                    temp = model.AllChannels.FirstOrDefault(param => param.Id == id)
                           ?? new GeneralChannelModel(id, ChannelType.InviteOnly) {Title = name};

                    Dispatcher.Invoke((Action) (() => model.CurrentChannels.Add(temp)));

                    container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", id));
                }

                if (!model.CurrentChannels.Contains(temp))
                    Dispatcher.Invoke((Action) (() => model.CurrentChannels.Add(temp)));
            }
        }

        public void AddMessage(
            string message, string channelName, string poster, MessageType messageType = MessageType.Normal)
        {
            var sender =
                characterManager.Find(poster == Arguments.ThisCharacter ? model.CurrentCharacter.Name : poster);

            var channel = model.CurrentChannels.FirstByIdOrNull(channelName)
                          ?? (ChannelModel) model.CurrentPms.FirstByIdOrNull(channelName);

            if (channel == null)
                return; // exception circumstance, swallow message

            Dispatcher.Invoke(
                (Action) delegate
                    {
                        var thisMessage = new MessageModel(sender, message, messageType);

                        channel.AddMessage(thisMessage, characterManager.IsOfInterest(poster));

                        if (channel.Settings.LoggingEnabled && ApplicationSettings.AllowLogging)
                        {
                            // check if the user wants logging for this channel
                            Logger.LogMessage(channel.Title, channel.Id, thisMessage);
                        }

                        if (poster == Arguments.ThisCharacter)
                            return;

                        // don't push events for our own messages
                        if (channel is GeneralChannelModel)
                        {
                            events.GetEvent<NewMessageEvent>()
                                .Publish(
                                    new Dictionary<string, object>
                                        {
                                            {Arguments.Message, thisMessage},
                                            {Arguments.Channel, channel}
                                        });
                        }
                        else
                            events.GetEvent<NewPmEvent>().Publish(thisMessage);
                    });
        }

        public void JoinChannel(ChannelType type, string id, string name = "")
        {
            IEnumerable<string> history = new List<string>();
            if (!id.Equals("Home"))
                history = Logger.GetLogs(string.IsNullOrWhiteSpace(name) ? id : name, id);

            var toJoin = model.CurrentPms.FirstByIdOrNull(id)
                         ?? (ChannelModel) model.CurrentChannels.FirstByIdOrNull(id);

            if (toJoin == null)
            {
                AddChannel(type, id, name);

                toJoin = model.CurrentPms.FirstByIdOrNull(id)
                         ?? (ChannelModel) model.CurrentChannels.FirstByIdOrNull(id);
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

        public void RemoveChannel(string name)
        {
            RequestNavigate("Home");

            if (model.CurrentChannels.Any(param => param.Id == name))
            {
                var temp = model.CurrentChannels.First(param => param.Id == name);
                temp.Description = null;

                Dispatcher.Invoke(
                    (Action) (() => model.CurrentChannels.Remove(temp)));

                object toSend = new {channel = name};
                connection.SendMessage(toSend, Commands.ChannelLeave);
            }
            else if (model.CurrentPms.Any(param => param.Id == name))
            {
                var temp = model.CurrentPms.First(param => param.Id == name);

                model.CurrentPms.Remove(temp);
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
            var characterName = command.Get(Arguments.Character);
            if (characterName.Equals(model.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
                events.GetEvent<ErrorEvent>().Publish("Hmmm... talking to yourself?");
            else
            {
                var guess = characterManager.SortedCharacters.OrderBy(x => x.Name)
                    .FirstOrDefault(c => c.Name.StartsWith(characterName, true, null));

                JoinChannel(ChannelType.PrivateMessage, guess == null ? characterName : guess.Name);
            }
        }

        private void OnPriRequested(IDictionary<string, object> command)
        {
            AddMessage(command.Get(Arguments.Message), command.Get("recipient"),
                Arguments.ThisCharacter);
            connection.SendMessage(command);
        }

        private void OnMsgRequested(IDictionary<string, object> command)
        {
            AddMessage(command.Get(Arguments.Message), command.Get(Arguments.Channel),
                Arguments.ThisCharacter);
            connection.SendMessage(command);
        }

        private void OnLrpRequested(IDictionary<string, object> command)
        {
            AddMessage(command.Get(Arguments.Message), command.Get(Arguments.Channel),
                Arguments.ThisCharacter,
                MessageType.Ad);
            connection.SendMessage(command);
        }

        private void OnStatusChangeRequested(IDictionary<string, object> command)
        {
            var statusmsg = command.Get(Arguments.StatusMessage);
            var status = command.Get(Arguments.Status).ToEnum<StatusType>();

            model.CurrentCharacter.Status = status;
            model.CurrentCharacter.StatusMessage = statusmsg;
            connection.SendMessage(command);
        }

        private void OnCloseRequested(IDictionary<string, object> command)
        {
            RemoveChannel(command.Get(Arguments.Channel));
        }

        private void OnJoinRequested(IDictionary<string, object> command)
        {
            var args = command.Get(Arguments.Channel);

            if (model.CurrentChannels.FirstByIdOrNull(args) != null)
            {
                RequestNavigate(args);
                return;
            }

            var guess =
                model.AllChannels.OrderBy(channel => channel.Title)
                    .FirstOrDefault(channel => channel.Title.StartsWith(args, true, null));

            var toJoin = guess != null ? guess.Id : args;
            var toSend = new {channel = toJoin};

            connection.SendMessage(toSend, Commands.ChannelJoin);
        }

        private void OnIgnoreRequested(IDictionary<string, object> command)
        {
            var args = command.Get(Arguments.Character);

            var action = command.Get(Arguments.Action);

            if (action == Arguments.ActionAdd)
                characterManager.Add(args, ListKind.Ignored);
            else if (action == Arguments.ActionDelete)
                characterManager.Remove(args, ListKind.Ignored);
            else
                return;

            events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        characterManager.Find(args),
                        new CharacterUpdateModel.ListChangedEventArgs
                            {
                                IsAdded = action == Arguments.ActionAdd,
                                ListArgument = CharacterUpdateModel.ListChangedEventArgs.ListType.Ignored
                            }));

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
            if (command.ContainsKey(Arguments.Channel))
            {
                var toOpen = command.Get(Arguments.Channel);
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
                target = command.Get("target");

            if (command.ContainsKey("kind"))
                kind = command.Get("kind");

            // first off, see if we have a target defined. If we do, then let's see if it's one of our current channels
            if (target != null)
            {
                if (target.StartsWith("http://"))
                {
                    // if our target is a command to get the latest link-able thing, let's grab that
                    Process.Start(target);
                    return;
                }

                if (kind != null && kind.Equals(Arguments.Report))
                {
                    command.Clear();
                    command[Arguments.Name] = target;
                    OnHandleLatestReportByUserRequested(command);
                }

                var channel = (ChannelModel) model.CurrentPms.FirstByIdOrNull(target)
                              ?? model.CurrentChannels.FirstByIdOrNull(target);

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
            var newChannel = model.AllChannels.FirstByIdOrNull(doStuffWith.TargetChannel.Id);

            if (newChannel == null)
            {
                // if it's null, then we've got an invite to a new channel
                var toSend = new {channel = doStuffWith.TargetChannel.Id};
                connection.SendMessage(toSend, Commands.ChannelJoin);
                Dispatcher.Invoke((Action) NotificationsDaemon.ShowWindow);
                return;
            }

            var chanType = newChannel.Type;
            JoinChannel(chanType, doStuffWith.TargetChannel.Id);
            Dispatcher.Invoke((Action) NotificationsDaemon.ShowWindow);
        }

        private void OnInviteToChannelRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey(Arguments.Character) && command.Get(Arguments.Character).Equals(
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
            var args = command.Get(Arguments.Character);
            var isAdd = !characterManager.IsOnList(args, ListKind.Interested);
            if (isAdd)
                characterManager.Add(args, ListKind.Interested);
            else
                characterManager.Remove(args, ListKind.Interested);

            events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        characterManager.Find(args),
                        new CharacterUpdateModel.ListChangedEventArgs
                            {
                                IsAdded = isAdd,
                                ListArgument =
                                    CharacterUpdateModel.ListChangedEventArgs.ListType.Interested
                            }));
        }

        private void OnMarkNotInterestedRequested(IDictionary<string, object> command)
        {
            var args = command.Get(Arguments.Character);

            var isAdd = !characterManager.IsOnList(args, ListKind.NotInterested);
            if (isAdd)
                characterManager.Add(args, ListKind.NotInterested);
            else
                characterManager.Remove(args, ListKind.NotInterested);

            events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        characterManager.Find(args),
                        new CharacterUpdateModel.ListChangedEventArgs
                            {
                                IsAdded = isAdd,
                                ListArgument =
                                    CharacterUpdateModel.ListChangedEventArgs.ListType.NotInterested
                            }));
        }

        private void OnIgnoreUpdatesRequested(IDictionary<string, object> command)
        {
            var args = command.Get(Arguments.Character);

            var isAdd = !characterManager.IsOnList(args, ListKind.IgnoreUpdates);
            if (isAdd)
                characterManager.Add(args, ListKind.IgnoreUpdates);
            else
                characterManager.Remove(args, ListKind.IgnoreUpdates);
        }

        private void OnReportRequested(IDictionary<string, object> command)
        {
            if (!command.ContainsKey(Arguments.Report))
                command.Add(Arguments.Report, string.Empty);

            var logId = -1; // no log

            // report format: "Current Tab/Channel: <channel> | Reporting User: <reported user> | <report body>
            var reportText = string.Format(
                "Current Tab/Channel: {0} | Reporting User: {1} | {2}",
                command.Get(Arguments.Channel),
                command.Get(Arguments.Name),
                command.Get(Arguments.Report));

            // upload log
            var channelText = command.Get(Arguments.Channel);
            if (!string.IsNullOrWhiteSpace(channelText) && !channelText.Equals("None"))
            {
                // we could just use _model.SelectedChannel, but the user might change tabs immediately after reporting, creating a race condition
                ChannelModel channel;
                if (channelText == command.Get(Arguments.Name))
                    channel = model.CurrentPms.FirstByIdOrNull(channelText);
                else
                    channel = model.CurrentChannels.FirstByIdOrNull(channelText);

                if (channel != null)
                {
                    var report = new ReportModel
                        {
                            Reporter = model.CurrentCharacter,
                            Reported = command.Get(Arguments.Name),
                            Complaint = command.Get(Arguments.Report),
                            Tab = channelText
                        };

                    logId = api.UploadLog(report, channel.Messages);
                }
            }

            command.Remove(Arguments.Name);
            command[Arguments.Report] = reportText;
            command[Arguments.LogId] = logId;

            if (!command.ContainsKey(Arguments.Action))
                command[Arguments.Action] = Arguments.ActionReport;

            connection.SendMessage(command);
        }

        private void OnTemporaryIgnoreRequested(IDictionary<string, object> command)
        {
            var character = command.Get(Arguments.Character).ToLower().Trim();
            var add = command.Get(Arguments.Type) == "tempignore";

            if (add)
                characterManager.Add(character, ListKind.Ignored, true);
            else
                characterManager.Remove(character, ListKind.Ignored, true);
        }

        private void OnTemporaryInterestingRequested(IDictionary<string, object> command)
        {
            var character = command.Get(Arguments.Character).ToLower().Trim();
            var add = command.Get(Arguments.Type) == "tempinteresting";

            if (add)
                characterManager.Add(character, ListKind.Interested, true);
            else
                characterManager.Add(character, ListKind.NotInterested, true);
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

            command.Add(Arguments.Type, Commands.AdminAlert);
            if (args != null) command.Add(Arguments.CallId, args.CallId);
            command.Add(Arguments.Action, Arguments.ActionConfirm);

            JoinChannel(ChannelType.PrivateMessage, latest.TargetCharacter.Name);

            var logId = -1;
            if (command.ContainsKey(Arguments.LogId))
                int.TryParse(command.Get(Arguments.LogId), out logId);

            if (logId != -1)
                Process.Start(Constants.UrlConstants.ReadLog + logId);

            connection.SendMessage(command);
        }

        private void OnHandleLatestReportByUserRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey(Arguments.Name))
            {
                var target = characterManager.Find(command.Get(Arguments.Name));

                if (!target.HasReport)
                {
                    events.GetEvent<ErrorEvent>()
                        .Publish("Cannot find report for specified character!");
                    return;
                }

                command[Arguments.Type] = Commands.AdminAlert;
                command.Add(Arguments.CallId, target.LastReport.CallId);
                if (!command.ContainsKey(Arguments.Action))
                    command[Arguments.Action] = Arguments.ActionConfirm;

                JoinChannel(ChannelType.PrivateMessage, target.Name);

                var logId = -1;
                if (command.ContainsKey(Arguments.LogId))
                    int.TryParse(command.Get(Arguments.LogId), out logId);

                if (logId != -1)
                    Process.Start(Constants.UrlConstants.ReadLog + logId);

                connection.SendMessage(command);
            }

            OnHandleLatestReportRequested(command);
        }

        private void CommandRecieved(IDictionary<string, object> command)
        {
            var type = command.Get(Arguments.Type);

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
                            var toUpdate = model.CurrentChannels.FirstByIdOrNull(lastSelected.Id)
                                           ??
                                           (ChannelModel) model.CurrentPms.FirstByIdOrNull(lastSelected.Id);

                            if (toUpdate == null)
                                lastSelected = null;
                            else 
                                toUpdate.IsSelected = false;
                        });
            }

            var channelModel = model.CurrentChannels.FirstByIdOrNull(channelId)
                               ?? (ChannelModel) model.CurrentPms.FirstByIdOrNull(channelId);

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