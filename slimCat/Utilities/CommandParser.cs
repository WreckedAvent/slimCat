#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandParser.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualBasic.FileIO;
    using Models;

    #endregion

    /// <summary>
    ///     This is responsible for translating text and interpreting commands
    /// </summary>
    public class CommandParser
    {
        #region Fields

        private readonly string currentChannel;

        private readonly string type;
        private IList<string> arguments;

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
            this.currentChannel = currentChannel;

            if (rawInput.Length <= 1 || !rawInput[0].Equals('/'))
            {
                HasCommand = false;
                return;
            }

            var trimmed = rawInput.Trim().Substring(1);
            if (!trimmed.Contains(' '))
            {
                type = trimmed;
                return;
            }

            var spaceIndex = trimmed.IndexOf(' ');
            type = trimmed.Substring(0, spaceIndex);
            trimmed = trimmed.Substring(spaceIndex + 1);

            CommandModel model;
            if (CommandDefinitions.Commands.TryGetValue(type, out model))
            {
                if (type == "status")
                {
                    arguments = trimmed.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                    return;
                }

                if (model.CommandType == CommandModel.CommandTypes.TwoArgs
                    || model.CommandType == CommandModel.CommandTypes.TwoArgsAndChannel)
                {
                    ParseMultipleArguments(trimmed);
                    return;
                }
            }

            arguments = new[] {trimmed};
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether the current input has a command.
        /// </summary>
        public bool HasCommand { get; } = true;

        /// <summary>
        ///     Gets the type.
        /// </summary>
        public string Type => HasCommand ? type.ToLower() : "none";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Determines if the input is a command which is not sent to the server, but still handled
        ///     differently from normal input.
        /// </summary>
        /// <param name="input">
        ///     The input.
        /// </param>
        public static bool HasNonCommand(string input)
        {
            return CommandDefinitions.NonCommandCommands.Keys.Any(input.StartsWith);
        }

        /// <summary>
        ///     Turns a user command to a send-able dictionary.
        /// </summary>
        public IDictionary<string, object> ToDictionary()
        {
            return CommandDefinitions.CreateCommand(Type, arguments, currentChannel).ToDictionary();
        }

        #endregion

        #region Private Methods

        private void ParseMultipleArguments(string input)
        {
            var parser = new TextFieldParser(new StringReader(input))
            {
                TextFieldType = FieldType.Delimited
            };

            parser.SetDelimiters(",");

            arguments = parser.ReadFields().Select(x => x.Trim()).ToList();
            parser.Close();
        }

        #endregion
    }
}