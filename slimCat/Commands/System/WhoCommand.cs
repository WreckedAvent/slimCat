#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WhoCommand.cs">
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
    using System.Linq;
    using System.Windows.Navigation;
    using Microsoft.Practices.ObjectBuilder2;
    using Models;
    using Utilities;

    #endregion

    public partial class UserCommandService
    {
        private void OnWhoInformationRequested(IDictionary<string, object> command)
        {
            events.GetEvent<ErrorEvent>()
                .Publish(
                    "Server, server, across the sea,\nWho is connected, most to thee?\nWhy, "
                    + model.CurrentCharacter.Name + " be!");
        }

        private void OnWhoIsRequested(IDictionary<string, object> command)
        {
            var character = command.Get(Constants.Arguments.Character);

            var names = model.CurrentChannels
                .Where(channel => channel.CharacterManager.IsOnList(character, ListKind.Online))
                .Select(x => x.Title)
                .JoinStrings(", ");

            if (string.IsNullOrEmpty(names))
                names = "no shared channels";

            events.GetEvent<ErrorEvent>().Publish("User '{0}' present in {1}".FormatWith(character, names));
        }
    }
}