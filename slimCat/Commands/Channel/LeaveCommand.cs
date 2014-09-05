#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LeaveCommand.cs">
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

    using System.Collections.Generic;
    using Models;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void LeaveChannelCommand(IDictionary<string, object> command)
        {
            var channelId = command.Get(Constants.Arguments.Channel);
            var characterName = command.Get(Constants.Arguments.Character);

            var channel = ChatModel.CurrentChannels.FirstByIdOrNull(channelId);
            if (ChatModel.CurrentCharacter.NameEquals(characterName))
            {
                if (channel != null)
                    manager.RemoveChannel(channelId, false, true);

                return;
            }

            if (channel == null)
                return;

            var ignoreUpdate = false;

            if (command.ContainsKey("ignoreUpdate"))
                ignoreUpdate = (bool) command["ignoreUpdate"];

            if (!channel.CharacterManager.SignOff(characterName) || ignoreUpdate) return;

            var updateArgs = new JoinLeaveEventArgs
            {
                Joined = false,
                TargetChannel = channel.Title,
                TargetChannelId = channel.Id
            };

            Events.NewCharacterUpdate(CharacterManager.Find(characterName), updateArgs);
        }
    }

    public partial class UserCommandService
    {
        private void OnCloseRequested(IDictionary<string, object> command)
        {
            channelService.RemoveChannel(command.Get(Constants.Arguments.Channel));
        }

        private void OnForceChannelCloseRequested(IDictionary<string, object> command)
        {
            var channelName = command.Get(Constants.Arguments.Channel);
            channelService.RemoveChannel(channelName, true);
        }
    }
}