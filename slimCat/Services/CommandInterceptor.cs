using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using SimpleJson;
using slimCat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using ViewModels;

namespace Services
{
    /// <summary>
    /// This interprets the commands and translates them to methods that our various other services can use.
    /// It also coordinates them to prevent collisions.
    /// This intercepts just about every single command that the server sends.
    /// </summary>
    class CommandInterceptor : ViewModelBase
    {
        #region Fields
        private readonly IChatConnection _connection;
        private readonly IChannelManager _manager;
        private IList<IDictionary<string, object>> _que = new List<IDictionary<String, object>>();
        
        public delegate void CommandDelegate(IDictionary<string, object> command);
        #endregion

        #region Constructors
        public CommandInterceptor(IChatModel cm, IChatConnection conn, IChannelManager manager,
                                  IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            _connection = conn;
            _manager = manager;

            _events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(getCharacter, ThreadOption.BackgroundThread, true);
            _events.GetEvent<ChatCommandEvent>().Subscribe(EnqueAction, ThreadOption.BackgroundThread, true);

            _cm.OurAccount = _connection.Account;
        }
        #endregion

        public override void Initialize() { }

        private CommandDelegate InterpretCommand(IDictionary<string, object> command)
        {
            if (command == null || String.IsNullOrWhiteSpace(command["command"] as string))
                return null;

            switch ((string)command["command"])
            {
                case "IDN": return new CommandDelegate(LoginCommand);
                case "UPT": return new CommandDelegate(UptimeCommand);
                case "FRL": return new CommandDelegate(BookmarkCommand);
                case "ADL": return new CommandDelegate(AdminsCommand);
                case "IGN": return new CommandDelegate(IgnoreUserCommand);
                case "LIS": return new CommandDelegate(InitialCharacterListCommand);
                case "CHA": return new CommandDelegate(PublicChannelListCommand);
                case "ORS": return new CommandDelegate(PrivateChannelListCommand);
                case "STA": return new CommandDelegate(StatusChangedCommand);
                case "LRP": return new CommandDelegate(AdMessageCommand);
                case "MSG": return new CommandDelegate(ChannelMessageCommand);
                case "PRI": return new CommandDelegate(PrivateMessageCommand);
                case "TPN": return new CommandDelegate(TypingStatusCommand);
                case "JCH": return new CommandDelegate(JoinChannelCommand);
                case "LCH": return new CommandDelegate(LeaveChannelCommand);
                case "COL": return new CommandDelegate(ChannelOperatorListCommand);
                case "ICH": return new CommandDelegate(ChannelInitializedCommand);
                case "CDS": return new CommandDelegate(ChannelDesciptionCommand);
                case "SYS":
                case "ERR": return new CommandDelegate(ErrorCommand);
                case "CIU": return new CommandDelegate(InviteCommand);
                case "CKU": return new CommandDelegate(KickCommand);
                case "CBU": return new CommandDelegate(KickCommand);
                case "NLN": return new CommandDelegate(UserLoggedInCommand);
                case "FLN": return new CommandDelegate(CharacterDisconnectCommand);
                case "RLL": return new CommandDelegate(RollCommand);
                case "DOP": return new CommandDelegate(OperatorDemoteCommand);
                case "COP": return new CommandDelegate(OperatorPromoteCommand);
                case "COR": return new CommandDelegate(OperatorDemoteCommand);
                case "COA": return new CommandDelegate(OperatorPromoteCommand);
                case "RMO": return new CommandDelegate(RoomModeChangedCommand);
                default: return null;
            }
        }

