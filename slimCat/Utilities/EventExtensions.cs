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
        ///     Sends the command as the current user.
        /// </summary>
        public static void SendUserCommand(this IEventAggregator events, string commandName,
            IList<string> arguments = null, string channel = null)
        {
            events.GetEvent<UserCommandEvent>()
                .Publish(CommandDefinitions.CreateCommand(commandName, arguments, channel).ToDictionary());
        }

        public static void NewCharacterUpdate(this IEventAggregator events, ICharacter character,
            CharacterUpdateEventArgs e)
        {
            events.GetEvent<NewUpdateEvent>().Publish(new CharacterUpdateModel(character, e));
        }

        public static void NewChannelUpdate(this IEventAggregator events, ChannelModel channel, ChannelUpdateEventArgs e)
        {
            events.GetEvent<NewUpdateEvent>().Publish(new ChannelUpdateModel(channel, e));
        }
    }
}