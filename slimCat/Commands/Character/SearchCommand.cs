#region Copyright

// <copyright file="SearchCommand.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using SimpleJson;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void SearchResultCommand(IDictionary<string, object> command)
        {
            var resultsList = ((JsonArray) command[Constants.Arguments.MultipleCharacters]).ToList();
            CharacterManager.Set(new JsonArray(), ListKind.SearchResult);

            foreach (string character in resultsList)
            {
                CharacterManager.Add(character, ListKind.SearchResult);
            }

            Events.GetEvent<ChatSearchResultEvent>().Publish(resultsList.Any());
            Events.GetEvent<ErrorEvent>()
                .Publish(resultsList.Any()
                    ? "Got search results successfully."
                    : "Got search results, but with no relevant characters.");
        }
    }

    public partial class UserCommandService
    {
        private void OnSearchTagToggleRequested(IDictionary<string, object> command)
        {
            var character = command.Get(Constants.Arguments.Character);

            var isAdd = !characterManager.IsOnList(character, ListKind.SearchResult, false);

            if (isAdd)
            {
                characterManager.Add(character, ListKind.SearchResult);
            }
            else
            {
                characterManager.Remove(character, ListKind.SearchResult);
            }

            var updateArgs = new CharacterListChangedEventArgs
            {
                IsAdded = isAdd,
                ListArgument = ListKind.SearchResult
            };

            events.NewCharacterUpdate(characterManager.Find(character), updateArgs);
        }
    }
}