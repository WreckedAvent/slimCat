#region Copyright

// <copyright file="PrivateMessageCommand.cs">
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
    using Utilities;

    #endregion

    public partial class UserCommandService
    {
        private void OnPrivRequested(IDictionary<string, object> command)
        {
            var characterName = command.Get(Constants.Arguments.Character);
            if (cm.CurrentCharacter.NameEquals(characterName))
            {
                events.NewError("Hmmm... talking to yourself?");
                return;
            }

            var isExact = characterName.StartsWith("\"") && characterName.EndsWith("\"");

            ICharacter guess = null;
            if (isExact)
            {
                characterName = characterName.Substring(1, characterName.Length - 2);
            }
            else
            {
                guess = characterManager.SortedCharacters.OrderBy(x => x.Name)
                    .Where(x => !x.NameEquals(cm.CurrentCharacter.Name))
                    .FirstOrDefault(c => c.Name.StartsWith(characterName, true, null));
            }

            channels.JoinChannel(ChannelType.PrivateMessage, guess == null ? characterName : guess.Name);
        }

        private void OnPivateMessageSendRequested(IDictionary<string, object> command)
        {
            channels.AddMessage(
                command.Get(Constants.Arguments.Message),
                command.Get("recipient"),
                Constants.Arguments.ThisCharacter);
            connection.SendMessage(command);
        }
    }

    public partial class ServerCommandService
    {
        private void PrivateMessageCommand(IDictionary<string, object> command)
        {
            var sender = command.Get(Constants.Arguments.Character);
            if (!CharacterManager.IsOnList(sender, ListKind.Ignored))
            {
                if (CharacterManager.IsOnList(sender, ListKind.ClientIgnored))
                {
                    // client ignored, so swallow PM
                    return;
                }

                if (ChatModel.CurrentPms.FirstByIdOrNull(sender) == null)
                    channels.AddChannel(ChannelType.PrivateMessage, sender);

                channels.AddMessage(command.Get(Constants.Arguments.Message), sender, sender);

                var temp = ChatModel.CurrentPms.FirstByIdOrNull(sender);
                if (temp == null)
                    return;

                temp.TypingStatus = TypingStatus.Clear; // webclient assumption
            }
            else
            {
                ChatConnection.SendMessage(
                    new Dictionary<string, object>
                    {
                        {Constants.Arguments.Action, Constants.Arguments.ActionNotify},
                        {Constants.Arguments.Character, sender},
                        {Constants.Arguments.Type, Constants.ClientCommands.UserIgnore}
                    });
            }
        }

        private void TypingStatusCommand(IDictionary<string, object> command)
        {
            var sender = command.Get(Constants.Arguments.Character);

            var channel = ChatModel.CurrentPms.FirstByIdOrNull(sender);
            if (channel == null)
                return;

            var type = command.Get("status").ToEnum<TypingStatus>();

            channel.TypingStatus = type;
        }
    }
}