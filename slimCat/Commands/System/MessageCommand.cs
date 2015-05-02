#region Copyright

// <copyright file="MessageCommand.cs">
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
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void ErrorCommand(IDictionary<string, object> command)
        {
            var thisMessage = command.Get(Constants.Arguments.Message);

            // for some fucktarded reason room status changes are only done through SYS
            if (thisMessage.ContainsOrdinal("this channel is now"))
            {
                RoomTypeChangedCommand(command);
                return;
            }

            // checks to see if this is a channel ban message
            if (thisMessage.ContainsOrdinal("channel ban"))
            {
                ChannelBanListCommand(command);
                return;
            }

            // checks to ensure it's not a mod promote message
            if (!thisMessage.ContainsOrdinal("has been promoted") && !thisMessage.ContainsOrdinal("Invalid ticket"))
                Events.GetEvent<ErrorEvent>().Publish(thisMessage);
        }
    }
}