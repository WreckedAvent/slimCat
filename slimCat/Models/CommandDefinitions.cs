#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandDefinitions.cs">
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

namespace slimCat.Models
{
    #region Usings

    // I know these are bad names, but it makes the giant list below more legible
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utilities;
    using A = Utilities.Constants.Arguments;
    using C = Utilities.Constants.ClientCommands;
    using P = CommandModel.PermissionLevel;
    using T = CommandModel.CommandTypes;

    #endregion

    /// <summary>
    ///     This class provides a list of possible commands and all possible information relating to those commands.
    ///     It also provides a method for creating a command.
    /// </summary>
    public static class CommandDefinitions
    {
        #region Constants

        public const string ClientSendChannelAd = "_send_channel_ad";

        public const string ClientSendChannelMessage = "_send_channel_message";

        public const string ClientSendPm = "_send_private_message";

        public const string ClientSendTypingStatus = "_send_typing_status";

        #endregion

        static CommandDefinitions()
        {
            UserCommands(
                Define("acceptfriendrequest", "acceptrequest").As("request-accept").AsForCharacters(),
                Define("addbookmark").As("bookmark-add").WithArgument(A.Name),
                Define("bottle").As(C.ChannelRoll).WithArguments("dice", A.Channel),
                Define("code").AsArgumentless(),
                Define("clear").AsForChannels(),
                Define("clearall").AsArgumentless(),
                Define("close").AsForChannels(),
                Define("coplist", "modlist").As(C.ChannelModeratorList).AsForChannels(),
                Define("denyfriendrequest", "denyrequest").As("request-deny").AsForCharacters(),
                Define("forceclose").AsForChannels(),
                Define("ignore").As(C.UserIgnore).WithArguments(A.Character, A.Action),
                Define("ignoreUpdates").AsForCharacters(),
                Define("interesting").AsForCharacters(),
                Define("invite").As(C.UserInvite).WithArguments(A.Character, A.Channel),
                Define("ignorelist").As(C.UserIgnore).WithArgument(A.Action),
                Define("join").AsForChannels(),
                Define("logheader").As("_logger_new_header").WithArgument("title"),
                Define("logsection").As("_logger_new_section").WithArgument("title"),
                Define("lognewline").As("_logger_new_line").AsArgumentless(),
                Define("logout").AsArgumentless(),
                Define("makeroom").As(C.ChannelCreate).AsForChannels(),
                Define("notinteresting").AsForCharacters(),
                Define("openlog").As("_logger_open_log").AsForChannels(),
                Define("openlogfolder", "openfolder").As("_logger_open_folder").AsForChannels(),
                Define("priv", "pm", "tell").AsForCharacters(),
                Define("rejoin").AsForChannels(),
                Define("removebookmark").As("bookmark-remove").WithArgument(A.Name),
                Define("removefriend").As("friend-remove").AsForCharacters(),
                Define("cancelrequest", "cancelfriendrequest").As("request-cancel").AsForCharacters(),
                Define("roll").As(C.ChannelRoll).WithArguments("dice", A.Channel),
                Define("report").As(C.AdminAlert).WithArguments(A.Name, A.Report, A.Action, A.Channel),
                Define("status", "online", "away", "busy", "idle", "dnd", "looking")
                    .As(C.UserStatus)
                    .WithArguments(A.Status, A.StatusMessage),
                Define("setowner").As(C.ChannelSetOwner).WithArguments(A.Character, A.Channel),
                Define("soundon").AsArgumentless(),
                Define("soundoff").AsArgumentless(),
                Define("sendfriendrequest", "sendrequest", "addfriend").As("request-send").AsForCharacters(),
                Define("searchtag").AsForCharacters(),
                Define("tempignore").AsForCharacters(),
                Define("tempunignore").AsForCharacters(),
                Define("tempnotinteresting").AsForCharacters(),
                Define("unignore").As(C.UserIgnore).WithArguments(A.Character, A.Action),
                Define("who").AsArgumentless()
                );


            ModeratorCommands(
                Define("ban").As(C.ChannelBan).WithArguments(A.Character, A.Channel),
                Define("banlist").As(C.ChannelBanList).AsForChannels(),
                Define("closeroom").As(C.ChannelKind).WithArguments("status", A.Channel),
                Define("demote", "dop").As(C.ChannelDemote).WithArguments(A.Character, A.Channel),
                Define("getdescription").WithArgument(A.Channel),
                Define("kick").As(C.ChannelKick).WithArguments(A.Character, A.Channel),
                Define("killchannel").As(C.ChannelKill).WithArgument(A.Channel),
                Define("openroom").As(C.ChannelKind).WithArguments("status", A.Channel),
                Define("promote", "cop").As(C.ChannelPromote).WithArguments(A.Character, A.Channel),
                Define("setdescription").As(C.ChannelDescription).WithArguments("description", A.Channel),
                Define("timeout").As(C.ChannelTimeOut).WithArguments(A.Character, "time", "reason"),
                Define("unban").As(C.ChannelUnban).WithArguments(A.Character, A.Channel),
                Define("setmode").As(C.ChannelMode).WithArguments(A.Mode, A.Channel)
                );


            AdminCommands(
                Define("chatban", "accountban", "gban").As(C.AdminBan).AsForCharacters(),
                Define("chatkick", "gkick").As(C.AdminKick).AsForCharacters(),
                Define("chatunban", "gunban").As(C.AdminUnban).AsForCharacters(),
                Define("reward").As(C.AdminReward).AsForCharacters(),
                Define("chattimeout", "gtimeout").As(C.AdminTimeout).WithArguments(A.Character, "time", "reason"),
                Define("handlereport", "hr").WithArgument(A.Name),
                Define("handlelatest", "r").WithArgument(A.Name),
                Define("broadcast").As(C.AdminBroadcast).AsForCharacters(),
                Define("chatdemote", "deop").As(C.AdminDemote).AsForCharacters(),
                Define("chatpromote", "op").As(C.AdminPromote).AsForCharacters(),
                Define("makechannel", "createchannel").As(C.SystemChannelCreate).AsForChannels()
                );

            SystemCommands(
                Define(ClientSendTypingStatus).As(C.UserTyping).WithArguments("status", A.Character),
                Define(ClientSendPm).As(C.UserMessage).WithArguments(A.Message, "recipient"),
                Define(ClientSendChannelMessage).As(C.ChannelMessage).WithArguments(A.Message, A.Channel),
                Define(ClientSendChannelAd).As(C.ChannelAd).WithArguments(A.Message, A.Channel)
                );
        }

