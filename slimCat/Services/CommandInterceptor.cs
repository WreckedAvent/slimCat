#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandInterceptor.cs">
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
    using System.Linq;
    using System.Timers;
    using System.Web;
    using System.Windows;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using SimpleJson;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     This interprets the commands and translates them to methods that our various other services can use.
    ///     It also coordinates them to prevent collisions.
    ///     This intercepts just about every single command that the server sends.
    /// </summary>
    internal class CommandInterceptor : ViewModelBase
    {
        #region Fields

        private readonly IChatConnection connection;

        private readonly IChannelManager manager;

        private readonly IList<IDictionary<string, object>> que = new List<IDictionary<string, object>>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandInterceptor" /> class.
        /// </summary>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="conn">
        ///     The conn.
        /// </param>
        /// <param name="manager">
        ///     The manager.
        /// </param>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="eventagg">
        ///     The eventagg.
        /// </param>
        public CommandInterceptor(
            IChatModel cm,
            IChatConnection conn,
            IChannelManager manager,
            IUnityContainer contain,
            IRegionManager regman,
            IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            connection = conn;
            this.manager = manager;

            Events.GetEvent<CharacterSelectedLoginEvent>()
                .Subscribe(GetCharacter, ThreadOption.BackgroundThread, true);
            Events.GetEvent<ChatCommandEvent>().Subscribe(EnqueAction, ThreadOption.BackgroundThread, true);
            Events.GetEvent<ConnectionClosedEvent>().Subscribe(WipeState, ThreadOption.PublisherThread, true);

            ChatModel.CurrentAccount = connection.Account;
        }

        #endregion

        #region Delegates

        /// <summary>
        ///     The command delegate.
        /// </summary>
        /// <param name="command">
        ///     The command.
        /// </param>
        private delegate void CommandDelegate(IDictionary<string, object> command);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
        }

        #endregion

        #region Methods

        private static void AddToSomeListCommand(
            IDictionary<string, object> command, string paramaterToPullFrom, ICollection<string> listToAddTo)
        {
            if (!(command[paramaterToPullFrom] is string))
            {
                // ensure that our arguments are actually an array
                var arr = (JsonArray) command[paramaterToPullFrom];
                foreach (
                    var character in
                        from string character in arr
                        where !string.IsNullOrWhiteSpace(character)
                        where !listToAddTo.Contains(character)
                        select character)
                    listToAddTo.Add(character);
            }
            else
            {
                var toAdd = command[paramaterToPullFrom] as string;
                if (!listToAddTo.Contains(toAdd))
                {
                    // IGN crash fix
                    listToAddTo.Add(toAdd);
                }
            }
        }

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
                    return (Gender) Enum.Parse(typeof (Gender), input, true);
            }
        }

        private void AdMessageCommand(IDictionary<string, object> command)
        {
            MessageRecieved(command, true);
        }

        private void AdminsCommand(IDictionary<string, object> command)
        {
            AddToSomeListCommand(command, "ops", ChatModel.Mods);
            if (ChatModel.Mods.Contains(ChatModel.CurrentCharacter.Name))
                Dispatcher.Invoke((Action) delegate { ChatModel.IsGlobalModerator = true; });
        }

        private void BroadcastCommand(IDictionary<string, object> command)
        {
            var message = command["message"] as string;
            var posterName = command["character"] as string;
            var poster = ChatModel.FindCharacter(posterName);

            Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(poster, new CharacterUpdateModel.BroadcastEventArgs {Message = message}));
        }

        private void ChannelBanListCommand(IDictionary<string, object> command)
        {
            var channelId = (string) command["channel"];
            var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentChannels, channelId);

            if (channel == null)
                return;

            channel.Banned.Clear();

            var message = ((string) command["message"]).Split(':');
            var banned = message[1].Trim();

            if (banned.IndexOf(',') == -1)
                channel.Banned.Add(banned);
            else
            {
                var bannedList = banned.Split(',');
                foreach (var ban in bannedList)
                    channel.Banned.Add(ban.Trim());
            }

            Events.GetEvent<NewUpdateEvent>()
                .Publish(new ChannelUpdateModel(channel, new ChannelUpdateModel.ChannelTypeBannedListEventArgs()));
        }

        private void ChannelDesciptionCommand(IDictionary<string, object> command)
        {
            var channelName = command["channel"];
            var channel = Enumerable.First(ChatModel.CurrentChannels, x => x.Id == channelName as string);
            var description = command["description"];

            var isInitializer = string.IsNullOrWhiteSpace(channel.Description);

            if (!isInitializer)
                channel.Description = description as string;
            else if (description.ToString().StartsWith("Welcome to your private room!"))
            {
                // shhh go away lame init description
                channel.Description =
                    "Man this description is lame. You should change it and make it amaaaaaazing. Click that pencil, man.";
            }
            else
                channel.Description = description as string; // derpherp no channel description bug fix

            if (isInitializer)
                return;

            var args = new ChannelUpdateModel.ChannelDescriptionChangedEventArgs();
            Events.GetEvent<NewUpdateEvent>().Publish(new ChannelUpdateModel(channel, args));
        }

        private void ChannelInitializedCommand(IDictionary<string, object> command)
        {
            var channelName = (string) command["channel"];
            var mode = (ChannelMode) Enum.Parse(typeof (ChannelMode), (string) command["mode"], true);
            var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentChannels, channelName);

            channel.Mode = mode;
            dynamic users = command["users"]; // dynamic lets us deal with odd syntax
            foreach (IDictionary<string, object> character in users)
            {
                var name = character["identity"] as string;

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (
                    !channel.Users.Any(
                        x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && ChatModel.IsOnline(name)))
                    channel.Users.Add(ChatModel.FindCharacter(name));
            }
        }

        private void ChannelListCommand(IDictionary<string, object> command, bool isPublic)
        {
            dynamic arr = command["channels"];
            lock (ChatModel.AllChannels)
            {
                foreach (IDictionary<string, object> channel in arr)
                {
                    var name = channel["name"] as string;
                    string title = null;
                    if (!isPublic)
                        title = HttpUtility.HtmlDecode(channel["title"] as string);

                    var mode = ChannelMode.Both;
                    if (isPublic)
                        mode = (ChannelMode) Enum.Parse(typeof (ChannelMode), (string) channel["mode"], true);

                    var number = (long) channel["characters"];
                    if (number < 0)
                        number = 0;

                    Dispatcher.Invoke(
                        (Action)
                            (() =>
                                ChatModel.AllChannels.Add(
                                    new GeneralChannelModel(
                                        name, isPublic ? ChannelType.Public : ChannelType.Private, (int) number, mode)
                                        {
                                            Title =
                                                isPublic
                                                    ? name
                                                    : title
                                        })));
                }
            }
        }

        private void ChannelMessageCommand(IDictionary<string, object> command)
        {
            MessageRecieved(command, false);
        }

        private void ChannelOperatorListCommand(IDictionary<string, object> command)
        {
            var channelName = (string) command["channel"];
            var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentChannels, channelName);

            if (channel == null)
                return;

            AddToSomeListCommand(command, "oplist", channel.Moderators);
            channel.CallListChanged();
        }

        private void CharacterDisconnectCommand(IDictionary<string, object> command)
        {
            var characterName = (string) command["character"];

            if (!ChatModel.IsOnline(characterName))
                return;

            var character = ChatModel.FindCharacter(characterName);
            var ofInterest = ChatModel.IsOfInterest(characterName);

            var channels = from c in ChatModel.CurrentChannels
                where c.RemoveCharacter(characterName)
                select
                    new Dictionary<string, object>
                        {
                            {
                                "character", character.Name
                            },
                            {
                                "channel", c.Id
                            }
                        };

            if (!ofInterest)
            {
                // don't show leave/join notifications if we already get a log out notification
                foreach (var c in channels)
                    LeaveChannelCommand(c);
            }

            var characterChannel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentPms, characterName);
            if (characterChannel != null)
                characterChannel.TypingStatus = TypingStatus.Clear;

            ChatModel.RemoveCharacter(characterName);
            Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        character, new CharacterUpdateModel.LoginStateChangedEventArgs {IsLogIn = false}));
        }

        private void DoAction()
        {
            lock (que)
            {
                if (que.Count <= 0)
                    return;

                var workingData = que[0];
                var toInvoke = InterpretCommand(workingData);
                if (toInvoke != null)
                    toInvoke.Invoke(workingData);

                que.RemoveAt(0);

                DoAction();
            }
        }

        private void EnqueAction(IDictionary<string, object> data)
        {
            que.Add(data);

            DoAction();
        }

        private void ErrorCommand(IDictionary<string, object> command)
        {
            var thisMessage = (string) command["message"];

            // for some fucktarded reason room status changes are only done through SYS
            if (thisMessage.IndexOf("this channel is now", StringComparison.OrdinalIgnoreCase) != -1)
            {
                RoomTypeChangedCommand(command);
                return;
            }

            // checks to see if this is a channel ban message
            if (thisMessage.IndexOf("Channel bans", StringComparison.OrdinalIgnoreCase) != -1)
            {
                ChannelBanListCommand(command);
                return;
            }

            // checks to ensure it's not a mod promote message
            if (thisMessage.IndexOf("has been", StringComparison.OrdinalIgnoreCase) == -1)
                Events.GetEvent<ErrorEvent>().Publish(thisMessage);
        }

        private void IgnoreUserCommand(IDictionary<string, object> command)
        {
            if ((command["action"] as string) != "delete")
            {
                AddToSomeListCommand(
                    command, command.ContainsKey("character") ? "character" : "characters", ChatModel.Ignored);

                // todo: add notification for this
            }
            else
            {
                // this makes unignore actually work
                var toRemove =
                    Enumerable.FirstOrDefault(ChatModel.Ignored,
                        ignore => ignore.Equals(command["character"] as string, StringComparison.OrdinalIgnoreCase));
                if (toRemove != null)
                    ChatModel.Ignored.Remove(toRemove);

                // todo: add notification for this
            }
        }

        private void InitialCharacterListCommand(IDictionary<string, object> command)
        {
            dynamic arr = command["characters"]; // dynamic for ease-of-use
            foreach (JsonArray character in arr)
            {
                ICharacter temp = new CharacterModel();

                temp.Name = (string) character[0]; // Character's name

                if (ChatModel.IsOnline(temp.Name))
                    continue;

                temp.Gender = ParseGender((string) character[1]); // character's gender

                temp.Status = (StatusType) Enum.Parse(typeof (StatusType), (string) character[2], true);

                // Character's status
                temp.StatusMessage = (string) character[3]; // Character's status message

                ChatModel.AddCharacter(temp); // also add it to the online characters collection
            }
        }

        private CommandDelegate InterpretCommand(IDictionary<string, object> command)
        {
            ChatModel.LastMessageReceived = DateTimeOffset.Now;

            if (command == null || string.IsNullOrWhiteSpace(command["command"] as string))
                return null;

            switch ((string) command["command"])
            {
                case "IDN":
                    return LoginCommand;
                case "UPT":
                    return UptimeCommand;
                case "ADL":
                    return AdminsCommand;
                case "IGN":
                    return IgnoreUserCommand;
                case "LIS":
                    return InitialCharacterListCommand;
                case "CHA":
                    return PublicChannelListCommand;
                case "ORS":
                    return PrivateChannelListCommand;
                case "STA":
                    return StatusChangedCommand;
                case "LRP":
                    return AdMessageCommand;
                case "MSG":
                    return ChannelMessageCommand;
                case "PRI":
                    return PrivateMessageCommand;
                case "TPN":
                    return TypingStatusCommand;
                case "JCH":
                    return JoinChannelCommand;
                case "LCH":
                    return LeaveChannelCommand;
                case "COL":
                    return ChannelOperatorListCommand;
                case "ICH":
                    return ChannelInitializedCommand;
                case "CDS":
                    return ChannelDesciptionCommand;
                case "SYS":
                case "ERR":
                    return ErrorCommand;
                case "CIU":
                    return InviteCommand;
                case "CKU":
                    return KickCommand;
                case "CBU":
                    return KickCommand;
                case "NLN":
                    return UserLoggedInCommand;
                case "FLN":
                    return CharacterDisconnectCommand;
                case "RLL":
                    return RollCommand;
                case "DOP":
                    return OperatorDemoteCommand;
                case "COP":
                    return OperatorPromoteCommand;
                case "COR":
                    return OperatorDemoteCommand;
                case "COA":
                    return OperatorPromoteCommand;
                case "RMO":
                    return RoomModeChangedCommand;
                case "BRO":
                    return BroadcastCommand;
                case "RTB":
                    return RealTimeBridgeCommand;
                case "SFC":
                    return NewReportCommand;
                default:
                    return null;
            }
        }

        private void InviteCommand(IDictionary<string, object> command)
        {
            var sender = command["sender"] as string;
            var id = command["name"] as string;
            var title = command["title"] as string;

            var args = new ChannelUpdateModel.ChannelInviteEventArgs {Inviter = sender};
            Events.GetEvent<NewUpdateEvent>().Publish(new ChannelUpdateModel(ChatModel.FindChannel(id, title), args));
        }

        private void JoinChannelCommand(IDictionary<string, object> command)
        {
            var title = (string) command["title"];
            var channelName = (string) command["channel"];

            var id = (IDictionary<string, object>) command["character"];
            var identity = (string) id["identity"];

            // JCH is used in a few situations. It is used when others join a channel and when we join a channel

            // if this is a situation where we are joining a channel...
            var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentChannels, channelName);
            if (channel == null)
            {
                var kind = ChannelType.Public;
                if (channelName.Contains("ADH-"))
                    kind = ChannelType.Private;

                manager.JoinChannel(kind, channelName, title);
            }
            else
            {
                var toAdd = ChatModel.FindCharacter(identity);
                if (channel.AddCharacter(toAdd))
                {
                    Events.GetEvent<NewUpdateEvent>().Publish(
                        new CharacterUpdateModel(
                            toAdd,
                            new CharacterUpdateModel.JoinLeaveEventArgs
                                {
                                    Joined = true,
                                    TargetChannel = channel.Title,
                                    TargetChannelId = channel.Id
                                }));
                }
            }
        }

        private void KickCommand(IDictionary<string, object> command)
        {
            var kicker = (string) command["operator"];
            var channelId = (string) command["channel"];
            var kicked = (string) command["character"];
            var isBan = (string) command["command"] == "CBU";

            if (kicked.Equals(ChatModel.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
                kicked = "you";

            var args = new ChannelUpdateModel.ChannelDisciplineEventArgs
                {
                    IsBan = isBan,
                    Kicked = kicked,
                    Kicker = kicker
                };
            var update = new ChannelUpdateModel(ChatModel.FindChannel(channelId), args);

            Events.GetEvent<NewUpdateEvent>().Publish(update);

            if (kicked == "you")
                manager.RemoveChannel(channelId);
        }

        private void LeaveChannelCommand(IDictionary<string, object> command)
        {
            var channelName = (string) command["channel"];
            var characterName = (string) command["character"];

            if (ChatModel.CurrentCharacter.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase))
                return;

            var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentChannels, channelName);
            if (channel == null)
                return;

            if (channel.RemoveCharacter(characterName))
            {
                Events.GetEvent<NewUpdateEvent>().Publish(
                    new CharacterUpdateModel(
                        ChatModel.FindCharacter(characterName),
                        new CharacterUpdateModel.JoinLeaveEventArgs
                            {
                                Joined = false,
                                TargetChannel = channel.Title,
                                TargetChannelId = channel.Id
                            }));
            }
        }

        private void LoginCommand(IDictionary<string, object> command)
        {
            ChatModel.ClientUptime = DateTimeOffset.Now;

            connection.SendMessage("CHA"); // request channels
            connection.SendMessage("UPT"); // request uptime
            connection.SendMessage("ORS"); // request private channels

            Dispatcher.Invoke((Action) delegate { ChatModel.IsAuthenticated = true; });
        }

        private void MessageRecieved(IDictionary<string, object> command, bool isAd)
        {
            var character = (string) command["character"];
            var message = (string) command["message"];
            var channel = (string) command["channel"];

            if (!Enumerable.Contains(ChatModel.Ignored, character, StringComparer.OrdinalIgnoreCase))
                manager.AddMessage(message, channel, character, isAd ? MessageType.Ad : MessageType.Normal);
        }

        private void NewReportCommand(IDictionary<string, object> command)
        {
            var type = command["action"] as string;
            if (string.IsNullOrWhiteSpace(type))
                return;

            if (type.Equals("report"))
            {
                // new report
                var report = command["report"] as string;
                var callId = command["callid"] as string;
                var logId = command.ContainsKey("logid") ? command["logid"] as int? : null;

                var reportIsClean = false;

                // "report" is in some sort of arbitrary and non-compulsory format
                // attempt to decipher it
                if (report != null)
                {
                    var rawReport = report.Split('|').Select(x => x.Trim()).ToList();

                    var starters = new[] {"Current Tab/Channel:", "Reporting User:", string.Empty};

                    // each section should start with one of these
                    var reportData = new List<string>();

                    for (var i = 0; i < rawReport.Count; i++)
                    {
                        if (rawReport[i].StartsWith(starters[i]))
                            reportData.Add(rawReport[i].Substring(starters[i].Length).Trim());
                    }

                    if (reportData.Count == 3)
                        reportIsClean = true;

                    var reporterName = command["character"] as string;
                    var reporter = ChatModel.FindCharacter(reporterName);

                    if (reportIsClean)
                    {
                        Events.GetEvent<NewUpdateEvent>()
                            .Publish(
                                new CharacterUpdateModel(
                                    reporter,
                                    new CharacterUpdateModel.ReportFiledEventArgs
                                        {
                                            Reported = reportData[0],
                                            Tab = reportData[1],
                                            Complaint = reportData[2],
                                            LogId = logId,
                                            CallId = callId,
                                        }));

                        reporter.LastReport = new ReportModel
                            {
                                Reporter = reporter,
                                Reported = reportData[0],
                                Tab = reportData[1],
                                Complaint = reportData[2],
                                CallId = callId,
                                LogId = logId
                            };
                    }
                    else
                    {
                        Events.GetEvent<NewUpdateEvent>()
                            .Publish(
                                new CharacterUpdateModel(
                                    reporter,
                                    new CharacterUpdateModel.ReportFiledEventArgs
                                        {
                                            Complaint = report,
                                            CallId = callId,
                                            LogId = logId,
                                        }));

                        reporter.LastReport = new ReportModel
                            {
                                Reporter = reporter,
                                Complaint = report,
                                CallId = callId,
                                LogId = logId
                            };
                    }
                }
            }
            else if (type.Equals("confirm"))
            {
                // someone else handling a report
                var handlerName = command["moderator"] as string;
                var handled = command["character"] as string;
                var handler = ChatModel.FindCharacter(handlerName);

                Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            handler, new CharacterUpdateModel.ReportHandledEventArgs {Handled = handled}));
            }
        }

        private void OperatorDemoteCommand(IDictionary<string, object> command)
        {
            var target = command["character"] as string;
            string channelId = null;

            if (command.ContainsKey("channel"))
                channelId = command["channel"] as string;

            PromoteOrDemote(target, false, channelId);
        }

        private void OperatorPromoteCommand(IDictionary<string, object> command)
        {
            var target = command["character"] as string;
            string channelId = null;

            if (command.ContainsKey("channel"))
                channelId = command["channel"] as string;

            PromoteOrDemote(target, true, channelId);
        }

        private void PrivateChannelListCommand(IDictionary<string, object> command)
        {
            ChannelListCommand(command, false);
        }

        private void PrivateMessageCommand(IDictionary<string, object> command)
        {
            var sender = (string) command["character"];
            if (!Enumerable.Contains(ChatModel.Ignored, sender, StringComparer.OrdinalIgnoreCase))
            {
                if (ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentPms, sender) == null)
                    manager.AddChannel(ChannelType.PrivateMessage, sender);

                manager.AddMessage(command["message"] as string, sender, sender);

                var temp = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentPms, sender);
                if (temp == null)
                    return;

                temp.TypingStatus = TypingStatus.Clear; // webclient assumption
            }
            else
            {
                connection.SendMessage(
                    new Dictionary<string, object>
                        {
                            {"action", "notify"},
                            {"character", sender},
                            {"type", "IGN"}
                        });
            }
        }

        private void PromoteOrDemote(string character, bool isPromote, string channelId = null)
        {
            string title = null;
            if (channelId != null)
            {
                var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentChannels, channelId);
                if (channel != null)
                    title = channel.Title;
            }

            var target = ChatModel.FindCharacter(character);

            if (target != null)
            {
                // avoids nasty null reference
                Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            target,
                            new CharacterUpdateModel.PromoteDemoteEventArgs
                                {
                                    TargetChannelId = channelId,
                                    TargetChannel = title,
                                    IsPromote = isPromote,
                                }));
            }
        }

        private void PublicChannelListCommand(IDictionary<string, object> command)
        {
            ChannelListCommand(command, true);

            // Per Kira's statements this avoids spamming the server
            var waitTimer = new Timer(350);
            var channels = from c in ApplicationSettings.SavedChannels
                where !string.IsNullOrWhiteSpace(c)
                select new {channel = c};
            var walk = channels.GetEnumerator();

            if (walk.MoveNext())
            {
                waitTimer.Elapsed += (s, e) =>
                    {
                        connection.SendMessage(walk.Current, "JCH");
                        if (walk.MoveNext())
                            return;

                        waitTimer.Stop();
                        waitTimer.Dispose();
                    };
            }

            waitTimer.Start();
        }

        private void RealTimeBridgeCommand(IDictionary<string, object> command)
        {
            var type = command["type"] as string;

            if (type != null && type.Equals("note"))
            {
                var senderName = command["sender"] as string;
                var subject = command["subject"] as string;
                var id = (long) command["id"];

                var sender = ChatModel.FindCharacter(senderName);

                Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            sender, new CharacterUpdateModel.NoteEventArgs {Subject = subject, NoteId = id}));
            }
            else if (type != null && type.Equals("comment"))
            {
                var name = command["name"] as string;
                var character = ChatModel.FindCharacter(name);

                // sometimes ID is sent as a string. Sometimes it is sent as a number.
                // so even though it's THE SAME COMMAND we have to treat *each* number differently
                var commentId = long.Parse((string) command["id"]);
                var parentId = (long) command["parent_id"];
                var targetId = long.Parse((string) command["target_id"]);

                var title = HttpUtility.HtmlDecode(command["target"] as string);

                var commentType =
                    (CharacterUpdateModel.CommentEventArgs.CommentTypes)
                        Enum.Parse(typeof (CharacterUpdateModel.CommentEventArgs.CommentTypes),
                            (string) command["target_type"], true);

                Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            character,
                            new CharacterUpdateModel.CommentEventArgs
                                {
                                    CommentId = commentId,
                                    CommentType = commentType,
                                    ParentId = parentId,
                                    TargetId = targetId,
                                    Title = title
                                }));
            }
        }

        private void RollCommand(IDictionary<string, object> command)
        {
            var channel = command["channel"] as string;
            var message = command["message"] as string;
            var poster = command["character"] as string;

            if (!ChatModel.Ignored.Contains(poster))
                manager.AddMessage(message, channel, poster, MessageType.Roll);
        }

        private void RoomModeChangedCommand(IDictionary<string, object> command)
        {
            var channelId = command["channel"] as string;
            var mode = (string) command["mode"];

            var newMode = (ChannelMode) Enum.Parse(typeof (ChannelMode), mode, true);
            var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentChannels, channelId);

            if (channel == null)
                return;

            channel.Mode = newMode;
            Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new ChannelUpdateModel(
                        channel,
                        new ChannelUpdateModel.ChannelModeUpdateEventArgs {NewMode = newMode,}));
        }

        private void RoomTypeChangedCommand(IDictionary<string, object> command)
        {
            var channelId = command["channel"] as string;
            var isPublic = ((string) command["message"]).IndexOf("public", StringComparison.OrdinalIgnoreCase) != -1;

            var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentChannels, channelId);

            if (channel == null)
                return; // can't change the settings of a room we don't know

            if (isPublic)
            {
                // room is now open
                channel.Type = ChannelType.Private;

                Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new ChannelUpdateModel(
                            channel,
                            new ChannelUpdateModel.ChannelTypeChangedEventArgs {IsOpen = true}));
            }
            else
            {
                // room is InviteOnly
                channel.Type = ChannelType.InviteOnly;

                Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new ChannelUpdateModel(
                            channel,
                            new ChannelUpdateModel.ChannelTypeChangedEventArgs {IsOpen = false}));
            }
        }

        private void StatusChangedCommand(IDictionary<string, object> command)
        {
            var character = (string) command["character"];
            var status = (StatusType) Enum.Parse(typeof (StatusType), (string) command["status"], true);
            var statusMessage = (string) command["statusmsg"];

            if (!ChatModel.IsOnline(character))
                return;

            var temp = ChatModel.FindCharacter(character);
            var statusChanged = false;
            var statusMessageChanged = false;

            if (temp.Status != status)
            {
                statusChanged = true;
                temp.Status = status;
            }

            if (temp.StatusMessage != statusMessage)
            {
                statusMessageChanged = true;
                temp.StatusMessage = statusMessage;
            }

            // fixes a bug wherein webclients could send a do-nothing update
            if (!statusChanged && !statusMessageChanged)
                return;

            var args = new CharacterUpdateModel.StatusChangedEventArgs
                {
                    NewStatusType =
                        statusChanged
                            ? status
                            : StatusType.Offline,
                    NewStatusMessage =
                        statusMessageChanged
                            ? statusMessage
                            : null
                };

            Events.GetEvent<NewUpdateEvent>().Publish(new CharacterUpdateModel(temp, args));
        }

        private void TypingStatusCommand(IDictionary<string, object> command)
        {
            var sender = (string) command["character"];

            var channel = ExtensionMethods.FirstByIdOrDefault(ChatModel.CurrentPms, sender);
            if (channel == null)
                return;

            var type = (TypingStatus) Enum.Parse(typeof (TypingStatus), (string) command["status"], true);

            channel.TypingStatus = type;
        }

        private void UptimeCommand(IDictionary<string, object> command)
        {
            var time = (long) command["starttime"];
            ChatModel.ServerUpTime = HelperConverter.UnixTimeToDateTime(time);
        }

        private void UserLoggedInCommand(IDictionary<string, object> command)
        {
            var character = (string) command["identity"];

            if (ChatModel.CurrentCharacter.Name.Equals(character, StringComparison.OrdinalIgnoreCase))
                return;

            // we do not need to keep track of our own character in the character list
            if (ChatModel.IsOnline(character))
                return;

            var temp = new CharacterModel
                {
                    Name = character,
                    Gender = ParseGender((string) command["gender"]),
                    Status =
                        (StatusType) Enum.Parse(typeof (StatusType), (string) command["status"], true)
                };

            // Character's status
            ChatModel.AddCharacter(temp); // also add it to the online characters collection

            Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        temp, new CharacterUpdateModel.LoginStateChangedEventArgs {IsLogIn = true}));
        }

        private void GetCharacter(string character)
        {
            Events.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(GetCharacter);
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
            ChatModel.Wipe();
        }

        #endregion
    }
}