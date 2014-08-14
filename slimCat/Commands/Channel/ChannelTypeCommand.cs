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
    public class ChannelTypeChangedEventArgs : ChannelUpdateEventArgs
    {
        public bool IsOpen { get; set; }

        public override string ToString()
        {
            return "is now " + (IsOpen ? "open" : "InviteOnly") + '.';
        }
    }
}

namespace slimCat.Services
{
    using Models;
    using System;
    using System.Collections.Generic;
    using Utilities;

    public partial class ServerCommandService
    {
        private void RoomTypeChangedCommand(IDictionary<string, object> command)
        {
            var channelId = command.Get(Constants.Arguments.Channel);
            var isPublic =
                (command.Get(Constants.Arguments.Message)).IndexOf("public", StringComparison.OrdinalIgnoreCase) !=
                -1;

            var channel = ChatModel.CurrentChannels.FirstByIdOrNull(channelId);

            if (channel == null)
                return; // can't change the settings of a room we don't know

            channel.Type = isPublic ? ChannelType.Private : ChannelType.InviteOnly;

            var updateArgs = new ChannelTypeChangedEventArgs()
            {
                IsOpen = isPublic
            };


            Events.NewChannelUpdate(channel, updateArgs);
        }

    }
}
