#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DisciplineCommand.cs">
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

namespace slimCat.Models
{
    #region Usings

    using Utilities;

    #endregion

    public class ChannelDisciplineEventArgs : ChannelUpdateEventArgs
    {
        public bool IsBan { get; set; }

        public string Kicked { get; set; }

        public string Kicker { get; set; }

        public override string ToString()
        {
            return "{0} has {1} {2} from {3}".FormatWith(WrapInUser(Kicker), IsBan ? "banned" : "kicked", Kicked,
                GetChannelBbCode());
        }
    }
}

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void KickCommand(IDictionary<string, object> command)
        {
            var kicker = command.Get("operator");
            var channelId = command.Get(Constants.Arguments.Channel);
            var kicked = command.Get(Constants.Arguments.Character);
            var isBan = command.Get(Constants.Arguments.Command) == Constants.ServerCommands.ChannelBan;
            var channel = ChatModel.CurrentChannels.FirstByIdOrNull(channelId);

            if (channel == null)
                RequeueCommand(command);

            if (ChatModel.CurrentCharacter.NameEquals(kicked))
                kicked = "you";

            var args = new ChannelDisciplineEventArgs
            {
                IsBan = isBan,
                Kicked = kicked,
                Kicker = kicker
            };
            var update = new ChannelUpdateModel(channel, args);

            if (kicked == "you")
                manager.RemoveChannel(channelId);
            else
                channel.CharacterManager.SignOff(kicked);

            if (isBan)
            {
                channel.CharacterManager.Add(kicked, ListKind.Banned);
            }

            Events.GetEvent<NewUpdateEvent>().Publish(update);
        }
    }
}