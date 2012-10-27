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
