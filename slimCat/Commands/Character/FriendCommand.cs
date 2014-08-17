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
        // implementation is in f-list connection, these just prevent us from sending junk to chat server

        private void OnFriendRemoveRequested(IDictionary<string, object> command)
        {
        }

        private void OnFriendRequestAcceptRequested(IDictionary<string, object> command)
        {
            DoListAction(command, ListKind.FriendRequestReceived, false, false);
            friendRequestService.UpdatePendingRequests();
        }

        private void OnFriendRequestDenyRequested(IDictionary<string, object> command)
        {
            DoListAction(command, ListKind.FriendRequestReceived, false, false);
            friendRequestService.UpdatePendingRequests();
        }

        private void OnFriendRequestSendRequested(IDictionary<string, object> command)
        {
            DoListAction(command, ListKind.FriendRequestSent, true);
            friendRequestService.UpdateOutgoingRequests();
        }

        private void OnFriendRequestCancelRequested(IDictionary<string, object> command)
        {
            DoListAction(command, ListKind.FriendRequestSent, false);
            friendRequestService.UpdateOutgoingRequests();
        }

        private void DoListAction(IDictionary<string, object> command, ListKind list, bool isAdd, bool generateUpdate = true)
        {
            var character = command.Get(Constants.Arguments.Character);
            if (isAdd)
                characterManager.Add(character, list);
            else
                characterManager.Remove(character, list);

            if (!generateUpdate) return;

            var eventargs = new CharacterListChangedEventArgs
            {
                IsAdded = isAdd,
                ListArgument = list
            };

            events.NewCharacterUpdate(characterManager.Find(character), eventargs);
        }
    }
}
