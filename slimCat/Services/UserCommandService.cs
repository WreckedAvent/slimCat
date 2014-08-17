#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageService.cs">
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

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Threading;
    using Utilities;
    using Commands = Utilities.Constants.ClientCommands;

    #endregion

    /// <summary>
    ///     The user command service intercepts all commands the user can enter and responds accordingly.
    /// </summary>
    public partial class UserCommandService : DispatcherObject
    {
        #region Fields

        private readonly IListConnection api;

        private readonly ICharacterManager characterManager;

        private readonly IDictionary<string, CommandHandler> commands;

        private readonly IChatConnection connection;

        private readonly IEventAggregator events;

        private readonly ILoggingService logger;

        private readonly IChatModel model;

        private readonly IChannelService channelService;

        private readonly IFriendRequestService friendRequestService;

        private readonly IRegionManager regionManager;
        #endregion

        #region Constructors and Destructors

        public UserCommandService(
            IEventAggregator events,
            IChatModel model,
            IChatConnection connection,
            IListConnection api,
            ICharacterManager manager,
            ILoggingService logger,
            IChannelService channelService,
            IRegionManager regman,
            IFriendRequestService friendRequestService)
        {

            try
            {
                this.events = events.ThrowIfNull("events");
                this.model = model.ThrowIfNull("model");
                this.connection = connection.ThrowIfNull("connection");
                this.api = api.ThrowIfNull("api");
                this.logger = logger.ThrowIfNull("logger");
                this.channelService = channelService.ThrowIfNull("channelManager");
                regionManager = regman.ThrowIfNull("regman");
                characterManager = manager.ThrowIfNull("characterManager");
                this.friendRequestService = friendRequestService;

                this.events.GetEvent<UserCommandEvent>().Subscribe(CommandReceived, ThreadOption.UIThread, true);

                commands = new Dictionary<string, CommandHandler>
                    {
                        {"bookmark-add", OnBookmarkAddRequested},
                        {"friend-remove", OnFriendRemoveRequested},
                        {"request-accept", OnFriendRequestAcceptRequested},
                        {"request-deny", OnFriendRequestDenyRequested},
                        {"request-send", OnFriendRequestSendRequested},
                        {"request-cancel", OnFriendRequestCancelRequested},
                        {"priv", OnPrivRequested},
                        {Commands.UserMessage, OnPivateMessageSendRequested},
                        {Commands.ChannelMessage, OnMsgRequested},
                        {Commands.ChannelAd, OnLrpRequested},
                        {Commands.UserStatus, OnStatusChangeRequested},
                        {"close", OnCloseRequested},
                        {"forceclose", OnForceChannelCloseRequested},
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
                        {"getdescription", OnChannelDescriptionRequested},
                        {"interesting", OnMarkInterestedRequested},
                        {"notinteresting", OnMarkNotInterestedRequested},
                        {"ignoreUpdates", OnIgnoreUpdatesRequested},
                        {Commands.AdminAlert, OnReportRequested},
                        {"tempignore", OnTemporaryIgnoreRequested},
                        {"tempunignore", OnTemporaryIgnoreRequested},
                        {"tempinteresting", OnTemporaryInterestedRequested},
                        {"tempnotinteresting", OnTemporaryInterestedRequested},
                        {"handlelatest", OnHandleLatestReportRequested},
                        {"handlereport", OnHandleLatestReportByUserRequested},
                        {"bookmark-remove", OnBookmarkRemoveRequested},
                        {"rejoin", OnChannelRejoinRequested },
                        {"searchtag", OnSearchTagToggleRequested},
                        {"logout", OnLogoutRequested}
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

        #region Methods

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

                if (kind != null && kind.Equals(Constants.Arguments.Report))
                {
                    command.Clear();
                    command[Constants.Arguments.Name] = target;
                    OnHandleLatestReportByUserRequested(command);
                }

                var channel = (ChannelModel) model.CurrentPms.FirstByIdOrNull(target)
                              ?? model.CurrentChannels.FirstByIdOrNull(target);

                if (channel != null)
                {
                    events.GetEvent<RequestChangeTabEvent>().Publish(target);
                    Dispatcher.Invoke((Action) NotificationService.ShowWindow);
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
                channelService.JoinChannel(ChannelType.PrivateMessage, newCharacterUpdate.TargetCharacter.Name);

                Dispatcher.Invoke((Action) NotificationService.ShowWindow);
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
                Dispatcher.Invoke((Action) NotificationService.ShowWindow);
                return;
            }

            var chanType = newChannel.Type;
            channelService.JoinChannel(chanType, doStuffWith.TargetChannel.Id);
            Dispatcher.Invoke((Action) NotificationService.ShowWindow);
        }

        private void CommandReceived(IDictionary<string, object> command)
        {
            var type = command.Get(Constants.Arguments.Type);

            if (type == null)
                return;

            try
            {
                var useSlash = type.ToLower().Equals(type);
                Logging.Log((useSlash ? "/" : "") + type, "user cmnd");
                Logging.LogObject(command);
                Logging.Log();

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


        [Conditional("DEBUG")]
        private void Log(string text)
        {
            Logging.LogLine(text, "msg serv");
        }
        #endregion
    }
}