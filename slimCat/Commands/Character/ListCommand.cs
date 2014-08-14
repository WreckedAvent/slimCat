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

namespace slimCat.Models
{
    public class CharacterListChangedEventArgs : CharacterUpdateEventArgs
    {

        public bool IsAdded { get; set; }


        public bool IsTemporary { get; set; }


        public ListKind ListArgument { get; set; }


        public override string ToString()
        {
            var listKind = ListArgument != ListKind.NotInterested
                ? ListArgument.ToString()
                : "not interested";

            return "has been " + (IsAdded ? "added to" : "removed from") + " your " + listKind + " list"
                   + (IsTemporary ? " until this character logs out" : string.Empty) + '.';
        }
    }
}

namespace slimCat.Services
{
    using Models;
    using SimpleJson;
    using System;
    using System.Collections.Generic;
    using Utilities;

    public partial class ServerCommandService
    {
        private void AdminsListCommand(IDictionary<string, object> command)
        {
            CharacterManager.Set(command.Get<JsonArray>(Constants.Arguments.MultipleModerators), ListKind.Moderator);

            if (CharacterManager.IsOnList(ChatModel.CurrentCharacter.Name, ListKind.Moderator, false))
                Dispatcher.Invoke((Action)delegate { ChatModel.IsGlobalModerator = true; });
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
            var arr = (JsonArray)command[Constants.Arguments.MultipleCharacters];
            foreach (JsonArray character in arr)
            {
                ICharacter temp = new CharacterModel();

                temp.Name = (string)character[0]; // Character's name

                temp.Gender = ParseGender((string)character[1]); // character's gender

                temp.Status = character[2].ToEnum<StatusType>();

                // Character's status
                temp.StatusMessage = (string)character[3]; // Character's status message

                CharacterManager.SignOn(temp);
            }
        }

    }
}