        #region Generic and helper methods
        private Gender ParseGender(string input)
        {
            switch (input)
                {
                    //manually determine some really annoyingly-named genders
                    case "Male-Herm":
                        return Gender.Herm_M;

                    case "Herm":
                        return Gender.Herm_F;

                    case "Cunt-boy":
                        return Gender.Cuntboy;

                    default: // every other gender is parsed normally
                        return (Gender)Enum.Parse(typeof(Gender), input);
                }
        }
        private void ChannelListCommand(IDictionary<string, object> command, bool isPublic)
        {
            dynamic arr = command["channels"];
            foreach (IDictionary<string, object> channel in arr)
            {
                string name = channel["name"] as string;
                string title = null;
                if (!isPublic)
                    title = channel["title"] as string;

                ChannelMode mode = ChannelMode.both;
                if (isPublic)
                    mode = (ChannelMode)Enum.Parse(typeof(ChannelMode), channel["mode"] as string, true);

                long number = (long)channel["characters"];
                if (number < 0) number = 0;

                Dispatcher.Invoke(
                    (Action)delegate
                    {
                        _cm.AllChannels.Add(new GeneralChannelModel(name, (isPublic ? ChannelType.pub : ChannelType.priv), (int)number, mode)
                        { 
                            Title = (isPublic ? name : title)
                        });
                    });
            }
        }
        private void AddToSomeListCommand(IDictionary<string, object> command, string paramaterToPullFrom, IList<string> listToAddTo)
        {
            if (!(command[paramaterToPullFrom] is string)) // ensure that our arguments are actually an array
            {
                JsonArray arr = (JsonArray)command[paramaterToPullFrom];
                foreach (string character in arr)
                {
                    if (!listToAddTo.Contains(character))
                        listToAddTo.Add(character);
                }
            }
            else
            {
                var toAdd = (command[paramaterToPullFrom] as string);
                if (!listToAddTo.Contains(toAdd)) // IGN crash fix
                    listToAddTo.Add(toAdd);
            }
        }
        private void MessageRecieved(IDictionary<string, object> command, bool isAd)
        {
            string character = (string)command["character"];
            string message = (string)command["message"];
            string channel = (string)command["channel"];

            if (!_cm.Ignored.Any(ignoree => ignoree.Equals(character, StringComparison.OrdinalIgnoreCase))) 
                _manager.AddMessage(message, channel, character, (isAd ? MessageType.ad : MessageType.normal));
        }
        private void PromoteOrDemote(string character, bool isPromote, string channelID = null)
        {
            string title = null;
            if (channelID != null)
            {
                var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelID);
                if (channel != null)
                    title = channel.Title;
            }

            var target = _cm.FindCharacter(character);

            if (target != null) // avoids nasty null reference
            {
                _events.GetEvent<NewUpdateEvent>().Publish(
                    new CharacterUpdateModel(
                        target,
                        new Models.CharacterUpdateModel.PromoteDemoteEventArgs()
                        {
                            TargetChannelID = channelID,
                            TargetChannel = title,
                            IsPromote = isPromote,
                        })
                    );

            }
        }
        #endregion

        #region Command Delegate Methods
        private void LoginCommand(IDictionary<string, object> command)
        {
            _cm.ClientUptime = DateTimeOffset.Now;

            _connection.SendMessage("CHA"); // request channels
            _connection.SendMessage("UPT"); // request uptime
            _connection.SendMessage("ORS"); // request private channels

            Dispatcher.Invoke(
                (Action)delegate
                {
                    _cm.IsAuthenticated = true;
                });
        }

        private void UptimeCommand(IDictionary<string, object> command)
        {
            
            long time = (long)command["starttime"];
            _cm.ServerUpTime = HelperConverter.UnixTimeToDateTime(time);
        }

        private void BookmarkCommand(IDictionary<string, object> command)
        {
            AddToSomeListCommand(command, "characters", _cm.Bookmarks);
        }

        private void AdminsCommand(IDictionary<string, object> command)
        {
            AddToSomeListCommand(command, "ops", _cm.Mods);
        }

        private void IgnoreUserCommand(IDictionary<string, object> command)
        {
            if ((command["action"] as string) != "delete")
            {
                if (command.ContainsKey("character"))
                    AddToSomeListCommand(command, "character", _cm.Ignored);
                else // implicit, no need for if else check here
                    AddToSomeListCommand(command, "characters", _cm.Ignored);

                // todo: add notification for this
            }
            else // this makes unignore actually work
            {
                var toRemove = (_cm.Ignored.FirstOrDefault(ignore => ignore.Equals(command["character"] as string, StringComparison.OrdinalIgnoreCase)));
                if (toRemove != null)
                    _cm.Ignored.Remove(toRemove);

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

                if (!_cm.IsOnline(temp.Name))
                {
                    temp.Gender = ParseGender((string)character[1]); // character's gender
        
                    temp.Status = (StatusType)Enum.Parse(typeof(StatusType), (string)character[2]); // Character's status
                    temp.StatusMessage = (string)character[3]; // Character's status message

                    _cm.AddCharacter(temp); // also add it to the online characters collection
                }
            }
        }
        
        private void UserLoggedInCommand(IDictionary<string, object> command)
        {
            string character = (string)command["identity"];

            if (_cm.SelectedCharacter.Name.Equals(character, StringComparison.OrdinalIgnoreCase)) return;
            // we do not need to keep track of our own character in the character list

            if (!_cm.IsOnline(character))
            {
                ICharacter temp = new CharacterModel();
                temp.Name = character;
                temp.Gender = ParseGender((string)command["gender"]);
                temp.Status = (StatusType)Enum.Parse(typeof(StatusType), (string)command["status"]); // Character's status

                _cm.AddCharacter(temp); // also add it to the online characters collection

                _events.GetEvent<NewUpdateEvent>().Publish
                    (
                        new CharacterUpdateModel
                            (
                                temp,
                                new CharacterUpdateModel.LoginStateChangedEventArgs() { IsLogIn = true }
                            )
                    );
            }
        }

