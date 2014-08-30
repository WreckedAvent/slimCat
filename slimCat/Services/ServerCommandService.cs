#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandService.cs">
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

    using System.IO;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using System.Windows;
    using Utilities;
    using ViewModels;
    using Commands = Utilities.Constants.ServerCommands;

    #endregion

    /// <summary>
    ///     The server command service intercepts commands from the server and responds accordingly.
    /// </summary>
    public partial class ServerCommandService : ViewModelBase
    {
        #region Fields

        private readonly IAutomationService automation;

        private readonly object locker = new object();

        private readonly IChannelService manager;

        private readonly INoteService notes;

        private readonly IFriendRequestService friendRequestService;

        private readonly string[] noisyTypes;

        private readonly Queue<IDictionary<string, object>> que = new Queue<IDictionary<string, object>>();

        private readonly HashSet<string> autoJoinedChannels = new HashSet<string>();

        #endregion

        #region Constructors and Destructors

        public ServerCommandService(IChatState chatState,
            IAutomationService automation,
            INoteService notes,
            IChannelService manager,
            IFriendRequestService friendRequestService)
            : base(chatState)
        {
            this.manager = manager;
            this.automation = automation;
            this.notes = notes;
            this.friendRequestService = friendRequestService;

            Events.GetEvent<CharacterSelectedLoginEvent>()
                .Subscribe(GetCharacter, ThreadOption.BackgroundThread, true);
            Events.GetEvent<ChatCommandEvent>().Subscribe(EnqueueAction, ThreadOption.BackgroundThread, true);
            Events.GetEvent<ConnectionClosedEvent>().Subscribe(WipeState, ThreadOption.PublisherThread, true);

            ChatModel.CurrentAccount = ChatConnection.Account;

            noisyTypes = new[]
                {
                    Commands.UserJoin,
                    Commands.UserLeave,
                    Commands.UserStatus,
                    Commands.PublicChannelList,
                    Commands.PrivateChannelList,
                    Commands.UserList,
                    Commands.ChannelAd,
                    Commands.ChannelMessage
                };

            LoggingSection = "cmnd serv";
        }

        #endregion

        #region Delegates

        /// <summary>
        ///     Represents a function which can action on a server command.
        /// </summary>
        private delegate void CommandHandlerDelegate(IDictionary<string, object> command);

        #endregion

        #region Public Methods and Operators
        public override void Initialize()
        {
        }

        #endregion

        #region Methods
        private static Gender ParseGender(string input)
        {
            switch (input)
            {
                    // manually determine some really annoyingly-named genders
                case "Male-Herm":
                    return Gender.HermM;

                case "Herm":
                    return Gender.HermF;

                case "Cunt-boy":
                    return Gender.Cuntboy;

                default: // every other gender is parsed normally
                    return input.ToEnum<Gender>();
            }
        }

        private GeneralChannelModel FindChannel(string id)
        {
            return ChatModel.CurrentChannels.FirstByIdOrNull(id) 
                ?? ChatModel.AllChannels.FirstByIdOrNull(id);
        }

        private GeneralChannelModel FindChannel(IDictionary<string, object> command)
        {
            var channelId = command.Get(Constants.Arguments.Channel);
            return FindChannel(channelId);
        }

        private void DoAction()
        {
            lock (locker)
            {
                if (que.Count <= 0)
                    return;

                var workingData = que.Dequeue();

                Invoke(workingData);
                DoAction();
            }
        }

        private void EnqueueAction(IDictionary<string, object> data)
        {
            if (data == null) return;

            if (autoJoinedChannels.Count != 0 && data.Get(Constants.Arguments.Command) == Commands.ChannelJoin)
            {
                AutoJoinChannelCommand(data);
                return;
            }
            if (data.Get(Constants.Arguments.Command) == Commands.ChannelJoin)
            {
                var characterDict = data.Get<IDictionary<string, object>>(Constants.Arguments.Character);
                var character = characterDict.Get(Constants.Arguments.Identity);

                if (character == ChatModel.CurrentCharacter.Name)
                {
                    QuickJoinChannelCommand(data);
                    return;
                }
            }

            lock (locker)
            {
                que.Enqueue(data);
            }
            DoAction();
        }

        private void Invoke(IDictionary<string, object> command)
        {
            var toInvoke = InterpretCommand(command);
            if (toInvoke != null)
                toInvoke.Invoke(command);
        }

        private CommandHandlerDelegate InterpretCommand(IDictionary<string, object> command)
        {
            ChatModel.LastMessageReceived = DateTimeOffset.Now;

            if (command == null) return null;

            var commandType = command.Get(Constants.Arguments.Command);

            Log(commandType + " " + command.GetHashCode(), noisyTypes.Contains(commandType));

            switch (commandType)
            {
                case Commands.SystemAuthenticate:
                    return LoginCommand;
                case Commands.SystemUptime:
                    return UptimeCommand;
                case Commands.AdminList:
                    return AdminsListCommand;
                case Commands.UserIgnore:
                    return IgnoreUserCommand;
                case Commands.UserList:
                    return InitialCharacterListCommand;
                case Commands.PublicChannelList:
                    return PublicChannelListCommand;
                case Commands.PrivateChannelList:
                    return PrivateChannelListCommand;
                case Commands.UserStatus:
                    return StatusChangedCommand;
                case Commands.ChannelAd:
                    return AdMessageCommand;
                case Commands.ChannelMessage:
                    return ChannelMessageCommand;
                case Commands.UserMessage:
                    return PrivateMessageCommand;
                case Commands.UserTyping:
                    return TypingStatusCommand;
                case Commands.ChannelJoin:
                    return JoinChannelCommand;
                case Commands.ChannelLeave:
                    return LeaveChannelCommand;
                case Commands.ChannelModerators:
                    return ChannelOperatorListCommand;
                case Commands.ChannelInitialize:
                    return ChannelInitializedCommand;
                case Commands.ChannelDescription:
                    return ChannelDescriptionCommand;
                case Commands.SystemError:
                case Commands.SystemMessage:
                    return ErrorCommand;
                case Commands.UserInvite:
                    return InviteCommand;
                case Commands.ChannelKick:
                case Commands.ChannelBan:
                    return KickCommand;
                case Commands.UserJoin:
                    return UserLoggedInCommand;
                case Commands.UserLeave:
                    return CharacterDisconnectCommand;
                case Commands.ChannelRoll:
                    return RollCommand;
                case Commands.AdminDemote:
                    return OperatorDemoteCommand;
                case Commands.AdminPromote:
                    return OperatorPromoteCommand;
                case Commands.ChannelDemote:
                    return OperatorDemoteCommand;
                case Commands.ChannelPromote:
                    return OperatorPromoteCommand;
                case Commands.ChannelMode:
                    return RoomModeChangedCommand;
                case Commands.AdminBroadcast:
                    return BroadcastCommand;
                case Commands.SystemBridge:
                    return RealTimeBridgeCommand;
                case Commands.AdminReport:
                    return NewReportCommand;
                case Commands.SearchResult:
                    return SearchResultCommand;
                case Commands.ChannelSetOwner:
                    return SetNewOwnerCommand;
                default:
                    return null;
            }
        }

        private void LoginCommand(IDictionary<string, object> command)
        {
            ChatModel.ClientUptime = DateTimeOffset.Now;
            ChatConnection.SendMessage(Constants.ClientCommands.SystemUptime);

            Dispatcher.Invoke((Action) delegate { ChatModel.IsAuthenticated = true; });

            const string nojoinName = "nojoin";
            if ((!File.Exists(nojoinName) || ApplicationSettings.SavedChannels.Count == 0) && ApplicationSettings.SlimCatChannelId != null)
            {
                ApplicationSettings.SavedChannels.Add(ApplicationSettings.SlimCatChannelId);
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                File.Create(nojoinName);
            }

            // auto join
            var waitTimer = new Timer(200);
            var channels = (from c in ApplicationSettings.SavedChannels
                           where !string.IsNullOrWhiteSpace(c)
                           select new { channel = c })
                           .Distinct()
                           .ToList();

            var walk = channels.GetEnumerator();

            if (walk.MoveNext())
            {
                waitTimer.Elapsed += (s, e) =>
                    {
                        Log("Auto joining " + walk.Current);
                        autoJoinedChannels.Add(walk.Current.channel);
                        ChatConnection.SendMessage(walk.Current, Constants.ClientCommands.ChannelJoin);
                        if (walk.MoveNext())
                            return;

                        waitTimer.Stop();
                        waitTimer.Dispose();
                    };
            }

            waitTimer.Start();
        }

        private void UptimeCommand(IDictionary<string, object> command)
        {
            var time = (long) command["starttime"];
            ChatModel.ServerUpTime = HelperConverter.UnixTimeToDateTime(time);
        }

        private void GetCharacter(string character)
        {
            ChatModel.CurrentCharacter = new CharacterModel {Name = character, Status = StatusType.Online};
            ChatModel.CurrentCharacter.GetAvatar();

            Dispatcher.Invoke(
                (Action)
                    delegate
                        {
                            Application.Current.MainWindow.Title = string.Format(
                                "{0} {1} ({2})", Constants.ClientId, Constants.ClientName, character);
                        });
        }

        private void WipeState(string message)
        {
            Log("Resetting");

            CharacterManager.Clear();
            ChatModel.CurrentChannels.Each(x => x.CharacterManager.Clear());

            ChatModel.CurrentCharacter.Status = StatusType.Online;
            ChatModel.CurrentCharacter.StatusMessage = string.Empty;

            Dispatcher.Invoke((Action) (() =>
                {
                    ChatModel.AllChannels.Clear();
                    while (ChatModel.CurrentChannels.Count > 1)
                    {
                        ChatModel.CurrentChannels.RemoveAt(1);
                    }

                    ChatModel.CurrentPms.Each(pm => pm.TypingStatus = TypingStatus.Clear);
                }));
        }

        private void RequeueCommand(IDictionary<string, object> command)
        {
            object value;
            if (!command.TryGetValue("retryAttempt", out value))
                value = 0;

            var retryAttempts = (int) value;
            Logging.LogLine(command.Get(Constants.Arguments.Command) 
                + " " + command.GetHashCode() 
                + " fail #" + (retryAttempts + 1), "cmnd serv");

            if (retryAttempts >= 5) return;

            retryAttempts++;
            command["retryAttempt"] = retryAttempts;

            var delay = new Timer(2000 ^ retryAttempts);
            delay.Elapsed += (s, e) =>
            {
                EnqueueAction(command);
                delay.Stop();
                delay.Dispose();
            };
            delay.Start();
        }

        #endregion
    }
}