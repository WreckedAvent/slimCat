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

namespace slimCat.Models
{
    #region Usings

    using Services;

    #endregion

    public class ChannelMentionUpdateEventArgs : CharacterUpdateEventArgs
    {
        private object[] Args => new object[]
        {
            ApplicationSettings.ShowNamesInToasts ? Model.TargetCharacter.Name : "A user",
            TriggeredWord,
            Channel.Title
        };

        public string TriggeredWord { get; set; }
        public bool IsNameMention { get; set; }
        public string Context { get; set; }
        public ChannelModel Channel { get; set; }

        public string Title
            => string.Format(IsNameMention ? "{0}'s name matches {1} #{2}" : "{0} mentioned {1} #{2}", Args);

        public override string ToString()
            => string.Format(IsNameMention ? "'s name matches {1} in {2}" : "mentioned {1} in {2}", Args) + ": \"" +
               Context + "\"";

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            SetToastData(toastsManager.Toast);

            toastsManager.Toast.Title = Title;
            toastsManager.Toast.Navigator = new SimpleNavigator(chat =>
                chat.EventAggregator.GetEvent<RequestChangeTabEvent>().Publish(Channel.Id));
            toastsManager.Toast.Content = Context;

            toastsManager.AddNotification(Model);

            if (Channel.IsSelected)
            {
                if (ApplicationSettings.PlaySoundEvenWhenTabIsFocused)
                    toastsManager.PlaySound();
                return;
            }

            toastsManager.ShowToast();
            toastsManager.PlaySound();
            toastsManager.FlashWindow();
        }
    }
}

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;
    using Utilities;
    using System.Linq;

    #endregion

    public partial class ServerCommandService
    {
        private void ChannelMessageCommand(IDictionary<string, object> command)
        {
            MessageReceived(command, false);
        }

        private void MessageReceived(IDictionary<string, object> command, bool isAd)
        {
            var character = command.Get(Constants.Arguments.Character);
            var message = command.Get(Constants.Arguments.Message);
            var channel = command.Get(Constants.Arguments.Channel);

            // dedupe logic
            if (isAd && automation.IsDuplicateAd(character, message))
                return;

            if (ApplicationSettings.GlobalPruningTermsList.Any(x => message.ContainsOrdinal(x)))
                return;

            if (!CharacterManager.IsOnList(character, ListKind.Ignored))
                channels.AddMessage(message, channel, character, isAd ? MessageType.Ad : MessageType.Normal);
        }
    }

    public partial class UserCommandService
    {
        private void OnMsgRequested(IDictionary<string, object> command)
        {
            channels.AddMessage(
                command.Get(Constants.Arguments.Message),
                command.Get(Constants.Arguments.Channel),
                Constants.Arguments.ThisCharacter);
            connection.SendMessage(command);
        }
    }
}