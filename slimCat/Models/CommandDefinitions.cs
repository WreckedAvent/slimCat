#region Copyright

// <copyright file="CommandDefinitions.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utilities;
    using static Utilities.Constants.Arguments;
    using static Utilities.Constants.ClientCommands;
    using static CommandModel.PermissionLevel;
    using static CommandModel.CommandTypes;

    #endregion

    /// <summary>
    ///     This class provides a list of possible commands and all possible information relating to those commands.
    ///     It also provides a method for creating a command.
    /// </summary>
    public static class CommandDefinitions
    {
        static CommandDefinitions()
        {
            UserCommands(
                Define("acceptfriendrequest", "acceptrequest").As("request-accept").AsForCharacters(),
                Define("addbookmark").As("bookmark-add").WithArgument(Name),
                Define("bottle").As(ChannelRoll).WithArguments("dice", Channel),
                Define("code").AsArgumentless(),
                Define("clear").AsForChannels(),
                Define("clearall").AsArgumentless(),
                Define("close").AsForChannels(),
                Define("coplist", "modlist").As(ChannelModeratorList).AsForChannels(),
                Define("denyfriendrequest", "denyrequest").As("request-deny").AsForCharacters(),
                Define("forceclose").AsForChannels(),
                Define("ignore").As(UserIgnore).WithArguments(Character, Constants.Arguments.Action),
                Define("ignoreUpdates").AsForCharacters(),
                Define("interesting").AsForCharacters(),
                Define("invite").As(UserInvite).WithArguments(Character, Channel),
                Define("ignorelist").As(UserIgnore).WithArgument(Constants.Arguments.Action),
                Define("join").AsForChannels(),
                Define("logheader").As("_logger_new_header").WithArgument("title"),
                Define("logsection").As("_logger_new_section").WithArgument("title"),
                Define("lognewline").As("_logger_new_line").AsArgumentless(),
                Define("logout").AsArgumentless(),
                Define("makeroom").As(ChannelCreate).AsForChannels(),
                Define("notinteresting").AsForCharacters(),
                Define("openlog").As("_logger_open_log").AsForChannels(),
                Define("openlogfolder", "openfolder").As("_logger_open_folder").AsForChannels(),
                Define("priv", "pm", "tell").AsForCharacters(),
                Define("rejoin").AsForChannels(),
                Define("removebookmark").As("bookmark-remove").WithArgument(Name),
                Define("removefriend").As("friend-remove").AsForCharacters(),
                Define("cancelrequest", "cancelfriendrequest").As("request-cancel").AsForCharacters(),
                Define("roll").As(ChannelRoll).WithArguments("dice", Channel),
                Define("report").As(AdminAlert).WithArguments(Name, Report, Constants.Arguments.Action, Channel),
                Define("status", "online", "away", "busy", "idle", "dnd", "looking")
                    .As(UserStatus)
                    .WithArguments(Status, StatusMessage),
                Define("setowner").As(ChannelSetOwner).WithArguments(Character, Channel),
                Define("soundon").AsArgumentless(),
                Define("soundoff").AsArgumentless(),
                Define("sendfriendrequest", "sendrequest", "addfriend").As("request-send").AsForCharacters(),
                Define("searchtag").AsForCharacters(),
                Define("tempignore").AsForCharacters(),
                Define("tempunignore").AsForCharacters(),
                Define("tempnotinteresting").AsForCharacters(),
                Define("unignore").As(UserIgnore).WithArguments(Character, Constants.Arguments.Action),
                Define("who").AsArgumentless(),
                Define("whois").AsForCharacters()
                );


            ModeratorCommands(
                Define("ban").As(ChannelBan).WithArguments(Character, Channel),
                Define("banlist").As(ChannelBanList).AsForChannels(),
                Define("closeroom").As(ChannelKind).WithArguments("status", Channel),
                Define("demote", "dop").As(ChannelDemote).WithArguments(Character, Channel),
                Define("getdescription").WithArgument(Channel),
                Define("kick").As(ChannelKick).WithArguments(Character, Channel),
                Define("killchannel").As(ChannelKill).WithArgument(Channel),
                Define("openroom").As(ChannelKind).WithArguments("status", Channel),
                Define("promote", "cop").As(ChannelPromote).WithArguments(Character, Channel),
                Define("setdescription").As(ChannelDescription).WithArguments("description", Channel),
                Define("timeout").As(ChannelTimeOut).WithArguments(Character, "length", Channel),
                Define("unban").As(ChannelUnban).WithArguments(Character, Channel),
                Define("setmode").As(Constants.ClientCommands.ChannelMode).WithArguments(Mode, Channel)
                );


            AdminCommands(
                Define("chatban", "accountban", "gban").As(AdminBan).AsForCharacters(),
                Define("chatkick", "gkick").As(AdminKick).AsForCharacters(),
                Define("chatunban", "gunban").As(AdminUnban).AsForCharacters(),
                Define("reward").As(AdminReward).AsForCharacters(),
                Define("chattimeout", "gtimeout").As(AdminTimeout).WithArguments(Character, "time", "reason"),
                Define("handlereport", "hr").WithArgument(Name),
                Define("handlelatest", "r").WithArgument(Name),
                Define("broadcast").As(AdminBroadcast).AsForCharacters(),
                Define("chatdemote", "deop").As(AdminDemote).AsForCharacters(),
                Define("chatpromote", "op").As(AdminPromote).AsForCharacters(),
                Define("makechannel", "createchannel").As(SystemChannelCreate).AsForChannels()
                );

            SystemCommands(
                Define(ClientSendTypingStatus).As(UserTyping).WithArguments("status", Character),
                Define(ClientSendPm).As(UserMessage).WithArguments(Message, Recipient),
                Define(ClientSendChannelMessage).As(ChannelMessage).WithArguments(Message, Channel),
                Define(ClientSendChannelAd).As(ChannelAd).WithArguments(Message, Channel)
                );
        }

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
        public class CommandOverride
        {
            public readonly string ArgumentName;
            public readonly string ArgumentValue;

            public CommandOverride(string argName, string argValue)
            {
                ArgumentName = argName;
                ArgumentValue = argValue;
            }
        }

        #region Constants

        public const string ClientSendChannelAd = "_send_channel_ad";

        public const string ClientSendChannelMessage = "_send_channel_message";

        public const string ClientSendPm = "_send_private_message";

        public const string ClientSendTypingStatus = "_send_typing_status";

        #endregion

        #region Static Fields

        public static readonly IDictionary<string, CommandModel> Commands = new Dictionary<string, CommandModel>();

        // prevents long ugly checking in our viewmodels for these
        public static readonly Dictionary<string, Func<string, string>> NonCommandCommands = new Dictionary
            <string, Func<string, string>>
        {
            {
                "/me ", x =>
                {
                    var toReturn = x.Substring("/me ".Length);
                    // give /me some space, but not /me 's
                    return toReturn.Length >= 1 && toReturn[0] == '\'' ? toReturn : ' ' + toReturn;
                }
            },
            {"/me's ", x => x.Substring("/me".Length)},
            {"/my ", x => "'s" + x.Substring("/my".Length)},
            {"/post ", x => x.Substring("/post".Length) + " ~"},
            {"/warn ", x => " warns," + x.Substring("/warn".Length)}
        };

        private static readonly IList<string> WarningList = new[] {"me", "me's", "my", "post", "warn"};

        public static readonly IDictionary<string, string> CommandAliases = new Dictionary<string, string>();

        public static readonly IDictionary<string, CommandOverride> CommandOverrides = new Dictionary
            <string, CommandOverride>(StringComparer.OrdinalIgnoreCase)
        {
            // command to override, command parameter to override, value to override with
            {
                "online", new CommandOverride(Status, "online")
            },
            {
                "busy", new CommandOverride(Status, "busy")
            },
            {
                "looking", new CommandOverride(Status, "looking")
            },
            {
                "away", new CommandOverride(Status, "away")
            },
            {
                "dnd", new CommandOverride(Status, "dnd")
            },
            {
                "idle", new CommandOverride(Status, "idle")
            },
            {
                "ignore", new CommandOverride(Constants.Arguments.Action, ActionAdd)
            },
            {
                "unignore", new CommandOverride(Constants.Arguments.Action, ActionDelete)
            },
            {
                "openroom", new CommandOverride(Status, "public")
            },
            {
                "closeroom", new CommandOverride(Status, "private")
            },
            {
                "bottle", new CommandOverride("dice", "bottle")
            },
            {
                "report", new CommandOverride(Constants.Arguments.Action, ActionReport)
            },
            {
                "handlereport", new CommandOverride(Constants.Arguments.Action, ActionConfirm)
            },
            {
                "ignorelist", new CommandOverride(Constants.Arguments.Action, "list")
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
            if (WarningList.Contains(familiarName))
                throw new ArgumentException("Command '{0}' must be sent with text.".FormatWith(familiarName));

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

            var argsCount = args?.Count ?? 0;
            var modelArgsCount = model.ArgumentNames?.Count ?? 0;

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
                if (missingArgument == Channel && channel != null && channel != "Home")
                    return toReturn;
                if (missingArgument == StatusMessage || missingArgument == "reason")
                    return toReturn;
            }

            throw new ArgumentException("{0} is missing the '{1}' argument".FormatWith(familiarName, missingArgument));
        }

        public static CommandModel GetCommandModelFromName(string familiarName)
        {
            if (!IsValidCommand(familiarName))
                throw new ArgumentException("Unknown command", nameof(familiarName));

            if (CommandAliases.ContainsKey(familiarName))
                familiarName = CommandAliases[familiarName];

            return Commands[familiarName];
        }

        public static bool IsValidCommand(string familiarName)
        {
            return CommandAliases.ContainsKey(familiarName) || Commands.ContainsKey(familiarName);
        }

        #endregion
    }

    internal class FluentCommandBuilder
    {
        public FluentCommandBuilder(string commonName, params string[] aliases)
        {
            CommonName = commonName;
            Arguments = new List<string>();
            Aliases = aliases;
            CommandType = SingleWord;
            PermissionLevel = User;
        }

        public string CommonName { get; set; }
        public CommandModel.CommandTypes CommandType { get; set; }
        public CommandModel.PermissionLevel PermissionLevel { get; set; }
        public string RemoteName { get; set; }
        public IList<string> Arguments { get; set; }
        public IList<string> Aliases { get; set; }

        public FluentCommandBuilder OfType(CommandModel.CommandTypes type)
        {
            CommandType = type;
            return this;
        }

        public FluentCommandBuilder AsArgumentless()
        {
            CommandType = NoArgs;
            Arguments = null;
            return this;
        }

        public FluentCommandBuilder WithArgument(string argument)
        {
            CommandType = SingleWord;
            Arguments.Add(argument);

            if (argument == Character)
                CommandType = SingleSentence;

            if (argument == Channel)
                CommandType = OnlyChannel;

            return this;
        }

        public FluentCommandBuilder WithArguments(string first, string second, string third = null, string fourth = null)
        {
            if (first == Channel || second == Channel)
                CommandType = SingleArgsAndChannel;
            else
                CommandType = TwoArgs;

            if (third == Channel || fourth == Channel)
                CommandType = TwoArgsAndChannel;

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
            return WithArgument(Channel);
        }

        public FluentCommandBuilder AsForCharacters()
        {
            return WithArgument(Character);
        }

        public FluentCommandBuilder ForModerators()
        {
            PermissionLevel = Moderator;
            return this;
        }

        public FluentCommandBuilder ForAdmins()
        {
            PermissionLevel = GlobalMod;
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