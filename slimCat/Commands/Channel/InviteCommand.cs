#region Copyright

// <copyright file="InviteCommand.cs">
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

    using Utilities;

    #endregion

    public class ChannelInviteEventArgs : ChannelUpdateEventArgs
    {
        public string Inviter { get; set; }

        public override string ToString()
        {
            return "{0} has invited you to join {1}".FormatWith(WrapInUser(Inviter), GetChannelBbCode());
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

    public partial class UserCommandService
    {
        private void OnInviteToChannelRequested(IDictionary<string, object> command)
        {
            if (command.ContainsKey(Constants.Arguments.Character) &&
                cm.CurrentCharacter.NameEquals(command.Get(Constants.Arguments.Character)))
            {
                events.NewError("You don't need my help to talk to yourself.");
                return;
            }

            connection.SendMessage(command);
        }
    }

    public partial class ServerCommandService
    {
        private void InviteCommand(IDictionary<string, object> command)
        {
            var sender = command.Get(Constants.Arguments.Sender);
            var id = command.Get(Constants.Arguments.Name);
            var title = command.Get(Constants.Arguments.Title);

            var args = new ChannelInviteEventArgs {Inviter = sender};
            Events.NewChannelUpdate(ChatModel.FindChannel(id, title), args);
        }
    }
}