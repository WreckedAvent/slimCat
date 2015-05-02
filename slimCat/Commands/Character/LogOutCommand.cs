#region Copyright

// <copyright file="LogOutCommand.cs">
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

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Practices.Prism.Regions;
    using Models;
    using Utilities;
    using ViewModels;

    #endregion

    public partial class UserCommandService
    {
        private void OnLogoutRequested(IDictionary<string, object> command)
        {
            connection.Disconnect();
            model.IsAuthenticated = false;
            regionManager.RequestNavigate(Shell.MainRegion,
                new Uri(CharacterSelectViewModel.CharacterSelectViewName, UriKind.Relative));
        }
    }

    public partial class ServerCommandService
    {
        private void CharacterDisconnectCommand(IDictionary<string, object> command)
        {
            var characterName = command.Get(Constants.Arguments.Character);

            var character = CharacterManager.Find(characterName);
            var ofInterest = CharacterManager.IsOfInterest(characterName);

            character.LastAd = null;
            character.LastReport = null;

            CharacterManager.SignOff(characterName);

            var leaveChannelCommands =
                from channel in ChatModel.CurrentChannels
                where channel.CharacterManager.SignOff(characterName)
                select new Dictionary<string, object>
                {
                    {Constants.Arguments.Character, character.Name},
                    {Constants.Arguments.Channel, channel.Id},
                    {"ignoreUpdate", ofInterest}
                    // ignore updates from characters we'll already get a sign-out notice for
                };

            leaveChannelCommands.Each(LeaveChannelCommand);

            var characterChannel = ChatModel.CurrentPms.FirstByIdOrNull(characterName);
            if (characterChannel != null)
                characterChannel.TypingStatus = TypingStatus.Clear;

            var updateArgs = new LoginStateChangedEventArgs
            {
                IsLogIn = false
            };

            Events.NewCharacterUpdate(character, updateArgs);
        }
    }
}