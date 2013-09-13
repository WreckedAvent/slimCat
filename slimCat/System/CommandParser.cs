// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandParser.cs" company="Justin Kadrovach">
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
//   This is responsible for translating text and interpreting commands
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace System
{
    using System.Collections.Generic;
    using System.Linq;

    using Models;

    /// <summary>
    ///     This is responsible for translating text and interpreting commands
    /// </summary>
    public class CommandParser
    {
        #region Fields

        private readonly string _args;

        private readonly string _currentChan;

        private readonly bool _hasBadSyntx;

        private readonly bool _hasCommand = true;

        private readonly string _type;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandParser"/> class.
        /// </summary>
        /// <param name="rawInput">
        /// The raw input.
        /// </param>
        /// <param name="currentChannel">
        /// The current channel.
        /// </param>
        public CommandParser(string rawInput, string currentChannel)
        {
            if (!rawInput.StartsWith("/"))
            {
                this._hasCommand = false;
            }

            string type;
            string arguments;

            if (this.HasCommand)
            {
                if (!rawInput.Trim().Contains(' '))
                {
                    this._type = rawInput.Trim().Substring(1);
                }
                else
                {
                    int firstSpace = rawInput.Substring(1).IndexOf(' ');
                    type = rawInput.Substring(1, firstSpace);

                    arguments = rawInput.Substring(firstSpace + 2);

                    if (type.Equals("status", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!arguments.Contains(' '))
                        {
                            this._hasBadSyntx = true;
                        }
                        else
                        {
                            type = arguments.Substring(0, arguments.IndexOf(' '));
                            arguments = arguments.Substring(arguments.IndexOf(' ') + 1);
                        }
                    }

                    if (type.Contains('_'))
                    {
                        this._hasBadSyntx = true;
                    }

                    this._type = type;
                    this._args = arguments;
                }

                this._currentChan = currentChannel;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether has command.
        /// </summary>
        public bool HasCommand
        {
            get
            {
                return this._hasCommand;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return !this._hasBadSyntx && this.HasCommand && CommandDefinitions.IsValidCommand(this.Type);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether requires global mod.
        /// </summary>
        public bool RequiresGlobalMod
        {
            get
            {
                if (this.HasCommand && this.IsValid)
                {
                    return CommandDefinitions.Commands[this.Type].PermissionsLevel
                           == CommandModel.PermissionLevel.GlobalMod;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Gets a value indicating whether requires mod.
        /// </summary>
        public bool RequiresMod
        {
            get
            {
                if (this.HasCommand && this.IsValid)
                {
                    return CommandDefinitions.GetCommandModelFromName(this.Type).PermissionsLevel
                           == CommandModel.PermissionLevel.Moderator;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Gets the type.
        /// </summary>
        public string Type
        {
            get
            {
                return this.HasCommand ? this._type.ToLower() : "none";
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The has non command.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool HasNonCommand(string input)
        {
            return CommandDefinitions.NonCommandCommands.Any(noncommand => input.StartsWith(noncommand));
        }

        /// <summary>
        ///     The to dictionary.
        /// </summary>
        /// <returns>
        ///     The <see cref="IDictionary" />.
        /// </returns>
        public IDictionary<string, object> toDictionary()
        {
            return
                CommandDefinitions.CreateCommand(this.Type, new List<string> { this._args }, this._currentChan)
                                  .toDictionary();
        }

        #endregion
    }
}