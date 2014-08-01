#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandDefinitions.cs">
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

namespace slimCat.Models
{
    #region Usings

    // I know these are bad names, but it makes the giant list below more legible
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utilities;
    using C = Utilities.Constants.ClientCommands;
    using A = Utilities.Constants.Arguments;

    #endregion

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
        ///     The commands.
        /// </summary>
        public static readonly IDictionary<string, CommandModel> Commands = new Dictionary<string, CommandModel>
            {
                // user commands
                {"addbookmark", new CommandModel("addbookmark", "bookmark-add", new[] {A.Name})},
                {"addfriend", new CommandModel("addfriend", "friend-add", new[] {"dest_name"})},
                {
                    "bottle",
                    new CommandModel("bottle", C.ChannelRoll, new[] {"dice"},
                        CommandModel.CommandTypes.SingleArgsAndChannel)
                },
                {"code", new CommandModel("code", "code", null, CommandModel.CommandTypes.NoArgs)},
                {"clear", new CommandModel("clear", "clear", null, CommandModel.CommandTypes.NoArgs)},
                {"clearall", new CommandModel("clearall", "clearall", null, CommandModel.CommandTypes.NoArgs)},
                {"close", new CommandModel("close", "close", new[] {A.Channel}, CommandModel.CommandTypes.OnlyChannel)},
                {"forceclose", new CommandModel("forceclose", "forceclose", new [] {A.Channel}) },
                {
                    "ignore",
                    new CommandModel("ignore", C.UserIgnore, new[] {A.Character, A.Action},
                        CommandModel.CommandTypes.TwoArgs)
                },
                {"ignoreUpdates", new CommandModel("ignoreUpdates", "ignoreUpdates", new[] {A.Character})},
                {"interesting", new CommandModel("interesting", "interesting", new[] {A.Character})},
                {
                    "invite",
                    new CommandModel("invite", C.UserInvite, new[] {A.Character},
                        CommandModel.CommandTypes.SingleArgsAndChannel)
                },
                {"join", new CommandModel("join", "join", new[] {A.Channel})},
                {
                    "lastupdate",
                    new CommandModel("lastupdate", "_snap_to_last_update", null, CommandModel.CommandTypes.NoArgs)
                },
                {"logheader", new CommandModel("logheader", "_logger_new_header", new[] {"title"})},
                {"logsection", new CommandModel("logsection", "_logger_new_section", new[] {"title"})},
                {
                    "lognewline",
                    new CommandModel("lognewline", "_logger_new_line", null, CommandModel.CommandTypes.NoArgs)
                },
                {"makeroom", new CommandModel("makeroom", C.ChannelCreate, new[] {A.Channel})},
                {"notinteresting", new CommandModel("notinteresting", "notinteresting", new[] {A.Character})},
                {
                    "openlog",
                    new CommandModel("openlog", "_logger_open_log", null, CommandModel.CommandTypes.OnlyChannel)
                },
                {
                    "openlogfolder",
                    new CommandModel("openlogfolder", "_logger_open_folder", null, CommandModel.CommandTypes.OnlyChannel)
                },
                {"priv", new CommandModel("priv", "priv", new[] {A.Character})},
                {"rejoin", new CommandModel("rejoin", "rejoin", new [] {A.Channel}) },
                {"removebookmark", new CommandModel("removebookmark", "bookmark-remove", new[] {A.Name})},
                {"removefriend", new CommandModel("removefriend", "friend-remove", new[] {"dest_name"})},
                {
                    "roll",
                    new CommandModel("roll", C.ChannelRoll, new[] {"dice"},
                        CommandModel.CommandTypes.SingleArgsAndChannel)
                },
                {
                    "report",
                    new CommandModel("report", C.AdminAlert, new[] {A.Name, A.Report},
                        CommandModel.CommandTypes.TwoArgsAndChannel)
                },
                {
                    "status",
                    new CommandModel("status", C.UserStatus, new[] {A.Status, A.StatusMessage},
                        CommandModel.CommandTypes.TwoArgs)
                },
                {"tempignore", new CommandModel("tempignore", "tempignore", new[] {A.Character})},
                {"tempunignore", new CommandModel("tempunignore", "tempunignore", new[] {A.Character})},
                {"tempinteresting", new CommandModel("tempinteresting", "tempinteresting", new[] {A.Character})},
                {
                    "tempnotinteresting", new CommandModel("tempnotinteresting", "tempnotinteresting", new[] {A.Character})
                },
                {
                    "unignore",
                    new CommandModel("unignore", C.UserIgnore, new[] {A.Character, A.Action},
                        CommandModel.CommandTypes.TwoArgs)
                },
                {"who", new CommandModel("who", "who", null, CommandModel.CommandTypes.NoArgs)},

                // channel moderator commands
                {
                    "ban",
                    new CommandModel(
                        "ban",
                        C.ChannelBan,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleArgsAndChannel,
                        CommandModel.PermissionLevel.Moderator)
                },
                {
                    "banlist",
                    new CommandModel(
                        "banlist", C.ChannelBanList, null, CommandModel.CommandTypes.OnlyChannel,
                        CommandModel.PermissionLevel.Moderator)
                },
                {
                    "closeroom",
                    new CommandModel(
                        "closeroom",
                        C.ChannelKind,
                        new[] {"status"},
                        CommandModel.CommandTypes.OnlyChannel,
                        CommandModel.PermissionLevel.Moderator)
                },
                {
                    "demote",
                    new CommandModel(
                        "demote",
                        C.ChannelDemote,
                        new[] {A.Character},
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
                        C.ChannelKick,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleArgsAndChannel,
                        CommandModel.PermissionLevel.Moderator)
                },
                {
                    "openroom",
                    new CommandModel(
                        "openroom",
                        C.ChannelKind,
                        new[] {"status"},
                        CommandModel.CommandTypes.OnlyChannel,
                        CommandModel.PermissionLevel.Moderator)
                },
                {
                    "promote",
                    new CommandModel(
                        "promote",
                        C.ChannelPromote,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleArgsAndChannel,
                        CommandModel.PermissionLevel.Moderator)
                },
                {
                    "setdescription",
                    new CommandModel(
                        "setdescription",
                        C.ChannelDescription,
                        new[] {"description"},
                        CommandModel.CommandTypes.SingleArgsAndChannel,
                        CommandModel.PermissionLevel.Moderator)
                },
                {
                    "unban",
                    new CommandModel(
                        "unban",
                        C.ChannelUnban,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleArgsAndChannel,
                        CommandModel.PermissionLevel.Moderator)
                },
                {
                    "setmode",
                    new CommandModel(
                        "setmode",
                        C.ChannelMode,
                        new[] {A.Mode},
                        CommandModel.CommandTypes.SingleArgsAndChannel,
                        CommandModel.PermissionLevel.Moderator)
                },

                // global op commands
                {
                    "chatban",
                    new CommandModel(
                        "chatban",
                        C.AdminBan,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },
                {
                    "chatkick",
                    new CommandModel(
                        "chatkick",
                        C.AdminKick,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },
                {
                    "chatunban",
                    new CommandModel(
                        "chatunban",
                        C.AdminUnban,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },
                {
                    "reward",
                    new CommandModel(
                        "reward",
                        C.AdminReward,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },
                {
                    "timeout",
                    new CommandModel(
                        "timeout",
                        C.ChannelTimeOut,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },
                {
                    "handlereport",
                    new CommandModel(
                        "handlereport",
                        "handlereport",
                        new[] {A.Name},
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
                        C.AdminBroadcast,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },
                {
                    "chatdemote",
                    new CommandModel(
                        "chatdemote",
                        C.AdminDemote,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },
                {
                    "chatpromote",
                    new CommandModel(
                        "chatpromote",
                        C.AdminPromote,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },
                {
                    "makechannel",
                    new CommandModel(
                        "makechannel",
                        C.SystemChannelCreate,
                        new[] {A.Character},
                        CommandModel.CommandTypes.SingleSentence,
                        CommandModel.PermissionLevel.GlobalMod)
                },

                // client-only commands
                {
                    ClientSendTypingStatus,
                    new CommandModel(
                        ClientSendTypingStatus, "TPN", new[] {"status", A.Character}, CommandModel.CommandTypes.TwoArgs)
                },
                {
                    ClientSendPm,
                    new CommandModel(ClientSendPm, "PRI", new[] {A.Message, "recipient"},
                        CommandModel.CommandTypes.TwoArgs)
                },
                {
                    ClientSendChannelMessage,
                    new CommandModel(
                        ClientSendChannelMessage, "MSG", new[] {A.Message},
                        CommandModel.CommandTypes.SingleArgsAndChannel)
                },
                {
                    ClientSendChannelAd,
                    new CommandModel(
                        ClientSendChannelAd, C.ChannelAd, new[] {A.Message},
                        CommandModel.CommandTypes.SingleArgsAndChannel)
                },
            };

        /// <summary>
        ///     The non command commands.
        /// </summary>
        public static readonly string[] NonCommandCommands =
            {
                // prevents long ugly checking in our viewmodels for these
                "/me", "/warn", "/post"
            };

        /// <summary>
        ///     The command aliases.
        /// </summary>
        private static readonly IDictionary<string, string> CommandAliases =
            new Dictionary<string, string>
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
                    {"idle", "status"},

                    // channel mod
                    {"cop", "promote"},
                    {"cdeop", "demote"},

                    // admin - global mod
                    {"op", "chatpromote"},
                    {"deop", "chatdemote"},
                    {"gkick", "chatkick"},
                    {"createchannel", "makechannel"},
                    {"accountban", "chatban"},
                    {"hr", "handlereport"},
                    {"r", "handlelatest"},
                };

        /// <summary>
        ///     The command overrides.
        /// </summary>
        private static readonly IDictionary<string, CommandOverride> CommandOverrides = new Dictionary
            <string, CommandOverride>
            {
                // command to override, command parameter to override, value to override with
                {
                    "online", new CommandOverride(A.Status, "online")
                },
                {
                    "busy", new CommandOverride(A.Status, "busy")
                },
                {
                    "looking", new CommandOverride(A.Status, "looking")
                },
                {
                    "away", new CommandOverride(A.Status, "away")
                },
                {
                    "dnd", new CommandOverride(A.Status, "dnd")
                },
                {
                    "idle", new CommandOverride(A.Status, "idle")
                },
                {
                    "ignore", new CommandOverride(A.Action, A.ActionAdd)
                },
                {
                    "unignore", new CommandOverride(A.Action, A.ActionDelete)
                },
                {
                    "openroom", new CommandOverride(A.Status, "public")
                },
                {
                    "closeroom", new CommandOverride(A.Status, "private")
                },
                {
                    "bottle", new CommandOverride("dice", "bottle")
                },
                {
                    "report", new CommandOverride(A.Action, A.ActionReport)
                },
                {
                    "handlereport", new CommandOverride(A.Action, A.ActionConfirm)
                },
            };

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Creates a command complete with its meta data.
        /// </summary>
        /// <param name="familiarName">
        ///     The familiar name.
        /// </param>
        /// <param name="args">
        ///     The arguments of the command.
        /// </param>
        /// <param name="channel">
        ///     The channel, if applicable.
        /// </param>
        public static CommandDataModel CreateCommand(
            string familiarName, IList<string> args = null, string channel = null)
        {
            if (!IsValidCommand(familiarName))
                throw new ArgumentException("Unknown command.", "familiarName");

            if (HasCommandOverride(familiarName))
            {
                var model = GetCommandModelFromName(familiarName);

                var hasEmptyArguments = (args == null || args.All(string.IsNullOrWhiteSpace));
                var needsArguments = (model.CommandType != CommandModel.CommandTypes.NoArgs
                                   && model.CommandType != CommandModel.CommandTypes.TwoArgs);

                if (hasEmptyArguments && needsArguments && model.ArgumentNames.Count != 1)
                    throw new ArgumentException("The {0} command needs an argument.".FormatWith(familiarName));

                var overide = CommandOverrides[familiarName];

                // this inserts the override into the correct location for the toDictionary method
                var overrideArg = overide.ArgumentName;
                var position = model.ArgumentNames.IndexOf(overrideArg);

                args = args == null
                    ? new List<string>()
                    : new List<string>(args);

                if (position != -1 && !(position > args.Count))
                    args.Insert(position, overide.ArgumentValue);
                else
                    args.Add(overide.ArgumentValue);

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
                            throw new ArgumentException(
                                "The {0} command takes a parameter which must be a single word".FormatWith(familiarName));

                        break;

                    case CommandModel.CommandTypes.OnlyChannel:
                    case CommandModel.CommandTypes.NoArgs:
                        if (args != null && !string.IsNullOrWhiteSpace(args[0]))
                            throw new ArgumentException("The {0} command doesn't take an argument".FormatWith(familiarName));

                        break;

                    case CommandModel.CommandTypes.TwoArgs:
                        if (args != null && args.Count > 2)
                            throw new ArgumentException("The {0} command takes only two parameters".FormatWith(familiarName));

                        break;

                    case CommandModel.CommandTypes.SingleArgsAndChannel:
                        if (channel == null)
                            throw new ArgumentException("The {0} command needs an associated channel".FormatWith(familiarName));

                        break;

                    case CommandModel.CommandTypes.SingleSentence:
                        if (args == null || args.All(string.IsNullOrWhiteSpace))
                            throw new ArgumentException("The {0} command needs an argument.".FormatWith(familiarName));
                        break;
                }

                return new CommandDataModel(model, args == null ? null : args.ToArray(), channel);
            }
        }

        /// <summary>
        ///     The get command model from name.
        /// </summary>
        /// <param name="familiarName">
        ///     The familiar name.
        /// </param>
        public static CommandModel GetCommandModelFromName(string familiarName)
        {
            if (!IsValidCommand(familiarName))
                throw new ArgumentException("Unknown command", "familiarName");

            if (CommandAliases.ContainsKey(familiarName))
                familiarName = CommandAliases[familiarName];

            return Commands[familiarName];
        }

        /// <summary>
        ///     The is valid command.
        /// </summary>
        /// <param name="familiarName">
        ///     The familiar name.
        /// </param>
        public static bool IsValidCommand(string familiarName)
        {
            return CommandAliases.ContainsKey(familiarName) || Commands.ContainsKey(familiarName);
        }

        /// <summary>
        ///     The has command override.
        /// </summary>
        /// <param name="familiarName">
        ///     The familiar name.
        /// </param>
        private static bool HasCommandOverride(string familiarName)
        {
            return CommandOverrides.ContainsKey(familiarName);
        }

        #endregion

        /// <summary>
        ///     Represents a command argument override.
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
            ///     Initializes a new instance of the <see cref="CommandOverride" /> struct.
            /// </summary>
            /// <param name="argName">
            ///     The arg name.
            /// </param>
            /// <param name="argValue">
            ///     The arg value.
            /// </param>
            public CommandOverride(string argName, string argValue)
            {
                ArgumentName = argName;
                ArgumentValue = argValue;
            }

            #endregion
        }
    }
}