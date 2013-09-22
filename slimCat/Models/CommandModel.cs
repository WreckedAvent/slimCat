// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandModel.cs" company="Justin Kadrovach">
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
//   Represents metadata about a command
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Models
{
    using System.Collections.Generic;

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
        /// Initializes a new instance of the <see cref="CommandModel"/> class.
        /// </summary>
        /// <param name="familarName">
        /// The familar name.
        /// </param>
        /// <param name="serverName">
        /// The server name.
        /// </param>
        /// <param name="paramaterNames">
        /// The paramater names.
        /// </param>
        /// <param name="typeOfCommand">
        /// The type of command.
        /// </param>
        /// <param name="permissionLevel">
        /// The permission level.
        /// </param>
        public CommandModel(
            string familarName, 
            string serverName, 
            IList<string> paramaterNames = null, 
            CommandTypes typeOfCommand = CommandTypes.SingleSentence, 
            PermissionLevel permissionLevel = PermissionLevel.User)
        {
            this.familiarName = familarName;
            this.serverName = serverName;
            this.type = typeOfCommand;
            this.permissions = permissionLevel;

            this.argumentNames = paramaterNames;
        }

        #endregion

        #region Enums

        /// <summary>
        ///     The command types.
        /// </summary>
        public enum CommandTypes
        {
            /// <summary>
            /// Commands without any arguments.
            /// </summary>
            NoArgs, 

            /// <summary>
            /// Commands with only one-word arguments.
            /// </summary>
            SingleWord, 

            /// <summary>
            /// Commands with only one argument about the length of a sentence.
            /// </summary>
            SingleSentence, 

            /// <summary>
            /// Commands with a one-word argument which apply to a channel.
            /// </summary>
            SingleArgsAndChannel, 

            /// <summary>
            /// Commands with no arguments which apply to a channel.
            /// </summary>
            OnlyChannel, 

            /// <summary>
            /// Commands with two single-word arguments.
            /// </summary>
            TwoArgs, 

            /// <summary>
            /// Commands with two single-word arguments which apply to a channel.
            /// </summary>
            TwoArgsAndChannel, 
        }

        /// <summary>
        ///     The permission level.
        /// </summary>
        public enum PermissionLevel
        {
            /// <summary>
            /// Anyone can use the command (default).
            /// </summary>
            User, 

            /// <summary>
            /// Moderators and above can use the command.
            /// </summary>
            Moderator, 

            /// <summary>
            /// Global moderators and above can use the command.
            /// </summary>
            GlobalMod, 

            /// <summary>
            /// Only admins can use the command.
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
            get
            {
                return this.argumentNames;
            }
        }

        /// <summary>
        ///     What kind of command it is
        /// </summary>
        public CommandTypes CommandType
        {
            get
            {
                return this.type;
            }
        }

        /// <summary>
        ///     The permissions required to use this command
        /// </summary>
        public PermissionLevel PermissionsLevel
        {
            get
            {
                return this.permissions;
            }
        }

        /// <summary>
        ///     What the server or our service layer will recognize
        /// </summary>
        public string ServerName
        {
            get
            {
                return this.serverName;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a command with its meta data
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
        /// Initializes a new instance of the <see cref="CommandDataModel"/> struct.
        /// </summary>
        /// <param name="info">
        /// The info.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <param name="channelName">
        /// The channel name.
        /// </param>
        public CommandDataModel(CommandModel info, IList<string> args, string channelName)
        {
            this.commandInformation = info;
            this.arguments = args;
            this.channelName = channelName;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The to dictionary.
        /// </summary>
        /// <returns>
        ///     The <see>
        ///             <cref>IDictionary</cref>
        ///         </see>
        ///     .
        /// </returns>
        public IDictionary<string, object> ToDictionary()
        {
            var toSend = new Dictionary<string, object> { { "type", this.commandInformation.ServerName }, };

            if (this.arguments != null && this.arguments[0] != null)
            {
                var count = 0;
                foreach (var argumentName in this.commandInformation.ArgumentNames)
                {
                    toSend.Add(argumentName, this.arguments[count]);
                    count++;
                }
            }

            var isChannelCommand = this.commandInformation.CommandType
                                    == CommandModel.CommandTypes.SingleArgsAndChannel
                                    || this.commandInformation.CommandType == CommandModel.CommandTypes.OnlyChannel
                                    || this.commandInformation.CommandType
                                    == CommandModel.CommandTypes.TwoArgsAndChannel;

            if (this.channelName != null && isChannelCommand)
            {
                toSend.Add("channel", this.channelName);
            }

            return toSend;
        }

        #endregion
    }
}