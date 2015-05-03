#region Copyright

// <copyright file="ListCommand.cs">
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

namespace slimCat.Models
{
    #region Usings

    using Services;

    #endregion

    public class CharacterListChangedEventArgs : CharacterUpdateEventArgs
    {
        public bool IsAdded { get; set; }
        public bool IsTemporary { get; set; }
        public ListKind ListArgument { get; set; }

        public override string ToString()
        {
            var listKind = ListArgument.ToString();

            if (ListArgument == ListKind.NotInterested) listKind = "not interested";
            if (ListArgument == ListKind.SearchResult) listKind = "search result";

            if (ListArgument == ListKind.FriendRequestReceived)
            {
                return "has sent you a friend request";
            }

            if (ListArgument == ListKind.FriendRequestSent)
            {
                return IsAdded ? "has received your friend request" : "no longer has your friend request";
            }

            return "has been " + (IsAdded ? "added to" : "removed from") + " your " + listKind + " list"
                   + (IsTemporary ? " until this character logs out" : string.Empty) + '.';
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            if (ListArgument == ListKind.NotInterested) return;
            DoNormalToast(toastsManager);
        }
    }
}

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;
    using SimpleJson;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void AdminsListCommand(IDictionary<string, object> command)
        {
            CharacterManager.Set(command.Get<JsonArray>(Constants.Arguments.MultipleModerators), ListKind.Moderator);

            if (CharacterManager.IsOnList(ChatModel.CurrentCharacter.Name, ListKind.Moderator, false))
                Dispatcher.Invoke(() => ChatModel.IsGlobalModerator = true);
        }

        private void ChannelOperatorListCommand(IDictionary<string, object> command)
        {
            var channel = FindChannel(command);

            if (channel == null)
            {
                RequeueCommand(command);
                return;
            }

            channel.CharacterManager.Set(command.Get<JsonArray>("oplist"), ListKind.Moderator);
        }

        private void InitialCharacterListCommand(IDictionary<string, object> command)
        {
            var arr = (JsonArray) command[Constants.Arguments.MultipleCharacters];
            foreach (JsonArray character in arr)
            {
                ICharacter temp = new CharacterModel();

                temp.Name = (string) character[0]; // Character's name

                temp.Gender = ((string) character[1]).ParseGender(); // character's gender

                temp.Status = character[2].ToEnum<StatusType>();

                // Character's status
                temp.StatusMessage = (string) character[3]; // Character's status message

                CharacterManager.SignOn(temp);
            }
        }
    }
}