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

    public partial class ServerCommandService
    {
        private void ChannelMessageCommand(IDictionary<string, object> command)
        {
            MessageReceived(command, false);
        }

        private void MessageReceived(IDictionary<string, object> command, bool isAd)
        {
            var character = command.Get(Constants.Arguments.Character);
            var message = command.Get(Constants.Arguments.Message);
            var channel = command.Get(Constants.Arguments.Channel);

            // dedupe logic
            if (isAd && automation.IsDuplicateAd(character, message))
                return;

            if (!CharacterManager.IsOnList(character, ListKind.Ignored))
                manager.AddMessage(message, channel, character, isAd ? MessageType.Ad : MessageType.Normal);
        }
    }

    public partial class UserCommandService
    {
        private void OnMsgRequested(IDictionary<string, object> command)
        {
            channelManager.AddMessage(
                command.Get(Constants.Arguments.Message),
                command.Get(Constants.Arguments.Channel),
                Constants.Arguments.ThisCharacter);
            connection.SendMessage(command);
        }
    }
}