        #region Static Fields

        public static readonly IDictionary<string, CommandModel> Commands = new Dictionary<string, CommandModel>();

        public static readonly string[] NonCommandCommands =
        {
            // prevents long ugly checking in our viewmodels for these
            "/me", "/warn", "/post"
        };

        private static readonly IDictionary<string, string> CommandAliases = new Dictionary<string, string>();

        private static readonly IDictionary<string, CommandOverride> CommandOverrides = new Dictionary
            <string, CommandOverride>(StringComparer.OrdinalIgnoreCase)
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
            {
                "ignorelist", new CommandOverride(A.Action, "list")
            }
        };

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Creates a command complete with its meta data.
        /// </summary>
        /// <param name="familiarName">
        ///     The name of the command the user inputs.
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
                throw new ArgumentException("Unknown command: {0}.".FormatWith(familiarName));

            var model = GetCommandModelFromName(familiarName);

            // having an empty argument is the same as none for our purposes 
            if (args != null && string.IsNullOrEmpty(args[0]))
                args = null;

            CommandOverride commandOverride;
            if (CommandOverrides.TryGetValue(familiarName, out commandOverride))
            {
                var overrideArg = commandOverride.ArgumentName;
                var position = model.ArgumentNames.IndexOf(overrideArg);

                args = (args == null)
                    ? new List<string>()
                    : new List<string>(args);

                if (position != -1 && !(position > args.Count))
                    args.Insert(position, commandOverride.ArgumentValue);
                else
                    args.Add(commandOverride.ArgumentValue);
            }

            var toReturn = new CommandDataModel(model, args, channel);

            // with no arguments we needn't do any validation
            if (args == null && model.ArgumentNames == null) return toReturn;

            var argsCount = (args != null ? args.Count : 0);
            var modelArgsCount = (model.ArgumentNames != null ? model.ArgumentNames.Count : 0);

            var difference = argsCount - modelArgsCount;

            // if we have parity in counts we don't have any issues
            if (difference == 0) return toReturn;

            // error out if we have more arguments than we should
            if (difference > 0 || model.ArgumentNames == null)
            {
                throw new ArgumentException("{0} takes {1} arguments, not {2}.".FormatWith(familiarName, modelArgsCount,
                    argsCount));
            }

            var missingArgument = model.ArgumentNames[difference + model.ArgumentNames.Count];

            // error out if we have less arguments, but not if we are only missing the channel argument provided by the active channel
            if (difference == -1)
            {
                if (missingArgument == A.Channel && channel != null && channel != "Home")
                    return toReturn;
                if (missingArgument == A.StatusMessage || missingArgument == "reason")
                    return toReturn;
            }

