#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JoinCommand.cs">
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

    using Services;
    using Utilities;

    #endregion

    public class JoinLeaveEventArgs : CharacterUpdateInChannelEventArgs
    {
        public bool Joined { get; set; }

        public override string ToString()
        {
            return "has " + (Joined ? "joined" : "left") + $" {TargetChannel}.";
        }

        public override void DisplayNewToast(IChatState chatState, IManageToasts toastsManager)
        {
            var settings = chatState.GetChannelSettingsById(TargetChannelId);
            if (settings == null) return;

            var setting = new ChannelSettingPair(settings.JoinLeaveNotifyLevel,
                settings.JoinLeaveNotifyOnlyForInteresting);
            DoToast(setting, toastsManager, chatState);
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
        private void AutoJoinChannelCommand(IDictionary<string, object> command)
        {
            var id = command.Get(Constants.Arguments.Channel);

            var title = command.Get(Constants.Arguments.Title);

            var characterDict = command.Get<IDictionary<string, object>>(Constants.Arguments.Character);
            var character = characterDict.Get(Constants.Arguments.Identity);

            if (character != ChatModel.CurrentCharacter.Name || !autoJoinedChannels.Contains(id))
            {
                JoinChannelCommand(command);
                return;
            }

            manager.QuickJoinChannel(id, title);

            autoJoinedChannels.Remove(id);
        }

        private void QuickJoinChannelCommand(IDictionary<string, object> command)
        {
            var title = command.Get(Constants.Arguments.Title);
            var id = command.Get(Constants.Arguments.Channel);

            var characterDict = command.Get<IDictionary<string, object>>(Constants.Arguments.Character);
            var character = characterDict.Get(Constants.Arguments.Identity);

            if (character != ChatModel.CurrentCharacter.Name)
            {
                RequeueCommand(command);
                return;
            }

            var kind = ChannelType.Public;
            if (id.Contains("ADH-"))
                kind = ChannelType.Private;

            manager.JoinChannel(kind, id, title);
        }

        private new void JoinChannelCommand(IDictionary<string, object> command)
        {
            var title = command.Get(Constants.Arguments.Title);
            var id = command.Get(Constants.Arguments.Channel);

            var characterDict = command.Get<IDictionary<string, object>>(Constants.Arguments.Character);
            var character = characterDict.Get(Constants.Arguments.Identity);

            // JCH is used in a few situations. It is used when others join a channel and when we join a channel

            // if this is a situation where we are joining a channel...
            var channel = ChatModel.CurrentChannels.FirstByIdOrNull(id);
            if (channel == null)
            {
                var kind = ChannelType.Public;
                if (id.Contains("ADH-"))
                    kind = ChannelType.Private;

                manager.JoinChannel(kind, id, title);
            }
            else
            {
                var toAdd = CharacterManager.Find(character);
                if (!channel.CharacterManager.SignOn(toAdd)) return;

                var update = new CharacterUpdateModel(
                    toAdd,
                    new JoinLeaveEventArgs
                    {
                        Joined = true,
                        TargetChannel = channel.Title,
                        TargetChannelId = channel.Id
                    });

                Events.GetEvent<NewUpdateEvent>().Publish(update);
            }
        }
    }

    public partial class UserCommandService
    {
        private void OnJoinRequested(IDictionary<string, object> command)
        {
            var channelName = command.Get(Constants.Arguments.Channel);

            if (model.CurrentChannels.FirstByIdOrNull(channelName) != null)
            {
                events.GetEvent<RequestChangeTabEvent>().Publish(channelName);
                return;
            }

            var isExact = channelName.StartsWith("\"") && channelName.EndsWith("\"");

            ChannelModel guess = null;
            if (isExact)
            {
                channelName = channelName.Substring(1, channelName.Length - 2);
            }
            else
            {
                guess =
                    model.AllChannels.OrderBy(channel => channel.Title)
                        .FirstOrDefault(channel => channel.Title.StartsWith(channelName, true, null));
            }

            var toJoin = guess != null ? guess.Id : channelName;
            var toSend = new {channel = toJoin};

            connection.SendMessage(toSend, Constants.ClientCommands.ChannelJoin);
        }

        private void OnChannelRejoinRequested(IDictionary<string, object> command)
        {
            var channelName = command.Get(Constants.Arguments.Channel);
            channelService.RemoveChannel(channelName, true);

            var toSend = new {channel = channelName};
            connection.SendMessage(toSend, Constants.ClientCommands.ChannelJoin);
        }
    }
}