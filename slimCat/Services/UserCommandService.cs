#region Copyright

// <copyright file="UserCommandService.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
//
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
//
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>

#endregion

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Threading;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Models;
    using Utilities;
    using static Utilities.Constants.ClientCommands;

    #endregion

    /// <summary>
    ///     The user command service intercepts all commands the user can enter and responds accordingly.
    /// </summary>
    public partial class UserCommandService : DispatcherObject
    {
        #region Constructors and Destructors

        public UserCommandService(
            IChatState chatState,
            IHandleApi api,
            ILogThings logger,
            IManageChannels channels,
            IHandleIcons iconService)
        {
            try
            {
                regionManager = chatState.RegionManager;
                events = chatState.EventAggregator;
                cm = chatState.ChatModel;
                connection = chatState.Connection;
                characterManager = chatState.CharacterManager;
                this.api = api.ThrowIfNull("api");
                this.logger = logger.ThrowIfNull("logger");
                this.channels = channels.ThrowIfNull("channelManager");
                this.iconService = iconService;

                events.GetEvent<UserCommandEvent>().Subscribe(CommandReceived, ThreadOption.UIThread, true);

                commands = new Dictionary<string, CommandHandler>
                {
                    {"bookmark-add", OnBookmarkAddRequested},
                    {"friend-remove", OnFriendRemoveRequested},
                    {"request-accept", OnFriendRequestAcceptRequested},
                    {"request-deny", OnFriendRequestDenyRequested},
                    {"request-send", OnFriendRequestSendRequested},
                    {"request-cancel", OnFriendRequestCancelRequested},
                    {"priv", OnPrivRequested},
                    {UserMessage, OnPivateMessageSendRequested},
                    {ChannelMessage, OnMsgRequested},
                    {ChannelAd, OnLrpRequested},
                    {UserStatus, OnStatusChangeRequested},
                    {"close", OnCloseRequested},
                    {"forceclose", OnForceChannelCloseRequested},
                    {"join", OnJoinRequested},
                    {UserIgnore, OnIgnoreRequested},
                    {"clear", OnClearRequested},
                    {"clearall", OnClearAllRequested},
                    {"_logger_open_log", OnOpenLogRequested},
                    {"_logger_open_folder", OnOpenLogFolderRequested},
                    {"code", OnChannelCodeRequested},
                    {UserInvite, OnInviteToChannelRequested},
                    {"who", OnWhoInformationRequested},
                    {"getdescription", OnChannelDescriptionRequested},
                    {"interesting", OnMarkInterestedRequested},
                    {"notinteresting", OnMarkNotInterestedRequested},
                    {"ignoreUpdates", OnIgnoreUpdatesRequested},
                    {AdminAlert, OnReportRequested},
                    {"tempignore", OnTemporaryIgnoreRequested},
                    {"tempunignore", OnTemporaryIgnoreRequested},
                    {"tempinteresting", OnTemporaryInterestedRequested},
                    {"tempnotinteresting", OnTemporaryInterestedRequested},
                    {"handlelatest", OnHandleLatestReportRequested},
                    {"handlereport", OnHandleLatestReportByUserRequested},
                    {"bookmark-remove", OnBookmarkRemoveRequested},
                    {"rejoin", OnChannelRejoinRequested},
                    {"searchtag", OnSearchTagToggleRequested},
                    {"logout", OnLogoutRequested},
                    {"soundon", OnSoundOnRequested},
                    {"soundoff", OnSoundOffRequested},
                    {"whois", OnWhoIsRequested},
                    {ChannelRoll, OnRollRequested}
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

        #region Fields

        private readonly IHandleApi api;
        private readonly IManageChannels channels;

        private readonly ICharacterManager characterManager;

        private readonly IDictionary<string, CommandHandler> commands;

        private readonly IHandleChatConnection connection;

        private readonly IEventAggregator events;
        private readonly IHandleIcons iconService;

        private readonly ILogThings logger;

        private readonly IChatModel cm;

        private readonly IRegionManager regionManager;

        #endregion

        #region Methods

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
        private void Log(string text) => Logging.LogLine(text, "msg serv");

        #endregion
    }
}