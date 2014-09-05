#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandModel.cs">
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

    using System.Collections.Generic;
    using System.Linq;
    using Utilities;

    #endregion

    /// <summary>
    ///     Represents metadata about a command
    /// </summary>
    public class CommandModel
    {
        #region Fields

        private readonly IList<string> argumentNames;

        private readonly string familiarName;

        private readonly PermissionLevel permissions;

        private readonly string serverName;

        private readonly CommandTypes type;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandModel" /> class.
        /// </summary>
        /// <param name="familiarName">
        ///     The familiar name.
        /// </param>
        /// <param name="serverName">
        ///     The server name.
        /// </param>
        /// <param name="paramaterNames">
        ///     The paramater names.
        /// </param>
        /// <param name="typeOfCommand">
        ///     The type of command.
        /// </param>
        /// <param name="permissionLevel">
        ///     The permission level.
        /// </param>
        public CommandModel(
            string familiarName,
            string serverName,
            IList<string> paramaterNames = null,
            CommandTypes typeOfCommand = CommandTypes.SingleSentence,
            PermissionLevel permissionLevel = PermissionLevel.User)
        {
            this.familiarName = familiarName;
            this.serverName = serverName;
            type = typeOfCommand;
            permissions = permissionLevel;

            argumentNames = paramaterNames;
        }

        #endregion

        #region Enums

        /// <summary>
        ///     The command types.
        /// </summary>
        public enum CommandTypes
        {
            /// <summary>
            ///     Commands without any arguments.
            /// </summary>
            NoArgs,

            /// <summary>
            ///     Commands with only one-word arguments.
            /// </summary>
            SingleWord,

            /// <summary>
            ///     Commands with only one argument about the length of a sentence.
            /// </summary>
            SingleSentence,

            /// <summary>
            ///     Commands with a one-word argument which apply to a channel.
            /// </summary>
            SingleArgsAndChannel,

            /// <summary>
            ///     Commands with no arguments which apply to a channel.
            /// </summary>
            OnlyChannel,

            /// <summary>
            ///     Commands with two single-word arguments.
            /// </summary>
            TwoArgs,

            /// <summary>
            ///     Commands with two single-word arguments which apply to a channel.
            /// </summary>
            TwoArgsAndChannel,
        }

        /// <summary>
        ///     The permission level.
        /// </summary>
        public enum PermissionLevel
        {
            /// <summary>
            ///     Anyone can use the command (default).
            /// </summary>
            User,

            /// <summary>
            ///     Moderators and above can use the command.
            /// </summary>
            Moderator,

            /// <summary>
            ///     Global moderators and above can use the command.
            /// </summary>
            GlobalMod,

            /// <summary>
            ///     Only admins can use the command.
            /// </summary>
            Admin,
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     The names of the arguments it has
        /// </summary>
        public IList<string> ArgumentNames
        {
            get { return argumentNames; }
        }

        /// <summary>
        ///     What kind of command it is
        /// </summary>
        public CommandTypes CommandType
        {
            get { return type; }
        }

        /// <summary>
        ///     The permissions required to use this command
        /// </summary>
        public PermissionLevel PermissionsLevel
        {
            get { return permissions; }
        }

        /// <summary>
        ///     What the server or our service layer will recognize
        /// </summary>
        public string ServerName
        {
            get { return serverName; }
        }

        #endregion
    }

    /// <summary>
    ///     Represents a command with its meta data
    /// </summary>
    public class CommandDataModel
    {
        #region Fields

        private readonly IList<string> arguments;

        private readonly string channelName;

        private readonly CommandModel commandInformation;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandDataModel" /> struct.
        /// </summary>
        /// <param name="info">
        ///     The info.
        /// </param>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <param name="channelName">
        ///     The channel name.
        /// </param>
        public CommandDataModel(CommandModel info, IList<string> args, string channelName)
        {
            commandInformation = info;
            arguments = args;
            this.channelName = channelName;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Converts a command to a dictionary for sending over the wire.
        /// </summary>
        /// <returns>
        ///     An <see cref="Dictionary{TKey,TValue}" /> which can be serialized to be sent to the server.
        /// </returns>
        public IDictionary<string, object> ToDictionary()
        {
            var toSend = new Dictionary<string, object> {{"type", commandInformation.ServerName},};

            if (arguments != null && arguments[0] != null)
            {
                var count = 0;
                foreach (var argumentName in commandInformation.ArgumentNames.Take(arguments.Count))
                {
                    toSend.Add(argumentName, arguments[count]);
                    count++;
                }
            }

            if (channelName == null
                || commandInformation.ArgumentNames == null
                || !commandInformation.ArgumentNames.Contains(Constants.Arguments.Channel))
                return toSend;

            object value;
            if (toSend.TryGetValue(Constants.Arguments.Channel, out value))
            {
                if (!string.IsNullOrWhiteSpace(value as string))
                    return toSend;
            }

            toSend[Constants.Arguments.Channel] = channelName;

            return toSend;
        }

        #endregion
    }
}