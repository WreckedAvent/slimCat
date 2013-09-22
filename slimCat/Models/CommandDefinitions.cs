namespace Slimcat.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     This class provides a list of possible commands and all possible information relating to those commands.
    ///     It also provides a method for creating a command.
    /// </summary>
    public static class CommandDefinitions
    {
        #region Constants

        /// <summary>
        ///     The client send channel ad.
        /// </summary>
        public const string ClientSendChannelAd = "_send_channel_ad";

        /// <summary>
        ///     The client send channel message.
        /// </summary>
        public const string ClientSendChannelMessage = "_send_channel_message";

        /// <summary>
        ///     The client send pm.
        /// </summary>
        public const string ClientSendPm = "_send_private_message";

        /// <summary>
        ///     The client send typing status.
        /// </summary>
        public const string ClientSendTypingStatus = "_send_typing_status";

        #endregion

        #region Static Fields

        /// <summary>
        ///     The command aliases.
        /// </summary>
        private static readonly IDictionary<string, string> CommandAliases = new Dictionary<string, string>
        {
            { "pm", "priv" }, 
            { "tell", "priv" }, 
            { "msg", "priv" }, 
            {
                // user commands
                "online", 
                "status"
            }, 
            { "away", "status" }, 
            { "busy", "status" }, 
            {
                "looking", 
                "status"
            }, 
            { "dnd", "status" }, 
            {
                // channel mod
                "cop", "promote"
            }, 
            {
                "cdeop", "demote"
            }, 
            {
                // admin - global mod
                "op", 
                "chatpromote"
            }, 
            {
                "deop", 
                "chatdemote"
            }, 
            {
                "gkick", 
                "chatkick"
            }, 
            {
                "createchannel", 
                "makechannel"
            }, 
            {
                "accountban", 
                "chatban"
            }, 
            {
                "hr", 
                "handlereport"
            }, 
            {
                "r", 
                "handlelatest"
            }, 
        };

        /// <summary>
        ///     The command overrides.
        /// </summary>
        private static readonly IDictionary<string, CommandOverride> CommandOverrides = new Dictionary<string, CommandOverride>
        {
            { // format ->
                // commmand to override, command parameter to override, value to override with
                "online", new CommandOverride("status", "online")
            }, 
            {
                "busy", new CommandOverride("status", "busy")
            }, 
            {
                "looking", new CommandOverride("status", "looking")
            }, 
            {
                "away", new CommandOverride("status", "away")
            }, 
            {
                "dnd", new CommandOverride("status", "dnd")
            }, 
            {
                "ignore", new CommandOverride("action", "add")
            }, 
            {
                "unignore", new CommandOverride("action", "delete")
            }, 
            {
                "openroom", new CommandOverride("status", "public")
            }, 
            {
                "closeroom", new CommandOverride("status", "private")
            }, 
            {
                "bottle", new CommandOverride("dice", "bottle")
            }, 
            {
                "report", new CommandOverride("action", "report")
            }, 
            {
                "handlereport", new CommandOverride("action", "confirm")
            }, 
        };

        /// <summary>
        ///     The commands.
        /// </summary>
        public static readonly IDictionary<string, CommandModel> Commands = new Dictionary<string, CommandModel>
        {
            // user commands
            { "addbookmark", new CommandModel("addbookmark", "bookmark-add", new[] { "name" }) },
            { "addfriend", new CommandModel("addfriend", "friend-add", new[] { "dest_name" }) },
            {
                "bottle",
                new CommandModel("bottle", "RLL", new[] { "dice" }, CommandModel.CommandTypes.SingleArgsAndChannel)
            },
            { "code", new CommandModel("code", "code", null, CommandModel.CommandTypes.NoArgs) },
            { "clear", new CommandModel("clear", "clear", null, CommandModel.CommandTypes.NoArgs) },
            { "clearall", new CommandModel("clearall", "clearall", null, CommandModel.CommandTypes.NoArgs) },
            { "close", new CommandModel("close", "close", new[] { "channel" }, CommandModel.CommandTypes.OnlyChannel) },
            {
                "ignore",
                new CommandModel("ignore", "IGN", new[] { "character", "action" }, CommandModel.CommandTypes.TwoArgs)
            },
            { "interesting", new CommandModel("interesting", "interesting", new[] { "character" }) },
            {
                "invite",
                new CommandModel("invite", "CIU", new[] { "character" }, CommandModel.CommandTypes.SingleArgsAndChannel)
            },
            { "join", new CommandModel("join", "join", new[] { "channel" }) },
            {
                "lastupdate",
                new CommandModel("lastupdate", "_snap_to_last_update", null, CommandModel.CommandTypes.NoArgs)
            },
            { "logheader", new CommandModel("logheader", "_logger_new_header", new[] { "title" }) },
            { "logsection", new CommandModel("logsection", "_logger_new_section", new[] { "title" }) },
            { "lognewline", new CommandModel("lognewline", "_logger_new_line", null, CommandModel.CommandTypes.NoArgs) },
            { "makeroom", new CommandModel("makeroom", "CCR", new[] { "channel" }) },
            { "notinteresting", new CommandModel("notinteresting", "notinteresting", new[] { "character" }) },
            { "openlog", new CommandModel("openlog", "_logger_open_log", null, CommandModel.CommandTypes.OnlyChannel) },
            {
                "openlogfolder",
                new CommandModel("openlogfolder", "_logger_open_folder", null, CommandModel.CommandTypes.OnlyChannel)
            },
            { "priv", new CommandModel("priv", "priv", new[] { "character" }) },
            { "removebookmark", new CommandModel("removebookmark", "bookmark-remove", new[] { "name" }) },
            { "removefriend", new CommandModel("removefriend", "friend-remove", new[] { "dest_name" }) },
            {
                "roll", new CommandModel("roll", "RLL", new[] { "dice" }, CommandModel.CommandTypes.SingleArgsAndChannel)
            },
            {
                "report",
                new CommandModel("report", "SFC", new[] { "name", "report" }, CommandModel.CommandTypes.TwoArgsAndChannel)
            },
            {
                "status",
                new CommandModel("status", "STA", new[] { "status", "statusmsg" }, CommandModel.CommandTypes.TwoArgs)
            },
            { "tempignore", new CommandModel("tempignore", "tempignore", new[] { "character" }) },
            { "tempunignore", new CommandModel("tempunignore", "tempunignore", new[] { "character" }) },
            {
                "unignore",
                new CommandModel("unignore", "IGN", new[] { "character", "action" }, CommandModel.CommandTypes.TwoArgs)
            },
            { "who", new CommandModel("who", "who", null, CommandModel.CommandTypes.NoArgs) },

            // channel moderator commands
            {
                "ban",
                new CommandModel(
                "ban",
                "CBU",
                new[] { "character" },
                CommandModel.CommandTypes.SingleArgsAndChannel,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "banlist",
                new CommandModel(
                "banlist", "CBL", null, CommandModel.CommandTypes.OnlyChannel, CommandModel.PermissionLevel.Moderator)
            },
            {
                "closeroom",
                new CommandModel(
                "closeroom",
                "RST",
                new[] { "status" },
                CommandModel.CommandTypes.OnlyChannel,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "demote",
                new CommandModel(
                "demote",
                "COR",
                new[] { "character" },
                CommandModel.CommandTypes.SingleArgsAndChannel,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "getdescription",
                new CommandModel(
                "getdescription",
                "getdescription",
                null,
                CommandModel.CommandTypes.NoArgs,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "kick",
                new CommandModel(
                "kick",
                "CKU",
                new[] { "character" },
                CommandModel.CommandTypes.SingleArgsAndChannel,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "openroom",
                new CommandModel(
                "openroom",
                "RST",
                new[] { "status" },
                CommandModel.CommandTypes.OnlyChannel,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "promote",
                new CommandModel(
                "promote",
                "COA",
                new[] { "character" },
                CommandModel.CommandTypes.SingleArgsAndChannel,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "setdescription",
                new CommandModel(
                "setdescription",
                "CDS",
                new[] { "description" },
                CommandModel.CommandTypes.SingleArgsAndChannel,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "unban",
                new CommandModel(
                "unban",
                "CUB",
                new[] { "character" },
                CommandModel.CommandTypes.SingleArgsAndChannel,
                CommandModel.PermissionLevel.Moderator)
            },
            {
                "setmode",
                new CommandModel(
                "setmode",
                "RMO",
                new[] { "mode" },
                CommandModel.CommandTypes.SingleArgsAndChannel,
                CommandModel.PermissionLevel.Moderator)
            },

            // global op commands
            {
                "chatban",
                new CommandModel(
                "chatban",
                "ACB",
                new[] { "character" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "chatkick",
                new CommandModel(
                "chatkick",
                "KIK",
                new[] { "character" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "chatunban",
                new CommandModel(
                "chatunban",
                "UBN",
                new[] { "character" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "reward",
                new CommandModel(
                "reward",
                "RWD",
                new[] { "character" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "timeout",
                new CommandModel(
                "timeout",
                "TMO",
                new[] { "character" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "handlereport",
                new CommandModel(
                "handlereport",
                "handlereport",
                new[] { "name" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "handlelatest",
                new CommandModel(
                "handlelatest",
                "handlelatest",
                null,
                CommandModel.CommandTypes.NoArgs,
                CommandModel.PermissionLevel.GlobalMod)
            },
            
            // admin commands
            {
                "broadcast",
                new CommandModel(
                "broadcast",
                "BRO",
                new[] { "character" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "chatdemote",
                new CommandModel(
                "chatdemote",
                "DOP",
                new[] { "character" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "chatpromote",
                new CommandModel(
                "chatpromote",
                "AOP",
                new[] { "character" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },
            {
                "makechannel",
                new CommandModel(
                "makechannel",
                "CRC",
                new[] { "channel" },
                CommandModel.CommandTypes.SingleSentence,
                CommandModel.PermissionLevel.GlobalMod)
            },

            // client-only commands
            {
                ClientSendTypingStatus,
                new CommandModel(
                ClientSendTypingStatus, "TPN", new[] { "status", "character" }, CommandModel.CommandTypes.TwoArgs)
            },
            {
                ClientSendPm,
                new CommandModel(ClientSendPm, "PRI", new[] { "message", "recipient" }, CommandModel.CommandTypes.TwoArgs)
            },
            {
                ClientSendChannelMessage,
                new CommandModel(
                ClientSendChannelMessage, "MSG", new[] { "message" }, CommandModel.CommandTypes.SingleArgsAndChannel)
            },
            {
                ClientSendChannelAd,
                new CommandModel(
                ClientSendChannelAd, "LRP", new[] { "message" }, CommandModel.CommandTypes.SingleArgsAndChannel)
            },
        };

        /// <summary>
        ///     The non command commands.
        /// </summary>
        public static string[] NonCommandCommands = new[] 
        { // prevents long ugly checking in our viewmodels for these
            "/me", "/warn", "/post" 
        };

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Creates a command complete with its meta data.
        /// </summary>
        /// <param name="familiarName">
        /// The familiar name.
        /// </param>
        /// <param name="args">
        /// The arguments of the command.
        /// </param>
        /// <param name="channel">
        /// The channel, if applicable.
        /// </param>
        /// <returns>
        /// The <see cref="CommandDataModel"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static CommandDataModel CreateCommand(
            string familiarName, IList<string> args = null, string channel = null)
        {
            if (!IsValidCommand(familiarName))
            {
                throw new ArgumentException("Unknown command.", "familiarName");
            }

            if (HasCommandOverride(familiarName))
            {
                var model = GetCommandModelFromName(familiarName);
                var overide = CommandOverrides[familiarName];

                // this inserts the override into the correct location for the toDictionary method
                var overrideArg = overide.ArgumentName;
                var position = model.ArgumentNames.IndexOf(overrideArg);

                if (args == null)
                {
                    args = new List<string>();
                }

                if (position != -1 && !(position > args.Count))
                {
                    args.Insert(position, overide.ArgumentValue);
                }
                else
                {
                    args.Add(overide.ArgumentValue);
                }

                return new CommandDataModel(model, args.ToArray(), channel);
            }
            else
            {
                var model = GetCommandModelFromName(familiarName);

                // basic syntax validation
                switch (model.CommandType)
                {
                    case CommandModel.CommandTypes.SingleWord:
                        if (args != null && args.Count > 1)
                        {
                            throw new ArgumentException(
                                "This command takes a parameter which must be a single word", "args");
                        }

                        break;

                    case CommandModel.CommandTypes.OnlyChannel:
                    case CommandModel.CommandTypes.NoArgs:
                        if (args != null && args[0] != null)
                        {
                            throw new ArgumentException("This command doesn't take an argument", "args");
                        }

                        break;

                    case CommandModel.CommandTypes.TwoArgs:
                        if (args != null && args.Count > 2)
                        {
                            throw new ArgumentException("This command takes only two parameters", "args");
                        }

                        break;

                    case CommandModel.CommandTypes.SingleArgsAndChannel:
                        if (channel == null)
                        {
                            throw new ArgumentException("This command needs an associated channel", "args");
                        }

                        break;
                }

                return new CommandDataModel(model, args == null ? null : args.ToArray(), channel);
            }
        }

        /// <summary>
        /// The get command model from name.
        /// </summary>
        /// <param name="familiarName">
        /// The familiar name.
        /// </param>
        /// <returns>
        /// The <see cref="CommandModel"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static CommandModel GetCommandModelFromName(string familiarName)
        {
            if (!IsValidCommand(familiarName))
            {
                throw new ArgumentException("Unknown command", "familiarName");
            }

            if (CommandAliases.ContainsKey(familiarName))
            {
                familiarName = CommandAliases[familiarName];
            }

            return Commands[familiarName];
        }

        /// <summary>
        /// The is valid command.
        /// </summary>
        /// <param name="familiarName">
        /// The familiar name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsValidCommand(string familiarName)
        {
            return CommandAliases.ContainsKey(familiarName) || Commands.ContainsKey(familiarName);
        }

        /// <summary>
        /// The has command override.
        /// </summary>
        /// <param name="familiarName">
        /// The familiar name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool HasCommandOverride(string familiarName)
        {
            return CommandOverrides.ContainsKey(familiarName);
        }
        #endregion

        /// <summary>
        /// Represents a command argument override.
        /// </summary>
        private struct CommandOverride
        {
            #region Fields

            /// <summary>
            ///     The argument name.
            /// </summary>
            public readonly string ArgumentName;

            /// <summary>
            ///     The argument value.
            /// </summary>
            public readonly string ArgumentValue;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="CommandOverride"/> struct.
            /// </summary>
            /// <param name="argName">
            /// The arg name.
            /// </param>
            /// <param name="argValue">
            /// The arg value.
            /// </param>
            public CommandOverride(string argName, string argValue)
            {
                this.ArgumentName = argName;
                this.ArgumentValue = argValue;
            }

            #endregion
        }
    }
}