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

    /// <summary>
    ///     The login state changed event args.
    /// </summary>
    public class LoginStateChangedEventArgs : CharacterUpdateEventArgs
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets a value indicating whether is log in.
        /// </summary>
        public bool IsLogIn { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The to string.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string ToString()
        {
            return "has logged " + (IsLogIn ? "in." : "out.");
        }

        #endregion
    }
}

namespace slimCat.Services
{
    using Models;
    using System.Collections.Generic;
    using Utilities;

    public partial class ServerCommandService
    {


        private void UserLoggedInCommand(IDictionary<string, object> command)
        {
            var character = command.Get(Constants.Arguments.Identity);

            var temp = new CharacterModel
            {
                Name = character,
                Gender = ParseGender(command.Get("gender")),
                Status = command.Get("status").ToEnum<StatusType>()
            };

            CharacterManager.SignOn(temp);

            Events.GetEvent<NewUpdateEvent>()
                .Publish(
                    new CharacterUpdateModel(
                        temp, new LoginStateChangedEventArgs { IsLogIn = true }));
        }

    }
}