            throw new ArgumentException("{0} is missing the '{1}' argument".FormatWith(familiarName, missingArgument));
        }

        public static CommandModel GetCommandModelFromName(string familiarName)
        {
            if (!IsValidCommand(familiarName))
                throw new ArgumentException("Unknown command", "familiarName");

            if (CommandAliases.ContainsKey(familiarName))
                familiarName = CommandAliases[familiarName];

            return Commands[familiarName];
        }

        public static bool IsValidCommand(string familiarName)
        {
            return CommandAliases.ContainsKey(familiarName) || Commands.ContainsKey(familiarName);
        }

        #endregion

        private static FluentCommandBuilder Define(string commonName, params string[] aliases)
        {
            return new FluentCommandBuilder(commonName, aliases);
        }

        private static void UserCommands(params FluentCommandBuilder[] commands)
        {
            commands.Each(x => Commands.Add(x.Build()));
            AddAliases(commands);
        }

        private static void ModeratorCommands(params FluentCommandBuilder[] commands)
        {
            commands.Select(x => x.ForModerators()).Each(x => Commands.Add(x.Build()));
            AddAliases(commands);
        }

        private static void AdminCommands(params FluentCommandBuilder[] commands)
        {
            commands.Select(x => x.ForAdmins()).Each(x => Commands.Add(x.Build()));
            AddAliases(commands);
        }

        private static void SystemCommands(params FluentCommandBuilder[] commands)
        {
            commands.Each(x => Commands.Add(x.Build()));
            AddAliases(commands);
        }

        private static void AddAliases(params FluentCommandBuilder[] commands)
        {
            commands.Each(command =>
                command.Aliases.Each(alias =>
                    CommandAliases.Add(alias, command.CommonName)));
        }

        /// <summary>
        ///     Represents a command argument override.
        /// </summary>
        private class CommandOverride
        {
            public readonly string ArgumentName;

            public readonly string ArgumentValue;

            public CommandOverride(string argName, string argValue)
            {
                ArgumentName = argName;
                ArgumentValue = argValue;
            }
        }
    }

    internal class FluentCommandBuilder
    {
        public FluentCommandBuilder(string commonName, params string[] aliases)
        {
            CommonName = commonName;
            Arguments = new List<string>();
            Aliases = aliases;
            CommandType = T.SingleWord;
            PermissionLevel = P.User;
        }

        public string CommonName { get; set; }
        public T CommandType { get; set; }

        public P PermissionLevel { get; set; }

        public string RemoteName { get; set; }
        public IList<string> Arguments { get; set; }

        public IList<string> Aliases { get; set; }

        public FluentCommandBuilder OfType(T type)
        {
            CommandType = type;
            return this;
        }

        public FluentCommandBuilder AsArgumentless()
        {
            CommandType = T.NoArgs;
            Arguments = null;
            return this;
        }

        public FluentCommandBuilder WithArgument(string argument)
        {
            CommandType = T.SingleWord;
            Arguments.Add(argument);

            if (argument == A.Character)
                CommandType = T.SingleSentence;

            if (argument == A.Channel)
                CommandType = T.OnlyChannel;

            return this;
        }

        public FluentCommandBuilder WithArguments(string first, string second, string third = null, string fourth = null)
        {
            if (first == A.Channel || second == A.Channel)
                CommandType = T.SingleArgsAndChannel;
            else
                CommandType = T.TwoArgs;

            if (third == A.Channel || fourth == A.Channel)
                CommandType = T.TwoArgsAndChannel;

            Arguments.Add(first);
            Arguments.Add(second);

            if (third != null)
                Arguments.Add(third);

            if (fourth != null)
                Arguments.Add(fourth);

            return this;
        }

        public FluentCommandBuilder AsForChannels()
        {
            return WithArgument(A.Channel);
        }

        public FluentCommandBuilder AsForCharacters()
        {
            return WithArgument(A.Character);
        }

        public FluentCommandBuilder ForModerators()
        {
            PermissionLevel = P.Moderator;
            return this;
        }

        public FluentCommandBuilder ForAdmins()
        {
            PermissionLevel = P.GlobalMod;
            return this;
        }

        public FluentCommandBuilder As(string name)
        {
            RemoteName = name;
            return this;
        }


        public KeyValuePair<string, CommandModel> Build()
        {
            return new KeyValuePair<string, CommandModel>(CommonName,
                new CommandModel(CommonName, RemoteName ?? CommonName, Arguments, CommandType, PermissionLevel));
        }
    }
}