#region Copyright

// <copyright file="AdCommand.cs">
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

    public partial class ServerCommandService
    {
        private void AdMessageCommand(IDictionary<string, object> command)
        {
            MessageReceived(command, true);
        }
    }

    public partial class UserCommandService
    {
        private void OnLrpRequested(IDictionary<string, object> command)
        {
            channels.AddMessage(
                command.Get(Constants.Arguments.Message),
                command.Get(Constants.Arguments.Channel),
                Constants.Arguments.ThisCharacter,
                MessageType.Ad);
            connection.SendMessage(command);
        }
    }
}