        private void PublicChannelListCommand(IDictionary<string, object> command)
        {
            ChannelListCommand(command, true);
            #region Default Channel Join
            foreach (var savedChannel in Models.ApplicationSettings.SavedChannels)
            {
                if (!string.IsNullOrWhiteSpace(savedChannel))
                {
                    object toSend = new { channel = savedChannel };
                    _connection.SendMessage(toSend, "JCH");
                }
            }
            #endregion
        }

        private void PrivateChannelListCommand(IDictionary<string, object> command)
        {
            ChannelListCommand(command, false);
        }

        private void StatusChangedCommand(IDictionary<string, object> command)
        {
            string character = (string)command["character"];
            StatusType status = (StatusType)Enum.Parse(typeof(StatusType), command["status"] as string);
            string statusMessage = (string)command["statusmsg"];

            if (_cm.IsOnline(character))
            {
                ICharacter temp = _cm.FindCharacter(character);
                bool statusChanged = false;
                bool statusMessageChanged = false;

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
                if (statusChanged || statusMessageChanged)
                {
                    var args = new Models.CharacterUpdateModel.StatusChangedEventArgs()
                    {
                        NewStatusType = (statusChanged ? status : StatusType.None),
                        NewStatusMessage = (statusMessageChanged ? statusMessage : null)
                    };

                    _events.GetEvent<NewUpdateEvent>().Publish(
                        new CharacterUpdateModel
                        (
                            temp,
                            args
                        )
                    );
                }
            }
        }

        private void AdMessageCommand(IDictionary<string, object> command)
        {
            MessageRecieved(command, true);
        }

        private void ChannelMessageCommand(IDictionary<string, object> command)
        {
            MessageRecieved(command, false);
        }

        private void PrivateMessageCommand(IDictionary<string, object> command)
        {
            var sender = (string)command["character"];
            if (!_cm.Ignored.Contains(sender))
            {
                if (_cm.CurrentPMs.FirstByIdOrDefault(sender) == null)
                    _manager.AddChannel(ChannelType.pm, sender);

                _manager.AddMessage(command["message"] as string, sender, sender);

                var temp = _cm.CurrentPMs.FirstByIdOrDefault(sender);
                if (temp == null) return;

                temp.TypingStatus = Typing_Status.clear; // webclient assumption
            }

            else
                _connection.SendMessage(new Dictionary<string, object> { { "action", "notify" }, { "character", sender }, { "type", "IGN" } });
        }

        private void TypingStatusCommand(IDictionary<string, object> command)
        {
            var sender = (string)command["character"];

            var channel = _cm.CurrentPMs.FirstByIdOrDefault(sender);
            if (channel != null)
            {
                    Typing_Status type = (Typing_Status)Enum.Parse(typeof(Typing_Status), (string)command["status"]);

                    if (channel.TypingStatus != type)
                        channel.TypingStatus = type;
            }
        }

        new private void JoinChannelCommand(IDictionary<string, object> command)
        {
            var title = (string)command["title"];
            var channelName = (string)command["channel"];

            IDictionary<string, object> id = (IDictionary<string, object>)command["character"];
            var identity = (string)id["identity"];

            // JCH is used in a few situations. It is used when others join a channel and when we join a channel

            // if this is a situation where we are joining a channel...
            var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelName);
            if (channel == null)
            {
                ChannelType kind = ChannelType.pub;
                if (channelName.Contains("ADH-")) kind = ChannelType.priv;

                _manager.JoinChannel(kind, channelName, title);
            }

