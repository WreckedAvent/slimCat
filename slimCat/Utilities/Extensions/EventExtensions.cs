#region Copyright

// <copyright file="EventExtensions.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System.Collections.Generic;
    using Microsoft.Practices.Prism.Events;
    using Models;

    #endregion

    internal static class EventExtensions
    {
        /// <summary>
        ///     Sends the command as the current user. This is the same as if they had physically typed the command.
        /// </summary>
        public static void SendUserCommand(this IEventAggregator events, IDictionary<string, object> command)
            => events.GetEvent<UserCommandEvent>().Publish(command);

        /// <summary>
        ///     Sends the command as the current user. This is the same as if they had physically typed the command.
        /// </summary>
        public static void SendUserCommand(this IEventAggregator events, string commandName,
            IList<string> arguments = null, string channel = null)
            => events.SendUserCommand(CommandDefinitions.CreateCommand(commandName, arguments, channel).ToDictionary());

        public static void NewCharacterUpdate(this IEventAggregator events, ICharacter character,
            CharacterUpdateEventArgs e)
            => events.NewUpdate(new CharacterUpdateModel(character, e));

        public static void NewChannelUpdate(this IEventAggregator events, ChannelModel channel, ChannelUpdateEventArgs e)
            => events.NewUpdate(new ChannelUpdateModel(channel, e));

        public static void NewError(this IEventAggregator events, string error)
            => events.GetEvent<ErrorEvent>().Publish(error);

        public static void NewMessage(this IEventAggregator events, string message)
            => events.GetEvent<ErrorEvent>().Publish(message);

        public static void NewUpdate(this IEventAggregator events, NotificationModel update)
            => events.GetEvent<NewUpdateEvent>().Publish(update);
    }
}