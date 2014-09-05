#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IgnoreCommand.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using SimpleJson;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void IgnoreUserCommand(IDictionary<string, object> command)
        {
            Action<string> doAction;
            if (command.Get(Constants.Arguments.Action) != Constants.Arguments.ActionDelete)
                doAction = x => CharacterManager.Add(x, ListKind.Ignored);
            else
                doAction = x => CharacterManager.Remove(x, ListKind.Ignored);

            if (command.ContainsKey(Constants.Arguments.Character))
            {
                var character = command.Get(Constants.Arguments.Character);
                if (character != null)
                {
                    doAction(character);
                    return;
                }
            }

            var characters = command.Get<JsonArray>(Constants.Arguments.MultipleCharacters);
            if (characters != null)
                CharacterManager.Set(characters.Select(x => x as string), ListKind.Ignored);
        }
    }

    public partial class UserCommandService
    {
        private void OnIgnoreRequested(IDictionary<string, object> command)
        {
            if (!command.ContainsKey(Constants.Arguments.Character))
            {
                connection.SendMessage(command);
                return;
            }

            var args = command.Get(Constants.Arguments.Character);

            var action = command.Get(Constants.Arguments.Action);

            if (action == Constants.Arguments.ActionAdd)
                characterManager.Add(args, ListKind.Ignored);
            else if (action == Constants.Arguments.ActionDelete)
                characterManager.Remove(args, ListKind.Ignored);
            else
                return;

            var updateArgs = new CharacterListChangedEventArgs
            {
                IsAdded = action == Constants.Arguments.ActionAdd,
                ListArgument = ListKind.Ignored
            };

            events.NewCharacterUpdate(characterManager.Find(args), updateArgs);

            connection.SendMessage(command);
        }

        private void OnTemporaryIgnoreRequested(IDictionary<string, object> command)
        {
            var character = command.Get(Constants.Arguments.Character).ToLower().Trim();
            var add = command.Get(Constants.Arguments.Type) == "tempignore";

            if (add)
                characterManager.Add(character, ListKind.Ignored, true);
            else
                characterManager.Remove(character, ListKind.Ignored, true);
        }
    }
}