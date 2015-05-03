#region Copyright

// <copyright file="ChannelListUpdaterService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Timers;
    using Models;
    using Utilities;

    #endregion

    public class ChannelListUpdaterService : IUpdateChannelLists
    {
        private readonly IChatModel chatModel;
        private readonly IHandleChatConnection connection;
        private DateTime lastUpdate;

        public ChannelListUpdaterService(IChatState chatState)
        {
            connection = chatState.Connection;
            chatModel = chatState.ChatModel;

            chatState.EventAggregator.GetEvent<ConnectionClosedEvent>().Subscribe(OnWipeState);

            var timer = new Timer(60*1000*1);
            timer.Elapsed += (s, e) =>
            {
                UpdateChannels();
                timer.Dispose();
            };
            timer.Start();
        }

        public void UpdateChannels()
        {
            if (!ShouldUpdate()) return;

            connection.SendMessage(Constants.ClientCommands.PublicChannelList);
            connection.SendMessage(Constants.ClientCommands.PrivateChannelList);
            lastUpdate = DateTime.Now;
        }

        private void OnWipeState(bool o) => lastUpdate = new DateTime();

        private bool ShouldUpdate()
        {
            // if we didn't get our channel list by a minute in, try updating again
            if (lastUpdate.AddMinutes(1) < DateTime.Now)
            {
                if (chatModel.AllChannels.Count.Equals(ApplicationSettings.SavedChannels.Count))
                    return true;
            }

            // update every 2 hours
            return lastUpdate.AddHours(2) <= DateTime.Now;
        }
    }
}