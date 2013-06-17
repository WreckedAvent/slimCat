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

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;

namespace System
{
    /// <summary>
    /// This is responsible for translating text and interpreting commands
    /// </summary>
    public class CommandParser
    {
        #region Fields
        readonly string _args;
        readonly string _type;
        readonly string _currentChan;
        readonly bool _hasCommand = true;
        readonly bool _hasBadSyntx = false;
        #endregion

        #region Constructors
        public CommandParser(string rawInput, string currentChannel)
        {
            if (!rawInput.StartsWith("/"))
                _hasCommand = false;

            string type;
            string arguments;

            if (HasCommand)
            {

                if (!rawInput.Trim().Contains(' '))
                    _type = rawInput.Trim().Substring(1);
                else
                {
                    int firstSpace = rawInput.Substring(1).IndexOf(' ');
                    type = rawInput.Substring(1, firstSpace);

                    arguments = rawInput.Substring(firstSpace + 2);

                    if (type.Equals("status", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!arguments.Contains(' '))
                            _hasBadSyntx = true;
                        else
                        {
                            type = arguments.Substring(0, arguments.IndexOf(' '));
                            arguments = arguments.Substring(arguments.IndexOf(' ') + 1);
                        }
                    }

                    if (type.Contains('_'))
                        _hasBadSyntx = true;

                    _type = type;
                    _args = arguments;
                }

                _currentChan = currentChannel;
            }
        }
        #endregion

        #region Properties
        public bool HasCommand
        {
            get { return _hasCommand; }
        }

        public bool IsValid
        {
            get { return !_hasBadSyntx && HasCommand && CommandDefinitions.IsValidCommand(Type); }
        }

        public bool RequiresMod
        {
            get
            {
                if (HasCommand && IsValid)
                    return CommandDefinitions.GetCommandModelFromName(Type).PermissionsLevel == CommandModel.PermissionLevel.Moderator;
                else
                    return false;
            }
        }

        public bool RequiresGlobalMod
        {
            get
            {
                if (HasCommand && IsValid)
                    return CommandDefinitions.Commands[Type].PermissionsLevel == CommandModel.PermissionLevel.GlobalMod;
                else
                    return false;
            }
        }

        public string Type
        {
            get { return (HasCommand ? _type.ToLower() : "none"); }
        }
        #endregion

        #region Methods
        public static bool HasNonCommand(string input)
        {
            return CommandDefinitions.NonCommandCommands.Any(noncommand => input.StartsWith(noncommand));
        }

        public IDictionary<string, object> toDictionary()
        {
            return CommandDefinitions.CreateCommand(Type, new List<string>() { _args }, _currentChan).toDictionary();
        }
        #endregion
    }
}
