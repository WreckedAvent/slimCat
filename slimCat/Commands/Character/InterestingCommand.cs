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
    using System.Collections.Generic;
    using Utilities;

    public partial class UserCommandService
    {
        private void OnMarkInterestingNot(IDictionary<string, object> command, bool isInteresting)
        {
            var args = command.Get(Constants.Arguments.Character);
            var type = isInteresting ? ListKind.Interested : ListKind.NotInterested;

            var isAdd = !characterManager.IsOnList(args, type);
            if (isAdd)
                characterManager.Add(args, type);
            else
                characterManager.Remove(args, type);


            var updateArgs = new CharacterListChangedEventArgs
            {
                IsAdded = isAdd,
                ListArgument = type
            };

            events.NewCharacterUpdate(characterManager.Find(args), updateArgs);
        }

        private void OnMarkInterestedRequested(IDictionary<string, object> command)
        {
            OnMarkInterestingNot(command, true);
        }

        private void OnTemporaryInterestedRequested(IDictionary<string, object> command)
        {
            var character = command.Get(Constants.Arguments.Character).ToLower().Trim();
            var add = command.Get(Constants.Arguments.Type) == "tempinteresting";

            characterManager.Add(
                character,
                add ? ListKind.Interested : ListKind.NotInterested,
                true);
        }

        private void OnMarkNotInterestedRequested(IDictionary<string, object> command)
        {
            OnMarkInterestingNot(command, false);
        } 

    }
}
