#region Copyright

// <copyright file="BanListCommand.cs">
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

namespace slimCat.Models
{
    #region Usings



    #endregion

    public class ChannelTypeBannedListEventArgs : ChannelUpdateEventArgs
    {
        public override string ToString() => $"{GetChannelBbCode()}'s ban list has been updated";
    }
}

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void ChannelBanListCommand(IDictionary<string, object> command)
        {
            lock (chatStateLocker)
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
                    var banlist = msg.Split(new[] {",", ":", "has been"}, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(x => x.Trim())
                                     .ToList();

                    if (msg.ContainsOrdinal("has been"))
                    {
                        var character = banlist[0];
                        channel.CharacterManager.Remove(character, ListKind.Banned);
                        Events.NewUpdate(@event);
                        return;
                    }

                    channel.CharacterManager.Set(banlist.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)),
                        ListKind.Banned);
                    Events.NewUpdate(@event);
                    return;
                }

                var message = channelId.Split(':');
                var banned = message[1].Trim();

                if (banned.IndexOf(',') == -1)
                    channel.CharacterManager.Add(banned, ListKind.Banned);
                else
                    channel.CharacterManager.Set(banned.Split(','), ListKind.Banned);

                Events.NewUpdate(@event);
            }
        }
    }
}