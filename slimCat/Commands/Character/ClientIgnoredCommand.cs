#region Copyright

// <copyright file="IgnoreUpdatesCommand.cs">
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
        private void OnMarkClientIgnored(IDictionary<string, object> command, bool isClientIgnored)
        {
            var args = command.Get(Constants.Arguments.Character);
            var type = ListKind.ClientIgnored;

            var isAdd = !characterManager.IsOnList(args, type);
            if (isAdd)
                characterManager.Add(args, type);
            else
                characterManager.Remove(args, type);


            var updateArgs = new CharacterListChangedEventArgs
            {
                IsAdded = isAdd,
                ListArgument = type
            };

            events.NewCharacterUpdate(characterManager.Find(args), updateArgs);
        }

        private void OnMarkClientIgnoredRequested(IDictionary<string, object> command)
        {
            OnMarkClientIgnored(command, true);
        }

        private void OnMarkClientUnignoredRequested(IDictionary<string, object> command)
        {
            OnMarkClientIgnored(command, false);
        }
    }
}