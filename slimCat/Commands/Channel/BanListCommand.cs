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
    public class ChannelTypeBannedListEventArgs : ChannelUpdateEventArgs
    {
        public override string ToString()
        {
            return "'s ban list has been updated";
        }
    }
}

namespace slimCat.Services
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utilities;

    public partial class ServerCommandService
    {
        private void ChannelBanListCommand(IDictionary<string, object> command)
        {
            var channelId = command.Get(Constants.Arguments.Channel);
            var channel = ChatModel.CurrentChannels.FirstByIdOrNull(channelId);

            if (channel == null)
            {
                RequeueCommand(command);
                return;
            }

            var @event = new ChannelUpdateModel(channel, new ChannelTypeBannedListEventArgs());

            if (command.ContainsKey(Constants.Arguments.Message))
            {
                var msg = command.Get(Constants.Arguments.Message);
                var banlist = msg.Split(new[] { ",", ":", "has been" }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x => x.Trim())
                                 .ToList();

                if (msg.ContainsOrdinal("has been"))
                {
                    var character = banlist[0];
                    channel.CharacterManager.Remove(character, ListKind.Banned);
                    Events.GetEvent<NewUpdateEvent>().Publish(@event);
                    return;
                }

                channel.CharacterManager.Set(banlist.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)), ListKind.Banned);
                Events.GetEvent<NewUpdateEvent>().Publish(@event);
                return;
            }

            var message = channelId.Split(':');
            var banned = message[1].Trim();

            if (banned.IndexOf(',') == -1)
                channel.CharacterManager.Add(banned, ListKind.Banned);
            else
                channel.CharacterManager.Set(banned.Split(','), ListKind.Banned);

            Events.GetEvent<NewUpdateEvent>().Publish(@event);
        }
    }
}
