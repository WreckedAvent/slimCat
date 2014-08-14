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
    public class ChannelModeUpdateEventArgs : ChannelUpdateEventArgs
    {
        public ChannelMode NewMode { get; set; }

        public override string ToString()
        {
            if (NewMode != ChannelMode.Both)
                return "now only allows " + NewMode + '.';

            return "now allows Ads and chatting.";
        }
    }
}

namespace slimCat.Services
{
    using Models;
    using System.Collections.Generic;
    using Utilities;

    public partial class ServerCommandService
    {
        private void RoomModeChangedCommand(IDictionary<string, object> command)
        {
            var channelId = command.Get(Constants.Arguments.Channel);
            var mode = command.Get(Constants.Arguments.Mode);

            var newMode = mode.ToEnum<ChannelMode>();
            var channel = ChatModel.CurrentChannels.FirstByIdOrNull(channelId);

            if (channel == null)
                return;

            channel.Mode = newMode;
            Events.NewChannelUpdate(channel, new ChannelModeUpdateEventArgs { NewMode = newMode });
        }

    }
}