            // if it isn't, then it must be when someone else is.
            else
            {
                if (!channel.Users.Any(user => user.Name.Equals(identity, StringComparison.OrdinalIgnoreCase))) // checking by name is safer
                {
                    channel.Users.Add(_cm.FindCharacter(identity));
                    channel.CallListChanged();

                    _events.GetEvent<NewUpdateEvent>().Publish // send the join/leave notification
                        (
                            new CharacterUpdateModel
                            (
                                _cm.FindCharacter(identity),
                                new CharacterUpdateModel.JoinLeaveEventArgs() { Joined = true, TargetChannel = channel.Title, TargetChannelID = channel.ID }
                            )
                        );
                }
            }
        }

        private void LeaveChannelCommand(IDictionary<string, object> command)
        {
            var channelName = (string)command["channel"];
            var characterName = (string)command["character"];

            if (_cm.SelectedCharacter.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase)) return;

            var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelName);
            if (channel != null)
            {
                var toRemove = channel.Users.FirstOrDefault(character => character.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
                if (toRemove != null)
                {
                    _events.GetEvent<NewUpdateEvent>().Publish // send the join/leave notification
                        (
                            new CharacterUpdateModel
                            (
                                toRemove,
                                new CharacterUpdateModel.JoinLeaveEventArgs() { Joined = false, TargetChannel = channel.Title, TargetChannelID = channel.ID }
                            )
                        );

                    channel.Users.Remove(toRemove);
                    channel.CallListChanged();
                }
                else
                    Debug.WriteLine("Cannot do LCH with character " + characterName);
            }
        }

        private void CharacterDisconnectCommand(IDictionary<string, object> command)
        {
            var characterName = (string)command["character"];

            if (_cm.IsOnline(characterName))
            {
                var character = _cm.FindCharacter(characterName);
                bool ofInterest = _cm.IsOfInterest(characterName);

                foreach (GeneralChannelModel chan in _cm.CurrentChannels.Where(param => param.Users.Contains(character)))
                {
                    if (!ofInterest) // show join/leave notifications for LCH if we won't get a 'x has logged out' notification
                        LeaveChannelCommand(new Dictionary<string, object>() { { "character", character.Name }, { "channel", chan.ID } });
                    else
                    {
                        chan.Users.Remove(character);
                        chan.CallListChanged();
                    }
                }

                var characterChannel = _cm.CurrentPMs.FirstByIdOrDefault(characterName);
                if (characterChannel != null)
                    characterChannel.TypingStatus = Typing_Status.clear; // fixes bug where character will still appear typing afte rlog out

                _cm.RemoveCharacter(characterName);
                _events.GetEvent<NewUpdateEvent>().Publish
                    (
                        new CharacterUpdateModel
                            (
                                character,
                                new CharacterUpdateModel.LoginStateChangedEventArgs() { IsLogIn = false }
                            )
                    );
            }
        }

        private void ChannelOperatorListCommand(IDictionary<string, object> command)
        {
            var channelName = (string)command["channel"];
            var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelName);

            if (channel == null) return;

            AddToSomeListCommand(command, "oplist", channel.Moderators);
            channel.CallListChanged();
        }

        private void ChannelInitializedCommand(IDictionary<string, object> command)
        {
            var channelName = (string)command["channel"];
            var mode = (ChannelMode)Enum.Parse(typeof(ChannelMode), command["mode"] as string);
            var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelName);

            channel.Mode = mode;
            dynamic users = command["users"]; // dynamic lets us deal with odd syntax
            foreach (IDictionary<string, object> character in users)
            {
                string name = character["identity"] as string;

                if (!channel.Users.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    && _cm.IsOnline(name))
                    )
                    channel.Users.Add(_cm.FindCharacter(name));
            }
        }

        private void ChannelDesciptionCommand(IDictionary<string, object> command)
        {
            var channelName = command["channel"];
            var channel = _cm.CurrentChannels.First((x) => x.ID == channelName as string);
            var description = command["description"];

            bool isInitializer = String.IsNullOrWhiteSpace(channel.MOTD);

            if (!isInitializer)
                channel.MOTD = description as string;
            else if (description.ToString().StartsWith("Welcome to your private room!")) // shhh go away lame init description
                channel.MOTD = "Man this description is lame. You should change it and make it amaaaaaazing. Click that pencil, man.";
            else
                channel.MOTD = description as string; // derpherp no channel description bug fix

            if (!isInitializer)
            {
                var args = new Models.ChannelUpdateModel.ChannelDescriptionChangedEventArgs();
                _events.GetEvent<NewUpdateEvent>().Publish(new ChannelUpdateModel(channel.ID, args, channel.Title));
            }
        }

        private void ErrorCommand(IDictionary<string, object> command)
        {
            // for some fucktarded reason room status changes are only done through SYS
            if ((command["message"] as string).IndexOf("this channel is now", StringComparison.OrdinalIgnoreCase) != -1)
            {
                RoomTypeChangedCommand(command);
                return;
            }

            // checks to ensure it's not a mod promote message
            if ((command["message"] as string).IndexOf("has been", StringComparison.OrdinalIgnoreCase) == -1)
                _events.GetEvent<ErrorEvent>().Publish(command["message"] as string);
        }

        private void InviteCommand(IDictionary<string, object> command)
        {
            var sender = command["sender"] as string;
            var channelID = command["name"] as string;
            var channelName = command["title"] as string;

            var args = new Models.ChannelUpdateModel.ChannelInviteEventArgs() { Inviter = sender };
            _events.GetEvent<NewUpdateEvent>().Publish(new ChannelUpdateModel(channelID, args, channelName));
        }

        new private void KickCommand(IDictionary<string, object> command)
        {
            var kicker = (string)command["operator"];
            var channelId = (string)command["channel"];
            var kicked = (string)command["character"];
            bool isBan = false;

            if ((string)command["command"] == "CBU")
                isBan = true;

            if (kicked.Equals(_cm.SelectedCharacter.Name, StringComparison.OrdinalIgnoreCase))
                kicked = "you";

            var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelId);

            var args = new Models.ChannelUpdateModel.ChannelDisciplineEventArgs()
            { 
                IsBan = isBan,
                Kicked = kicked,
                Kicker = kicker 
            };
            var update = new ChannelUpdateModel(channelId, args, channel.Title);

            _events.GetEvent<NewUpdateEvent>().Publish(update);

            if (kicked == "you")
                _manager.RemoveChannel(channelId);
        }

        private void RollCommand(IDictionary<string, object> command)
        {
            string channel = command["channel"] as string;
            string type = command["type"] as string;
            string message = command["message"] as string;
            string poster = command["character"] as string;

            if (!_cm.Ignored.Contains(poster))
                _manager.AddMessage(message, channel, poster, MessageType.roll);
        }

        private void OperatorPromoteCommand(IDictionary<string, object> command)
        {
            var target = command["character"] as string;
            string channelID = null;

            if (command.ContainsKey("channel"))
                channelID = command["channel"] as string;

            PromoteOrDemote(target, true, channelID);
        }

        private void OperatorDemoteCommand(IDictionary<string, object> command)
        {
            var target = command["character"] as string;
            string channelID = null;

            if (command.ContainsKey("channel"))
                channelID = command["channel"] as string;

            PromoteOrDemote(target, false, channelID);
        }

        private void RoomTypeChangedCommand(IDictionary<string, object> command)
        {
            var channelID = command["channel"] as string;
            var isPublic = (command["message"] as string).IndexOf("public", StringComparison.OrdinalIgnoreCase) != -1;

            var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelID);

            if (channel == null)
                return; // can't change the settings of a room we don't know

            if (isPublic) // room is now open
            {
                channel.Type = ChannelType.priv;

                _events.GetEvent<NewUpdateEvent>().Publish(
                    new ChannelUpdateModel(
                        channelID,
                        new ChannelUpdateModel.ChannelTypeChangedEventArgs() { IsOpen = true },
                        channel.Title)
                        );
            }
            else // room is closed
            {
                channel.Type = ChannelType.closed;

                _events.GetEvent<NewUpdateEvent>().Publish(
                    new ChannelUpdateModel(
                        channelID,
                        new ChannelUpdateModel.ChannelTypeChangedEventArgs() { IsOpen = false },
                        channel.Title)
                        );
            }
        }

        private void RoomModeChangedCommand(IDictionary<string, object> command)
        {
            var channelID = command["channel"] as string;
            var mode = command["mode"] as string;

            var newMode = (ChannelMode)Enum.Parse(typeof(Models.ChannelMode), mode);
            var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelID);

            if (channel != null)
            {
                channel.Mode = newMode;
                _events.GetEvent<NewUpdateEvent>().Publish(
                    new ChannelUpdateModel(
                        channel.ID,
                        new ChannelUpdateModel.ChannelModeUpdateEventArgs(){ NewMode = newMode, },
                        channel.Title
                    ));
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Get character gets the selected character info for storing elsewhere.
        /// </summary>
        private void getCharacter(string character)
        {
            _events.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(getCharacter);
            _cm.SelectedCharacter = new CharacterModel() { Name = character, Status = StatusType.online };
            _cm.SelectedCharacter.GetAvatar();

            Dispatcher.Invoke(
                (Action)delegate
                {
                    Application.Current.MainWindow.Title = String.Format("slimCat Caracal ({0})", character);
                });
        }

        /// <summary>
        /// This converts our multi-threaded command receiver into a single-threaded executor.
        /// </summary>
        private void EnqueAction(IDictionary<string, object> data)
        {
            _que.Add(data);

            DoAction();
        }

        private void DoAction()
        {
            lock (_que)
            {
                if (_que.Count > 0)
                {
                    var workingData = _que[0];
                    var toInvoke = InterpretCommand(workingData);
                    if (toInvoke != null)
                        toInvoke.Invoke(workingData);

                    _que.RemoveAt(0);

                    DoAction();
                    return;
                }
            }
        }
        #endregion
    }
}
