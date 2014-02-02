#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandParser.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;

    #endregion

    /// <summary>
    ///     This is responsible for translating text and interpreting commands
    /// </summary>
    public class CommandParser
    {
        #region Fields

        private readonly string args;

        private readonly string currentChan;

        private readonly bool hasCommand = true;
        private readonly bool isInvalid;

        private readonly string type;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CommandParser" /> class.
        /// </summary>
        /// <param name="rawInput">
        ///     The raw input.
        /// </param>
        /// <param name="currentChannel">
        ///     The current channel.
        /// </param>
        public CommandParser(string rawInput, string currentChannel)
        {
            if (!rawInput.StartsWith("/"))
                hasCommand = false;

            if (!HasCommand)
                return;

            if (!rawInput.Trim().Contains(' '))
                type = rawInput.Trim().Substring(1);
            else
            {
                var firstSpace = rawInput.Substring(1).IndexOf(' ');
                var parsedType = rawInput.Substring(1, firstSpace);

                var arguments = rawInput.Substring(firstSpace + 2);

                if (parsedType.Equals("status", StringComparison.OrdinalIgnoreCase))
                {
                    if (!arguments.Contains(' '))
                        isInvalid = true;
                    else
                    {
                        parsedType = arguments.Substring(0, arguments.IndexOf(' '));
                        arguments = arguments.Substring(arguments.IndexOf(' ') + 1);
                    }
                }

                if (parsedType.Contains('_'))
                    isInvalid = true;

                type = parsedType;
                args = arguments;
            }

            currentChan = currentChannel;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether has command.
        /// </summary>
        public bool HasCommand
        {
            get { return hasCommand; }
        }

        /// <summary>
        ///     Gets a value indicating whether is valid.
        /// </summary>
        public bool IsValid
        {
            get { return !isInvalid && HasCommand && CommandDefinitions.IsValidCommand(Type); }
        }

        /// <summary>
        ///     Gets a value indicating whether requires global mod.
        /// </summary>
        public bool RequiresGlobalMod
        {
            get
            {
                if (HasCommand && IsValid)
                {
                    return CommandDefinitions.Commands[Type].PermissionsLevel
                           == CommandModel.PermissionLevel.GlobalMod;
                }

                return false;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether requires mod.
        /// </summary>
        public bool RequiresMod
        {
            get
            {
                if (HasCommand && IsValid)
                {
                    return CommandDefinitions.GetCommandModelFromName(Type).PermissionsLevel
                           == CommandModel.PermissionLevel.Moderator;
                }

                return false;
            }
        }

        /// <summary>
        ///     Gets the type.
        /// </summary>
        public string Type
        {
            get { return HasCommand ? type.ToLower() : "none"; }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The has non command.
        /// </summary>
        /// <param name="input">
        ///     The input.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool HasNonCommand(string input)
        {
            return CommandDefinitions.NonCommandCommands.Any(input.StartsWith);
        }

        /// <summary>
        ///     The to dictionary.
        /// </summary>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>IDictionary</cref>
        ///     </see>
        ///     .
        /// </returns>
        public IDictionary<string, object> ToDictionary()
        {
            return
                CommandDefinitions.CreateCommand(Type, new List<string> {args}, currentChan)
                    .ToDictionary();
        }

        #endregion
    }
}