#region Copyright

// <copyright file="ServerCommandService.cs">
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
    using System.IO;
    using System.Linq;
    using System.Timers;
    using System.Windows;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Utilities;
    using ViewModels;
    using static Utilities.Constants.ServerCommands;
    using static Utilities.Constants.Arguments;

    #endregion

    /// <summary>
    ///     The server command service intercepts commands from the server and responds accordingly.
    /// </summary>
    public partial class ServerCommandService : ViewModelBase
    {
        #region Constructors and Destructors

        public ServerCommandService(IChatState chatState,
            IAutomateThings automation,
            IManageNotes notes,
            IManageChannels channels,
            IFriendRequestService friendRequestService)
            : base(chatState)
        {
            this.channels = channels;
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
                UserJoin,
                UserLeave,
                UserStatus,
                PublicChannelList,
                PrivateChannelList,
                UserList,
                ChannelAd,
                ChannelMessage
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

        #region Fields

        private readonly HashSet<string> autoJoinedChannels = new HashSet<string>();

        private readonly IAutomateThings automation;

        private readonly IFriendRequestService friendRequestService;

        private readonly object queueLocker = new object();

        private readonly object chatStateLocker = new object();

        private readonly IManageChannels channels;

        private readonly string[] noisyTypes;

        private readonly IManageNotes notes;

        private readonly Queue<IDictionary<string, object>> que = new Queue<IDictionary<string, object>>();

        private IList<string> rejoinChannelList = new List<string>();

        #endregion

        #region Methods

        private GeneralChannelModel FindChannel(string id)
        {
            return ChatModel.CurrentChannels.FirstByIdOrNull(id)
                   ?? ChatModel.AllChannels.FirstByIdOrNull(id);
        }

        private GeneralChannelModel FindChannel(IDictionary<string, object> command)
        {
            var channelId = command.Get(Channel);
            return FindChannel(channelId);
        }

        private void DoAction()
        {
            lock (queueLocker)
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

            var isChannelJoin = data.Get(Command) == ChannelJoin;
            if (autoJoinedChannels.Count != 0 && isChannelJoin)
            {
                AutoJoinChannelCommand(data);
                return;
            }

            if (isChannelJoin)
            {
                var characterDict = data.Get<IDictionary<string, object>>(Character);
                var character = characterDict.Get(Identity);

                if (character == ChatModel.CurrentCharacter.Name)
                {
                    QuickJoinChannelCommand(data);
                    return;
                }
            }

            lock (queueLocker)
            {
                que.Enqueue(data);
            }
            DoAction();
        }

        private void Invoke(IDictionary<string, object> command)
        {
            var toInvoke = InterpretCommand(command);
            toInvoke?.Invoke(command);
        }

        private CommandHandlerDelegate InterpretCommand(IDictionary<string, object> command)
        {
            ChatModel.LastMessageReceived = DateTimeOffset.Now;

            if (command == null) return null;

            var commandType = command.Get(Command);

            Log(commandType + " " + command.GetHashCode(), noisyTypes.Contains(commandType));

            switch (commandType)
            {
                case SystemAuthenticate:
                    return LoginCommand;
                case SystemUptime:
                    return UptimeCommand;
                case AdminList:
                    return AdminsListCommand;
                case UserIgnore:
                    return IgnoreUserCommand;
                case UserList:
                    return InitialCharacterListCommand;
                case PublicChannelList:
                    return PublicChannelListCommand;
                case PrivateChannelList:
                    return PrivateChannelListCommand;
                case UserStatus:
                    return StatusChangedCommand;
                case ChannelAd:
                    return AdMessageCommand;
                case ChannelMessage:
                    return ChannelMessageCommand;
                case UserMessage:
                    return PrivateMessageCommand;
                case UserTyping:
                    return TypingStatusCommand;
                case ChannelJoin:
                    return JoinChannelCommand;
                case ChannelLeave:
                    return LeaveChannelCommand;
                case ChannelModerators:
                    return ChannelOperatorListCommand;
                case ChannelInitialize:
                    return ChannelInitializedCommand;
                case ChannelDescription:
                    return ChannelDescriptionCommand;
                case SystemMessage:
                case SystemError:
                    return ErrorCommand;
                case UserInvite:
                    return InviteCommand;
                case ChannelKick:
                case ChannelBan:
                    return KickCommand;
                case UserJoin:
                    return UserLoggedInCommand;
                case UserLeave:
                    return CharacterDisconnectCommand;
                case ChannelRoll:
                    return RollCommand;
                case AdminDemote:
                    return OperatorDemoteCommand;
                case AdminPromote:
                    return OperatorPromoteCommand;
                case ChannelDemote:
                    return OperatorDemoteCommand;
                case ChannelPromote:
                    return OperatorPromoteCommand;
                case Constants.ServerCommands.ChannelMode:
                    return RoomModeChangedCommand;
                case AdminBroadcast:
                    return BroadcastCommand;
                case SystemBridge:
                    return RealTimeBridgeCommand;
                case AdminReport:
                    return NewReportCommand;
                case SearchResult:
                    return SearchResultCommand;
                case ChannelSetOwner:
                    return SetNewOwnerCommand;
                default:
                    return null;
            }
        }

        private void LoginCommand(IDictionary<string, object> command)
        {
            ChatModel.ClientUptime = DateTimeOffset.Now;
            ChatConnection.SendMessage(Constants.ClientCommands.SystemUptime);

            Dispatcher.Invoke(() => ChatModel.IsAuthenticated = true);

            const string nojoinName = "nojoin";
            if ((!File.Exists(nojoinName) || ApplicationSettings.SavedChannels.Count == 0) &&
                ApplicationSettings.SlimCatChannelId != null)
            {
                ApplicationSettings.SavedChannels.Add(ApplicationSettings.SlimCatChannelId);
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                File.Create(nojoinName);
            }

            // auto join
            var waitTimer = new Timer(200);
            var channelsId =
                rejoinChannelList.Count == 0 ? ApplicationSettings.SavedChannels : rejoinChannelList;

            var channels = channelsId
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Select(x => new {channel = x});

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

            var currentCharacter = ChatModel.CurrentCharacter;
            if (currentCharacter.Status != StatusType.Online
                || !string.IsNullOrWhiteSpace(currentCharacter.StatusMessage))
            {
                Events.SendUserCommand("status",
                    new[] {currentCharacter.Status.ToString(), currentCharacter.StatusMessage});
            }

            waitTimer.Start();
        }

        private void UptimeCommand(IDictionary<string, object> command)
        {
            var time = (long) command["starttime"];
            ChatModel.ServerUpTime = time.UnixTimeToDateTime();
        }

        private void GetCharacter(string character)
        {
            ChatModel.CurrentCharacter = new CharacterModel {Name = character, Status = StatusType.Online};
            ChatModel.CurrentCharacter.GetAvatar();

            Dispatcher.Invoke(() =>
                Application.Current.MainWindow.Title = $"{Constants.ClientId} {Constants.ClientNickname} ({character})"
                );
        }

        private void WipeState(bool intentionalDisconnect)
        {
            Log("Resetting");

            CharacterManager.Clear();
            ChatModel.CurrentChannels.Each(x => x.CharacterManager.Clear());

            if (intentionalDisconnect)
            {
                ChatModel.CurrentCharacter.Status = StatusType.Online;
                ChatModel.CurrentCharacter.StatusMessage = string.Empty;
                RequestChannelJoinEvent(ChatModel.CurrentChannels.FirstByIdOrNull("Home").Id);
            }

            Dispatcher.Invoke(() =>
            {
                ChatModel.CurrentPms.Each(pm => pm.TypingStatus = TypingStatus.Clear);
                rejoinChannelList.Clear();

                if (intentionalDisconnect)
                {
                    ChatModel.AllChannels.Clear();
                    ChatModel.CurrentChannels
                        .Where(x => x.Id != "Home")
                        .ToList()
                        .Each(x =>
                        {
                            ChatModel.CurrentChannels.Remove(x);
                            x.Dispose();
                        });

                    ChatModel.CurrentPms.ToList().Each(x =>
                    {
                        ChatModel.CurrentPms.Remove(x);
                        x.Dispose();
                    });
                }
                else
                {
                    rejoinChannelList = ChatModel.CurrentChannels
                        .Where(x => x.Id != "Home")
                        .Select(x => x.Id)
                        .ToList();
                }

                ChatModel.IsAuthenticated = false;
            });
        }

        private void RequeueCommand(IDictionary<string, object> command)
        {
            object value;
            if (!command.TryGetValue("retryAttempt", out value))
                value = 0;

            var retryAttempts = (int) value;
            Logging.LogLine(command.Get(Command)
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