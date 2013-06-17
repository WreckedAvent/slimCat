/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models
{
    /// <summary>
    /// Represents metadata about a command
    /// </summary>
    public class CommandModel
    {
        #region Enums
        public enum CommandTypes
        {
            NoArgs,
            SingleArgsStrict,
            SingleArgsLoose,
            SingleArgsAndChannel,
            OnlyChannel,
            TwoArgs,
        }

        public enum PermissionLevel
        {
            User,
            Moderator,
            GlobalMod,
            Admin,
        }
        #endregion

        #region Fields
        private readonly string _famName;
        private readonly string _serName;
        private readonly CommandTypes _type;
        private readonly PermissionLevel _perm;
        private readonly IList<string> _argNames;
        #endregion

        #region Constructors
        public CommandModel(string familarName,
            string serverName,
            IList<string> paramaterNames = null,
            CommandTypes typeOfCommand = CommandTypes.SingleArgsLoose,
            PermissionLevel permissionLevel = PermissionLevel.User)
        {
            _famName = familarName;
            _serName = serverName;
            _type = typeOfCommand;
            _perm = permissionLevel;

            _argNames = paramaterNames;
        }
        #endregion

        #region Properties
        /// <summary>
        /// What the command is labeled as
        /// </summary>
        public string FamiliarName { get { return _famName; } }

        /// <summary>
        /// What the server or our service layer will recognize
        /// </summary>
        public string ServerName { get { return _serName; } }

        /// <summary>
        /// The permissions required to use this command
        /// </summary>
        public PermissionLevel PermissionsLevel { get { return _perm; } }

        /// <summary>
        /// What kind of command it is
        /// </summary>
        public CommandTypes CommandType { get { return _type; } }

        /// <summary>
        /// The names of the arguments it has
        /// </summary>
        public IList<string> ArgumentNames { get { return _argNames; } }
        #endregion
    }

    /// <summary>
    /// Represents a command with its meta data
    /// </summary>
    public struct CommandDataModel
    {
        public readonly IList<string> Arguments;
        public readonly CommandModel CommandInformation;
        public readonly string ChannelName;

        public CommandDataModel(CommandModel info, IList<string> args, string channelName)
        {
            this.CommandInformation = info;
            this.Arguments = args;
            this.ChannelName = channelName;
        }

        public IDictionary<string, object> toDictionary()
        {
            var toSend = new Dictionary<string, object>()
            {
                {"type", CommandInformation.ServerName},
            };

            if (Arguments != null && Arguments[0] != null)
            {
                int count = 0;
                foreach (string argumentName in CommandInformation.ArgumentNames)
                {
                    toSend.Add(argumentName, Arguments[count]);
                    count++;
                }
            }

            bool isChannelCommand =
                (CommandInformation.CommandType == CommandModel.CommandTypes.SingleArgsAndChannel
                || CommandInformation.CommandType == CommandModel.CommandTypes.OnlyChannel);

            if (ChannelName != null && isChannelCommand)
                toSend.Add("channel", ChannelName);

            return toSend;
        }
    }

    /// <summary>
    /// This class provides a list of possible commands and all possible information relating to those commands.
    /// It also provides a method for creating a command.
    /// </summary>
    public static class CommandDefinitions
    {
        static CommandDefinitions() { }

        public const string ClientSendPM = "_send_private_message";
        public const string ClientSendChannelMessage = "_send_channel_message";
        public const string ClientSendChannelAd = "_send_channel_ad";
        public const string ClientSendTypingStatus = "_send_typing_status";

        public static IDictionary<string, CommandModel> Commands = new Dictionary<string, CommandModel>()
        {
            // user commands
            {"addbookmark", new CommandModel("addbookmark", "bookmark-add", new [] {"name"})},
            {"addfriend", new CommandModel("addfriend", "friend-add", new [] {"dest_name"})},
            {"bottle", new CommandModel("bottle", "RLL", new [] {"dice"}, CommandModel.CommandTypes.SingleArgsAndChannel)},
            {"code", new CommandModel("code", "code", null, CommandModel.CommandTypes.NoArgs)},
            {"clear", new CommandModel("clear", "clear", null, CommandModel.CommandTypes.NoArgs)},
            {"clearall", new CommandModel("clearall", "clearall", null, CommandModel.CommandTypes.NoArgs)},
            {"close", new CommandModel("close", "close", new [] {"channel"}, CommandModel.CommandTypes.OnlyChannel)},
            {"handlereport", new CommandModel("handlereport", "SFC", new [] {"callid", "action"}, CommandModel.CommandTypes.TwoArgs)},
            {"ignore", new CommandModel("ignore", "IGN", new [] {"character", "action"}, CommandModel.CommandTypes.TwoArgs)},
            {"interesting", new CommandModel("interesting", "interesting", new [] {"character"})},
            {"invite", new CommandModel("invite", "CIU", new [] {"character"}, CommandModel.CommandTypes.SingleArgsAndChannel)},
            {"join", new CommandModel("join", "join", new [] {"channel"})},
            {"lastupdate", new CommandModel("lastupdate", "_snap_to_last_update", null, CommandModel.CommandTypes.NoArgs)},
            {"logheader", new CommandModel("logheader", "_logger_new_header", new [] {"title"})},
            {"logsection", new CommandModel("logsection", "_logger_new_section", new [] {"title"})},
            {"lognewline", new CommandModel("lognewline", "_logger_new_line", null, CommandModel.CommandTypes.NoArgs)},
            {"makeroom", new CommandModel("makeroom", "CCR", new [] {"channel"})},
            {"notinteresting", new CommandModel("notinteresting", "notinteresting", new [] {"character"})},
            {"openlog", new CommandModel("openlog", "_logger_open_log", null, CommandModel.CommandTypes.NoArgs)},
            {"openlogfolder", new CommandModel("openlogfolder", "_logger_open_folder", null, CommandModel.CommandTypes.NoArgs)},
            {"priv", new CommandModel("priv", "priv", new [] {"character"})},
            {"removebookmark", new CommandModel("removebookmark", "bookmark-remove", new [] {"name"})},
            {"removefriend", new CommandModel("removefriend", "friend-remove", new [] {"dest_name"})},
            {"roll", new CommandModel("roll", "RLL", new [] {"dice"}, CommandModel.CommandTypes.SingleArgsAndChannel)}, 
            {"report", new CommandModel("report", "SFC", new [] {"name", "action"}, CommandModel.CommandTypes.TwoArgs)},
            {"status", new CommandModel("status", "STA", new [] {"status", "statusmsg"}, CommandModel.CommandTypes.TwoArgs)},
            {"tempignore", new CommandModel("tempignore", "tempignore", new [] {"character"})},
            {"tempunignore", new CommandModel("tempunignore", "tempunignore", new [] {"character"})},
            {"unignore", new CommandModel("unignore", "IGN", new [] {"character", "action"}, CommandModel.CommandTypes.TwoArgs)},
            {"who", new CommandModel("who", "who", null, CommandModel.CommandTypes.NoArgs)},

            // channel moderator commands
            {"ban", new CommandModel("ban", "CBU", new [] {"character"}, CommandModel.CommandTypes.SingleArgsAndChannel, CommandModel.PermissionLevel.Moderator)},
            {"banlist", new CommandModel("banlist", "CBL", null, CommandModel.CommandTypes.OnlyChannel, CommandModel.PermissionLevel.Moderator)},
            {"closeroom", new CommandModel("closeroom", "RST", new [] { "status" }, CommandModel.CommandTypes.OnlyChannel, CommandModel.PermissionLevel.Moderator)},
            {"demote", new CommandModel("demote", "COR", new [] {"character"}, CommandModel.CommandTypes.SingleArgsAndChannel, CommandModel.PermissionLevel.Moderator)},
            {"getdescription", new CommandModel("getdescription", "getdescription", null, CommandModel.CommandTypes.NoArgs, CommandModel.PermissionLevel.Moderator)},
            {"kick", new CommandModel("kick", "CKU", new [] {"character"}, CommandModel.CommandTypes.SingleArgsAndChannel, CommandModel.PermissionLevel.Moderator)},
            {"openroom", new CommandModel("openroom", "RST", new [] {"status"}, CommandModel.CommandTypes.OnlyChannel, CommandModel.PermissionLevel.Moderator)},
            {"promote", new CommandModel("promote", "COA", new [] {"character"}, CommandModel.CommandTypes.SingleArgsAndChannel, CommandModel.PermissionLevel.Moderator)},    
            {"setdescription", new CommandModel("setdescription", "CDS", new [] {"description"}, CommandModel.CommandTypes.SingleArgsAndChannel, CommandModel.PermissionLevel.Moderator)},
            {"unban", new CommandModel("unban", "CUB", new [] {"character"}, CommandModel.CommandTypes.SingleArgsAndChannel, CommandModel.PermissionLevel.Moderator)},
            {"setmode", new CommandModel("setmode", "RMO", new [] {"mode"}, CommandModel.CommandTypes.SingleArgsAndChannel, CommandModel.PermissionLevel.Moderator)},

            // global op commands
            {"chatban", new CommandModel("chatban", "ACB", new [] {"character"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},
            {"chatkick", new CommandModel("chatkick", "KIK", new [] {"character"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},
            {"chatunban", new CommandModel("chatunban", "UBN", new [] {"character"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},
            {"reward", new CommandModel("reward", "RWD", new [] {"character"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},
            {"timeout", new CommandModel("timeout", "TMO", new [] {"character"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},

            // admin commands
            {"broadcast", new CommandModel("broadcast", "BRO", new [] {"character"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},
            {"chatdemote", new CommandModel("chatdemote", "DOP", new [] {"character"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},
            {"chatpromote", new CommandModel("chatpromote", "AOP", new [] {"character"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},
            {"makechannel", new CommandModel("makechannel", "CRC", new [] {"channel"}, CommandModel.CommandTypes.SingleArgsLoose, CommandModel.PermissionLevel.GlobalMod)},

            // client-only commands
            {ClientSendTypingStatus, new CommandModel(ClientSendTypingStatus, "TPN", new [] {"status", "character"}, CommandModel.CommandTypes.TwoArgs)},
            {ClientSendPM, new CommandModel(ClientSendPM, "PRI", new [] {"message", "recipient"}, CommandModel.CommandTypes.TwoArgs)},
            {ClientSendChannelMessage, new CommandModel(ClientSendChannelMessage, "MSG", new [] {"message"}, CommandModel.CommandTypes.SingleArgsAndChannel)},
            {ClientSendChannelAd, new CommandModel(ClientSendChannelAd, "LRP", new [] {"message"}, CommandModel.CommandTypes.SingleArgsAndChannel)},
        };

        public static IDictionary<string, string> CommandAliases = new Dictionary<string, string>()
        {
            // user commands
            {"pm", "priv"},
            {"tell", "priv"},
            {"msg", "priv"},
            {"online", "status"},
            {"away", "status"},
            {"busy", "status"},
            {"looking", "status"},
            {"dnd", "status"},

            // channel mod
            {"cop", "promote"},
            {"cdeop", "demote"},

            // admin - global mod
            {"op", "chatpromote"},
            {"deop", "chatdemote"},
            {"gkick", "chatkick"},
            {"createchannel", "makechannel"},
            {"accountban", "chatban"},
        };

        public static IDictionary<string, CommandOverride> CommandOverrides = new Dictionary<string, CommandOverride>()
        {
            // format ->
            // commmand to override, command parameter to override, value to override with
            {"online", new CommandOverride("status", "online")},
            {"busy", new CommandOverride("status", "busy")},
            {"looking", new CommandOverride("status", "looking")},
            {"away", new CommandOverride("status", "away")},
            {"dnd", new CommandOverride("status", "dnd")},
            {"ignore", new CommandOverride("action", "add")},
            {"unignore", new CommandOverride("action", "delete")},
            {"openroom", new CommandOverride("status", "public")},
            {"closeroom", new CommandOverride("status", "private")},
            {"bottle", new CommandOverride("dice", "bottle")},
            {"report", new CommandOverride("action", "report")},
            {"handlereport", new CommandOverride("action", "confirm")},
        };

        public static string[] NonCommandCommands = new []
        {
            // prevents long ugly checking in our viewmodels for these
            "/me",
            "/warn",
            "/post",
        };

        #region Methods
        public static bool HasCommandOverride(string familiarName)
        {
            return (CommandOverrides.ContainsKey(familiarName));
        }

        public static bool IsValidCommand(string familiarName)
        {
            return (CommandAliases.ContainsKey(familiarName) || Commands.ContainsKey(familiarName));
        }

        public struct CommandOverride
        {
            public readonly string ArgumentName, ArgumentValue;

            public CommandOverride(string argName, string argValue)
            {
                this.ArgumentName = argName;
                this.ArgumentValue = argValue;
            }
        }

        public static CommandModel GetCommandModelFromName(string familiarName)
        {
            if (IsValidCommand(familiarName))
            {
                if (CommandAliases.ContainsKey(familiarName))
                    familiarName = CommandAliases[familiarName];
                return Commands[familiarName];
            }
            else
                throw new ArgumentException("Unknown command", "familiarName");
        }

        public static CommandDataModel CreateCommand(string familiarName, IList<string> args = null, string channel = null)
        {
            if (IsValidCommand(familiarName))
            {
                if (!HasCommandOverride(familiarName))
                {
                    CommandModel model = GetCommandModelFromName(familiarName);

                    // basic syntax validation
                    switch (model.CommandType)
                    {
                        case CommandModel.CommandTypes.SingleArgsStrict:
                            if (args.Count > 1) throw new ArgumentException("This command takes a parameter which must be a single word", "args"); break;

                        case CommandModel.CommandTypes.OnlyChannel:
                        case CommandModel.CommandTypes.NoArgs:
                            if (args != null && args[0] != null) throw new ArgumentException("This command doesn't take an argument", "args"); break;

                        case CommandModel.CommandTypes.TwoArgs:
                            if (args.Count > 2) throw new ArgumentException("This command takes only two parameters", "args"); break;

                        case CommandModel.CommandTypes.SingleArgsAndChannel:
                            if (channel == null) throw new ArgumentException("This command needs an associated channel", "args"); break;

                        default: break;
                    }

                    return new CommandDataModel(model, (args == null ? null : args.ToArray()), channel);
                }
                else
                {
                    CommandModel model = GetCommandModelFromName(familiarName);
                    CommandOverride overide = CommandOverrides[familiarName];

                    // this inserts the override into the correct location for the toDictionary method
                    string overrideArg = overide.ArgumentName;
                    int position = model.ArgumentNames.IndexOf(overrideArg);

                    if (args == null)
                        args = new List<string>();

                    if (position != -1 && !(position > args.Count))
                        args.Insert(position, overide.ArgumentValue);
                    else
                        args.Add(overide.ArgumentValue);
                    //

                    return new CommandDataModel(model, args.ToArray(), channel);
                }
            }

            else
                throw new ArgumentException("Unknown command.", "familiarName");
        }
        #endregion
    }
}
