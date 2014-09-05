#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RollCommand.cs">
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
        private void RollCommand(IDictionary<string, object> command)
        {
            var channel = command.Get(Constants.Arguments.Channel);
            var message = command.Get(Constants.Arguments.Message);
            var poster = command.Get(Constants.Arguments.Character);

            if (!CharacterManager.IsOnList(poster, ListKind.Ignored))
                manager.AddMessage(message, channel, poster, MessageType.Roll);
        }
    }
}