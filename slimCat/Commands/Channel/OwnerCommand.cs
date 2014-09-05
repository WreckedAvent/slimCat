#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OwnerCommand.cs">
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

    public class ChannelOwnerChangedEventArgs : ChannelUpdateEventArgs
    {
        public string NewOwner { get; set; }

        public override string ToString()
        {
            return "is now owned by [user]{0}[/user]".FormatWith(NewOwner);
        }
    }
}

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void SetNewOwnerCommand(IDictionary<string, object> command)
        {
            var character = command.Get(Constants.Arguments.Character);
            var channelId = command.Get(Constants.Arguments.Channel);

            var channel = ChatModel.CurrentChannels.FirstByIdOrNull(channelId);

            if (channel == null) return;

            var mods = channel.CharacterManager.GetNames(ListKind.Moderator, false).ToList();
            mods[0] = character;
            channel.CharacterManager.Set(mods, ListKind.Moderator);

            var update = new ChannelUpdateModel(channel, new ChannelOwnerChangedEventArgs {NewOwner = character});
            Events.GetEvent<NewUpdateEvent>().Publish(update);
        }
    }
}