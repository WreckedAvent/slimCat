// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandInterceptor.cs" company="Justin Kadrovach">
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
//   This interprets the commands and translates them to methods that our various other services can use.
//   It also coordinates them to prevent collisions.
//   This intercepts just about every single command that the server sends.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using System.Web;
    using System.Windows;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using SimpleJson;

    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Utilities;
    using Slimcat.ViewModels;

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
        /// Initializes a new instance of the <see cref="CommandInterceptor"/> class.
        /// </summary>
        /// <param name="cm">
        /// The cm.
        /// </param>
        /// <param name="conn">
        /// The conn.
        /// </param>
        /// <param name="manager">
        /// The manager.
        /// </param>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="eventagg">
        /// The eventagg.
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
            this.connection = conn;
            this.manager = manager;

            this.Events.GetEvent<CharacterSelectedLoginEvent>()
                .Subscribe(this.GetCharacter, ThreadOption.BackgroundThread, true);
            this.Events.GetEvent<ChatCommandEvent>().Subscribe(this.EnqueAction, ThreadOption.BackgroundThread, true);

            this.ChatModel.CurrentAccount = this.connection.Account;
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
                var arr = (JsonArray)command[paramaterToPullFrom];
                foreach (
                    var character in
                        from string character in arr
                        where !string.IsNullOrWhiteSpace(character)
                        where !listToAddTo.Contains(character)
                        select character)
                {
                    listToAddTo.Add(character);
                }
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
                    return (Gender)Enum.Parse(typeof(Gender), input, true);
            }
        }

        private void AdMessageCommand(IDictionary<string, object> command)
        {
            this.MessageRecieved(command, true);
        }

        private void AdminsCommand(IDictionary<string, object> command)
        {
            AddToSomeListCommand(command, "ops", this.ChatModel.Mods);
            if (this.ChatModel.Mods.Contains(this.ChatModel.CurrentCharacter.Name))
            {
                this.Dispatcher.Invoke((Action)delegate { this.ChatModel.IsGlobalModerator = true; });
            }
        }

        private void BroadcastCommand(IDictionary<string, object> command)
        {
            var message = command["message"] as string;
            var posterName = command["character"] as string;
            var poster = this.ChatModel.FindCharacter(posterName);

            this.Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(poster, new CharacterUpdateModel.BroadcastEventArgs { Message = message }));
        }

        private void ChannelBanListCommand(IDictionary<string, object> command)
        {
            var channelId = (string)command["channel"];
            var channel = this.ChatModel.CurrentChannels.FirstByIdOrDefault(channelId);

            if (channel == null)
            {
                return;
            }

            channel.Banned.Clear();

            var message = ((string)command["message"]).Split(':');
            var banned = message[1].Trim();

            if (banned.IndexOf(',') == -1)
            {
                channel.Banned.Add(banned);
            }
            else
            {
                var bannedList = banned.Split(',');
                foreach (var ban in bannedList)
                {
                    channel.Banned.Add(ban.Trim());
                }
            }

            this.Events.GetEvent<NewUpdateEvent>()
                .Publish(new ChannelUpdateModel(channel, new ChannelUpdateModel.ChannelTypeBannedListEventArgs()));
        }

        private void ChannelDesciptionCommand(IDictionary<string, object> command)
        {
            var channelName = command["channel"];
            var channel = this.ChatModel.CurrentChannels.First(x => x.Id == channelName as string);
            var description = command["description"];

            var isInitializer = string.IsNullOrWhiteSpace(channel.Description);

            if (!isInitializer)
            {
                channel.Description = description as string;
            }
            else if (description.ToString().StartsWith("Welcome to your private room!"))
            {
                // shhh go away lame init description
                channel.Description =
                    "Man this description is lame. You should change it and make it amaaaaaazing. Click that pencil, man.";
            }
            else
            {
                channel.Description = description as string; // derpherp no channel description bug fix
            }

            if (isInitializer)
            {
                return;
            }

            var args = new ChannelUpdateModel.ChannelDescriptionChangedEventArgs();
            this.Events.GetEvent<NewUpdateEvent>().Publish(new ChannelUpdateModel(channel, args));
        }

        private void ChannelInitializedCommand(IDictionary<string, object> command)
        {
            var channelName = (string)command["channel"];
            var mode = (ChannelMode)Enum.Parse(typeof(ChannelMode), (string)command["mode"], true);
            var channel = this.ChatModel.CurrentChannels.FirstByIdOrDefault(channelName);

            channel.Mode = mode;
            dynamic users = command["users"]; // dynamic lets us deal with odd syntax
            foreach (IDictionary<string, object> character in users)
            {
                var name = character["identity"] as string;

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (
                    !channel.Users.Any(
                        x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && this.ChatModel.IsOnline(name)))
                {
                    channel.Users.Add(this.ChatModel.FindCharacter(name));
                }
            }
        }

        private void ChannelListCommand(IDictionary<string, object> command, bool isPublic)
        {
            dynamic arr = command["channels"];
            lock (this.ChatModel.AllChannels)
            {
                foreach (IDictionary<string, object> channel in arr)
                {
                    var name = channel["name"] as string;
                    string title = null;
                    if (!isPublic)
                    {
                        title = HttpUtility.HtmlDecode(channel["title"] as string);
                    }

                    var mode = ChannelMode.Both;
                    if (isPublic)
                    {
                        mode = (ChannelMode)Enum.Parse(typeof(ChannelMode), (string)channel["mode"], true);
                    }

                    var number = (long)channel["characters"];
                    if (number < 0)
                    {
                        number = 0;
                    }

                    this.Dispatcher.Invoke(
                        (Action)
                        (() =>
                         this.ChatModel.AllChannels.Add(
                             new GeneralChannelModel(
                             name, isPublic ? ChannelType.Public : ChannelType.Private, (int)number, mode)
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
            this.MessageRecieved(command, false);
        }

        private void ChannelOperatorListCommand(IDictionary<string, object> command)
        {
            var channelName = (string)command["channel"];
            var channel = this.ChatModel.CurrentChannels.FirstByIdOrDefault(channelName);

            if (channel == null)
            {
                return;
            }

            AddToSomeListCommand(command, "oplist", channel.Moderators);
            channel.CallListChanged();
        }

        private void CharacterDisconnectCommand(IDictionary<string, object> command)
        {
            var characterName = (string)command["character"];

            if (!this.ChatModel.IsOnline(characterName))
            {
                return;
            }

            var character = this.ChatModel.FindCharacter(characterName);
            var ofInterest = this.ChatModel.IsOfInterest(characterName);

            var channels = from c in this.ChatModel.CurrentChannels
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
                {
                    this.LeaveChannelCommand(c);
                }
            }

            var characterChannel = this.ChatModel.CurrentPMs.FirstByIdOrDefault(characterName);
            if (characterChannel != null)
            {
                characterChannel.TypingStatus = TypingStatus.Clear;
            }

            this.ChatModel.RemoveCharacter(characterName);
            this.Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        character, new CharacterUpdateModel.LoginStateChangedEventArgs { IsLogIn = false }));
        }

        private void DoAction()
        {
            lock (this.que)
            {
                if (this.que.Count <= 0)
                {
                    return;
                }

                var workingData = this.que[0];
                var toInvoke = this.InterpretCommand(workingData);
                if (toInvoke != null)
                {
                    toInvoke.Invoke(workingData);
                }

                this.que.RemoveAt(0);

                this.DoAction();
            }
        }

        private void EnqueAction(IDictionary<string, object> data)
        {
            this.que.Add(data);

            this.DoAction();
        }

        private void ErrorCommand(IDictionary<string, object> command)
        {
            var thisMessage = (string)command["message"];

            // for some fucktarded reason room status changes are only done through SYS
            if (thisMessage.IndexOf("this channel is now", StringComparison.OrdinalIgnoreCase) != -1)
            {
                this.RoomTypeChangedCommand(command);
                return;
            }

            // checks to see if this is a channel ban message
            if (thisMessage.IndexOf("Channel bans", StringComparison.OrdinalIgnoreCase) != -1)
            {
                this.ChannelBanListCommand(command);
                return;
            }

            // checks to ensure it's not a mod promote message
            if (thisMessage.IndexOf("has been", StringComparison.OrdinalIgnoreCase) == -1)
            {
                this.Events.GetEvent<ErrorEvent>().Publish(thisMessage);
            }
        }

        private void IgnoreUserCommand(IDictionary<string, object> command)
        {
            if ((command["action"] as string) != "delete")
            {
                AddToSomeListCommand(
                    command, command.ContainsKey("character") ? "character" : "characters", this.ChatModel.Ignored);

                // todo: add notification for this
            }
            else
            {
                // this makes unignore actually work
                var toRemove =
                    this.ChatModel.Ignored.FirstOrDefault(
                        ignore => ignore.Equals(command["character"] as string, StringComparison.OrdinalIgnoreCase));
                if (toRemove != null)
                {
                    this.ChatModel.Ignored.Remove(toRemove);
                }

                // todo: add notification for this
            }
        }

        private void InitialCharacterListCommand(IDictionary<string, object> command)
        {
            dynamic arr = command["characters"]; // dynamic for ease-of-use
            foreach (JsonArray character in arr)
            {
                ICharacter temp = new CharacterModel();

                temp.Name = (string)character[0]; // Character's name

                if (this.ChatModel.IsOnline(temp.Name))
                {
                    continue;
                }

                temp.Gender = ParseGender((string)character[1]); // character's gender

                temp.Status = (StatusType)Enum.Parse(typeof(StatusType), (string)character[2], true);

                // Character's status
                temp.StatusMessage = (string)character[3]; // Character's status message

                this.ChatModel.AddCharacter(temp); // also add it to the online characters collection
            }
        }

        private CommandDelegate InterpretCommand(IDictionary<string, object> command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command["command"] as string))
            {
                return null;
            }

            this.ChatModel.LastMessageReceived = DateTimeOffset.Now;

            switch ((string)command["command"])
            {
                case "IDN":
                    return this.LoginCommand;
                case "UPT":
                    return this.UptimeCommand;
                case "ADL":
                    return this.AdminsCommand;
                case "IGN":
                    return this.IgnoreUserCommand;
                case "LIS":
                    return this.InitialCharacterListCommand;
                case "CHA":
                    return this.PublicChannelListCommand;
                case "ORS":
                    return this.PrivateChannelListCommand;
                case "STA":
                    return this.StatusChangedCommand;
                case "LRP":
                    return this.AdMessageCommand;
                case "MSG":
                    return this.ChannelMessageCommand;
                case "PRI":
                    return this.PrivateMessageCommand;
                case "TPN":
                    return this.TypingStatusCommand;
                case "JCH":
                    return this.JoinChannelCommand;
                case "LCH":
                    return this.LeaveChannelCommand;
                case "COL":
                    return this.ChannelOperatorListCommand;
                case "ICH":
                    return this.ChannelInitializedCommand;
                case "CDS":
                    return this.ChannelDesciptionCommand;
                case "SYS":
                case "ERR":
                    return this.ErrorCommand;
                case "CIU":
                    return this.InviteCommand;
                case "CKU":
                    return this.KickCommand;
                case "CBU":
                    return this.KickCommand;
                case "NLN":
                    return this.UserLoggedInCommand;
                case "FLN":
                    return this.CharacterDisconnectCommand;
                case "RLL":
                    return this.RollCommand;
                case "DOP":
                    return this.OperatorDemoteCommand;
                case "COP":
                    return this.OperatorPromoteCommand;
                case "COR":
                    return this.OperatorDemoteCommand;
                case "COA":
                    return this.OperatorPromoteCommand;
                case "RMO":
                    return this.RoomModeChangedCommand;
                case "BRO":
                    return this.BroadcastCommand;
                case "RTB":
                    return this.RealTimeBridgeCommand;
                case "SFC":
                    return this.NewReportCommand;
                default:
                    return null;
            }
        }

        private void InviteCommand(IDictionary<string, object> command)
        {
            var sender = command["sender"] as string;
            var id = command["name"] as string;
            var title = command["title"] as string;

            var args = new ChannelUpdateModel.ChannelInviteEventArgs { Inviter = sender };
            this.Events.GetEvent<NewUpdateEvent>().Publish(new ChannelUpdateModel(this.ChatModel.FindChannel(id, title), args));
        }

        private new void JoinChannelCommand(IDictionary<string, object> command)
        {
            var title = (string)command["title"];
            var channelName = (string)command["channel"];

            var id = (IDictionary<string, object>)command["character"];
            var identity = (string)id["identity"];

            // JCH is used in a few situations. It is used when others join a channel and when we join a channel

            // if this is a situation where we are joining a channel...
            var channel = this.ChatModel.CurrentChannels.FirstByIdOrDefault(channelName);
            if (channel == null)
            {
                var kind = ChannelType.Public;
                if (channelName.Contains("ADH-"))
                {
                    kind = ChannelType.Private;
                }

                this.manager.JoinChannel(kind, channelName, title);
            }
            else
            {
                var toAdd = this.ChatModel.FindCharacter(identity);
                if (channel.AddCharacter(toAdd))
                {
                    this.Events.GetEvent<NewUpdateEvent>().Publish(
                            new CharacterUpdateModel(
                                toAdd, 
                                new CharacterUpdateModel.JoinLeaveEventArgs
                                    {
                                        Joined = true, 
                                        TargetChannel = channel.Title, 
                                        TargetChannelID = channel.Id
                                    }));
                }
            }
        }

        private new void KickCommand(IDictionary<string, object> command)
        {
            var kicker = (string)command["operator"];
            var channelId = (string)command["channel"];
            var kicked = (string)command["character"];
            var isBan = (string)command["command"] == "CBU";

            if (kicked.Equals(this.ChatModel.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
            {
                kicked = "you";
            }

            var args = new ChannelUpdateModel.ChannelDisciplineEventArgs
                           {
                               IsBan = isBan, 
                               Kicked = kicked, 
                               Kicker = kicker
                           };
            var update = new ChannelUpdateModel(this.ChatModel.FindChannel(channelId), args);

            this.Events.GetEvent<NewUpdateEvent>().Publish(update);

            if (kicked == "you")
            {
                this.manager.RemoveChannel(channelId);
            }
        }

        private void LeaveChannelCommand(IDictionary<string, object> command)
        {
            var channelName = (string)command["channel"];
            var characterName = (string)command["character"];

            if (this.ChatModel.CurrentCharacter.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var channel = this.ChatModel.CurrentChannels.FirstByIdOrDefault(channelName);
            if (channel == null)
            {
                return;
            }

            if (channel.RemoveCharacter(characterName))
            {
                this.Events.GetEvent<NewUpdateEvent>().Publish(
                        new CharacterUpdateModel(
                            this.ChatModel.FindCharacter(characterName), 
                            new CharacterUpdateModel.JoinLeaveEventArgs
                                {
                                    Joined = false, 
                                    TargetChannel = channel.Title, 
                                    TargetChannelID = channel.Id
                                }));
            }
        }

        private void LoginCommand(IDictionary<string, object> command)
        {
            this.ChatModel.ClientUptime = DateTimeOffset.Now;

            this.connection.SendMessage("CHA"); // request channels
            this.connection.SendMessage("UPT"); // request uptime
            this.connection.SendMessage("ORS"); // request private channels

            this.Dispatcher.Invoke((Action)delegate { this.ChatModel.IsAuthenticated = true; });
        }

        private void MessageRecieved(IDictionary<string, object> command, bool isAd)
        {
            var character = (string)command["character"];
            var message = (string)command["message"];
            var channel = (string)command["channel"];

            if (!this.ChatModel.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
            {
                this.manager.AddMessage(message, channel, character, isAd ? MessageType.Ad : MessageType.Normal);
            }
        }

        private void NewReportCommand(IDictionary<string, object> command)
        {
            var type = command["action"] as string;
            if (string.IsNullOrWhiteSpace(type))
            {
                return;
            }

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

                    var starters = new[] { "Current Tab/Channel:", "Reporting User:", string.Empty };

                    // each section should start with one of these
                    var reportData = new List<string>();

                    for (var i = 0; i < rawReport.Count; i++)
                    {
                        if (rawReport[i].StartsWith(starters[i]))
                        {
                            reportData.Add(rawReport[i].Substring(starters[i].Length).Trim());
                        }
                    }

                    if (reportData.Count == 3)
                    {
                        reportIsClean = true;
                    }

                    var reporterName = command["character"] as string;
                    var reporter = this.ChatModel.FindCharacter(reporterName);

                    if (reportIsClean)
                    {
                        this.Events.GetEvent<NewUpdateEvent>()
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
                        this.Events.GetEvent<NewUpdateEvent>()
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
                var handler = this.ChatModel.FindCharacter(handlerName);

                this.Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            handler, new CharacterUpdateModel.ReportHandledEventArgs { Handled = handled }));
            }
        }

        private void OperatorDemoteCommand(IDictionary<string, object> command)
        {
            var target = command["character"] as string;
            string channelId = null;

            if (command.ContainsKey("channel"))
            {
                channelId = command["channel"] as string;
            }

            this.PromoteOrDemote(target, false, channelId);
        }

        private void OperatorPromoteCommand(IDictionary<string, object> command)
        {
            var target = command["character"] as string;
            string channelId = null;

            if (command.ContainsKey("channel"))
            {
                channelId = command["channel"] as string;
            }

            this.PromoteOrDemote(target, true, channelId);
        }

        private void PrivateChannelListCommand(IDictionary<string, object> command)
        {
            this.ChannelListCommand(command, false);
        }

        private void PrivateMessageCommand(IDictionary<string, object> command)
        {
            var sender = (string)command["character"];
            if (!this.ChatModel.Ignored.Contains(sender, StringComparer.OrdinalIgnoreCase))
            {
                if (this.ChatModel.CurrentPMs.FirstByIdOrDefault(sender) == null)
                {
                    this.manager.AddChannel(ChannelType.PrivateMessage, sender);
                }

                this.manager.AddMessage(command["message"] as string, sender, sender);

                var temp = this.ChatModel.CurrentPMs.FirstByIdOrDefault(sender);
                if (temp == null)
                {
                    return;
                }

                temp.TypingStatus = TypingStatus.Clear; // webclient assumption
            }
            else
            {
                this.connection.SendMessage(
                    new Dictionary<string, object>
                        {
                            { "action", "notify" }, 
                            { "character", sender }, 
                            { "type", "IGN" }
                        });
            }
        }

        private void PromoteOrDemote(string character, bool isPromote, string channelID = null)
        {
            string title = null;
            if (channelID != null)
            {
                var channel = this.ChatModel.CurrentChannels.FirstByIdOrDefault(channelID);
                if (channel != null)
                {
                    title = channel.Title;
                }
            }

            var target = this.ChatModel.FindCharacter(character);

            if (target != null)
            {
                // avoids nasty null reference
                this.Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            target, 
                            new CharacterUpdateModel.PromoteDemoteEventArgs
                                {
                                    TargetChannelID = channelID, 
                                    TargetChannel = title, 
                                    IsPromote = isPromote, 
                                }));
            }
        }

        private void PublicChannelListCommand(IDictionary<string, object> command)
        {
            this.ChannelListCommand(command, true);

            // Per Kira's statements this avoids spamming the server
            var waitTimer = new Timer(350);
            var channels = from c in ApplicationSettings.SavedChannels
                           where !string.IsNullOrWhiteSpace(c)
                           select new { channel = c };
            var walk = channels.GetEnumerator();

            if (walk.MoveNext())
            {
                waitTimer.Elapsed += (s, e) =>
                    {
                        this.connection.SendMessage(walk.Current, "JCH");
                        if (walk.MoveNext())
                        {
                            return;
                        }

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
                var id = (long)command["id"];

                var sender = this.ChatModel.FindCharacter(senderName);

                this.Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            sender, new CharacterUpdateModel.NoteEventArgs { Subject = subject, NoteID = id }));
            }
            else if (type != null && type.Equals("comment"))
            {
                var name = command["name"] as string;
                var character = this.ChatModel.FindCharacter(name);

                // sometimes ID is sent as a string. Sometimes it is sent as a number.
                // so even though it's THE SAME COMMAND we have to treat *each* number differently
                var commentId = long.Parse((string)command["id"]);
                var parentId = (long)command["parent_id"];
                var targetId = long.Parse((string)command["target_id"]);

                var title = HttpUtility.HtmlDecode(command["target"] as string);

                var commentType =
                    (CharacterUpdateModel.CommentEventArgs.CommentTypes)
                    Enum.Parse(typeof(CharacterUpdateModel.CommentEventArgs.CommentTypes), (string)command["target_type"], true);

                this.Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new CharacterUpdateModel(
                            character, 
                            new CharacterUpdateModel.CommentEventArgs
                                {
                                    CommentID = commentId, 
                                    CommentType = commentType, 
                                    ParentID = parentId, 
                                    TargetID = targetId, 
                                    Title = title
                                }));
            }
        }

        private void RollCommand(IDictionary<string, object> command)
        {
            var channel = command["channel"] as string;
            var message = command["message"] as string;
            var poster = command["character"] as string;

            if (!this.ChatModel.Ignored.Contains(poster))
            {
                this.manager.AddMessage(message, channel, poster, MessageType.Roll);
            }
        }

        private void RoomModeChangedCommand(IDictionary<string, object> command)
        {
            var channelId = command["channel"] as string;
            var mode = (string)command["mode"];

            var newMode = (ChannelMode)Enum.Parse(typeof(ChannelMode), mode, true);
            var channel = this.ChatModel.CurrentChannels.FirstByIdOrDefault(channelId);

            if (channel == null)
            {
                return;
            }

            channel.Mode = newMode;
            this.Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new ChannelUpdateModel(
                        channel,
                        new ChannelUpdateModel.ChannelModeUpdateEventArgs { NewMode = newMode, }));
        }

        private void RoomTypeChangedCommand(IDictionary<string, object> command)
        {
            var channelId = command["channel"] as string;
            var isPublic = ((string)command["message"]).IndexOf("public", StringComparison.OrdinalIgnoreCase) != -1;

            var channel = this.ChatModel.CurrentChannels.FirstByIdOrDefault(channelId);

            if (channel == null)
            {
                return; // can't change the settings of a room we don't know
            }

            if (isPublic)
            {
                // room is now open
                channel.Type = ChannelType.Private;

                this.Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new ChannelUpdateModel(
                            channel, 
                            new ChannelUpdateModel.ChannelTypeChangedEventArgs { IsOpen = true }));
            }
            else
            {
                // room is InviteOnly
                channel.Type = ChannelType.InviteOnly;

                this.Events.GetEvent<NewUpdateEvent>()
                    .Publish(
                        new ChannelUpdateModel(
                            channel, 
                            new ChannelUpdateModel.ChannelTypeChangedEventArgs { IsOpen = false }));
            }
        }

        private void StatusChangedCommand(IDictionary<string, object> command)
        {
            var character = (string)command["character"];
            var status = (StatusType)Enum.Parse(typeof(StatusType), (string)command["status"], true);
            var statusMessage = (string)command["statusmsg"];

            if (!this.ChatModel.IsOnline(character))
            {
                return;
            }

            var temp = this.ChatModel.FindCharacter(character);
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
            {
                return;
            }

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

            this.Events.GetEvent<NewUpdateEvent>().Publish(new CharacterUpdateModel(temp, args));
        }

        private void TypingStatusCommand(IDictionary<string, object> command)
        {
            var sender = (string)command["character"];

            var channel = this.ChatModel.CurrentPMs.FirstByIdOrDefault(sender);
            if (channel == null)
            {
                return;
            }

            var type = (TypingStatus)Enum.Parse(typeof(TypingStatus), (string)command["status"], true);

            channel.TypingStatus = type;
        }

        private void UptimeCommand(IDictionary<string, object> command)
        {
            var time = (long)command["starttime"];
            this.ChatModel.ServerUpTime = HelperConverter.UnixTimeToDateTime(time);
        }

        private void UserLoggedInCommand(IDictionary<string, object> command)
        {
            var character = (string)command["identity"];

            if (this.ChatModel.CurrentCharacter.Name.Equals(character, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // we do not need to keep track of our own character in the character list
            if (this.ChatModel.IsOnline(character))
            {
                return;
            }

            var temp = new CharacterModel
                           {
                               Name = character,
                               Gender = ParseGender((string)command["gender"]),
                               Status =
                                   (StatusType)Enum.Parse(typeof(StatusType), (string)command["status"], true)
                           };

            // Character's status
            this.ChatModel.AddCharacter(temp); // also add it to the online characters collection

            this.Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        temp, new CharacterUpdateModel.LoginStateChangedEventArgs { IsLogIn = true }));
        }

        private void GetCharacter(string character)
        {
            this.Events.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(this.GetCharacter);
            this.ChatModel.CurrentCharacter = new CharacterModel { Name = character, Status = StatusType.Online };
            this.ChatModel.CurrentCharacter.GetAvatar();

            this.Dispatcher.Invoke(
                (Action)
                delegate
                    {
                        Application.Current.MainWindow.Title = string.Format(
                            "{0} {1} ({2})", Constants.ClientID, Constants.ClientName, character);
                    });
        }

        #endregion
    }
}