#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogInCommand.cs">
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

namespace slimCat.Models
{
    #region Usings

    using Services;
    using Utilities;

    #endregion

    /// <summary>
    ///     The login state changed event args.
    /// </summary>
    public class LoginStateChangedEventArgs : CharacterUpdateEventArgs
    {
        public bool IsLogIn { get; set; }

        public override string ToString()
        {
            return "has logged " + (IsLogIn ? "in." : "out.");
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            if (!ApplicationSettings.ShowLoginToasts
                || !chatState.IsInteresting(Model.TargetCharacter.Name))
            { return; }

            DoNormalToast(toastsManager);
        }
    }
}

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void UserLoggedInCommand(IDictionary<string, object> command)
        {
            var character = command.Get(Constants.Arguments.Character);

            var temp = new CharacterModel
            {
                Name = character,
                Gender = ParseGender(command.Get(Constants.Arguments.Gender)),
                Status = command.Get(Constants.Arguments.Status).ToEnum<StatusType>()
            };

            CharacterManager.SignOn(temp);

            Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        temp, new LoginStateChangedEventArgs {IsLogIn = true}));
        }
    }
}