#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BroadcastCommand.cs">
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

namespace slimCat.Services
{
    using Models;
    using SimpleJson;
    using System.Collections.Generic;
    using System.Linq;
    using Utilities;

    public partial class ServerCommandService
    {
        private void SearchResultCommand(IDictionary<string, object> command)
        {
            var characters = (JsonArray) command[Constants.Arguments.MultipleCharacters];
            CharacterManager.Set(new JsonArray(), ListKind.SearchResult);

            foreach (string character in characters.Where(x => !CharacterManager.IsOnList((string)x, ListKind.NotInterested)))
            {
                CharacterManager.Add(character, ListKind.SearchResult);
            }

            Events.GetEvent<ChatSearchResultEvent>().Publish(null);
            Events.GetEvent<ErrorEvent>().Publish("Got search results successfully.");
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
