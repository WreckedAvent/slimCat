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
    public class ChannelDescriptionChangedEventArgs : ChannelUpdateEventArgs
    {
        public override string ToString()
        {
            return "has a new description.";
        }
    }
}


namespace slimCat.Services
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using Utilities;

    public partial class ServerCommandService
    {
        private void ChannelDescriptionCommand(IDictionary<string, object> command)
        {
            var channel = FindChannel(command);
            var description = command.Get("description");

            if (channel == null)
            {
                RequeueCommand(command);
                return;
            }

            var isInitializer = string.IsNullOrWhiteSpace(channel.Description);

            if (string.Equals(channel.Description, description, StringComparison.Ordinal))
                return;

            channel.Description = description;

            if (isInitializer)
                return;

            var args = new ChannelDescriptionChangedEventArgs();
            Events.NewChannelUpdate(channel, args);
        }
    }

    public partial class UserCommandService
    {
        private void OnChannelDescriptionRequested(IDictionary<string, object> command)
        {
            if (model.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
            {
                events.GetEvent<ErrorEvent>()
                    .Publish("Poor home channel, with no description to speak of...");
                return;
            }

            if (model.CurrentChannel is GeneralChannelModel)
            {
                Clipboard.SetData(
                    DataFormats.Text, (model.CurrentChannel as GeneralChannelModel).Description);
                events.GetEvent<ErrorEvent>()
                    .Publish("Channel's description copied to clipboard.");
            }
            else
                events.GetEvent<ErrorEvent>().Publish("Hey! That's not a channel.");
        }
    }
}
