#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserCommandService.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

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
    using Commands = Utilities.Constants.ClientCommands;

    #endregion

    /// <summary>
    ///     The user command service intercepts all commands the user can enter and responds accordingly.
    /// </summary>
    public partial class UserCommandService : DispatcherObject
    {
        #region Fields

        private readonly IListConnection api;
        private readonly IChannelService channelService;

        private readonly ICharacterManager characterManager;

        private readonly IDictionary<string, CommandHandler> commands;

        private readonly IChatConnection connection;

        private readonly IEventAggregator events;
        private readonly IIconService iconService;

        private readonly ILoggingService logger;

        private readonly IChatModel model;

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
            IFriendRequestService friendRequestService,
            IIconService iconService)
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
                this.iconService = iconService;

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
                    {"rejoin", OnChannelRejoinRequested},
                    {"searchtag", OnSearchTagToggleRequested},
                    {"logout", OnLogoutRequested},
                    {"soundon", OnSoundOnRequested},
                    {"soundoff", OnSoundOffRequested}
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