#region Copyright

// <copyright file="FriendCommand.cs">
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
    using Models;
    using Utilities;

    #endregion

    public partial class UserCommandService
    {
        // implementation is in f-list connection, these just prevent us from sending junk to chat server

        private void OnFriendRemoveRequested(IDictionary<string, object> command)
        {
        }

        private void OnFriendRequestAcceptRequested(IDictionary<string, object> command)
        {
            DoListAction(command.Get(Constants.Arguments.Character), ListKind.FriendRequestReceived, false, false);
        }

        private void OnFriendRequestDenyRequested(IDictionary<string, object> command)
        {
            DoListAction(command.Get(Constants.Arguments.Character), ListKind.FriendRequestReceived, false, false);
        }

        private void OnFriendRequestSendRequested(IDictionary<string, object> command)
        {
            DoListAction(command.Get(Constants.Arguments.Character), ListKind.FriendRequestSent, true);
        }

        private void OnFriendRequestCancelRequested(IDictionary<string, object> command)
        {
            DoListAction(command.Get(Constants.Arguments.Character), ListKind.FriendRequestSent, false);
        }

        private void DoListAction(string name, ListKind list, bool isAdd, bool generateUpdate = true)
        {
            Dispatcher.Invoke(() =>
            {
                var result = isAdd
                    ? characterManager.Add(name, list)
                    : characterManager.Remove(name, list);

                var character = characterManager.Find(name);
                if (isAdd && character.Status == StatusType.Offline)
                {
                    characterManager.SignOff(name);
                }

                character.IsInteresting = characterManager.IsOfInterest(name);

                if (!generateUpdate || !result) return;

                var eventargs = new CharacterListChangedEventArgs
                {
                    IsAdded = isAdd,
                    ListArgument = list
                };

                events.NewCharacterUpdate(character, eventargs);
            });
        }
    }
}