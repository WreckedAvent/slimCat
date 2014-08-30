#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TicketProvider.cs">
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

namespace slimCat.Services
{
    #region Usings
    using Microsoft.Practices.Prism.Events;
    using System;
    using System.Timers;
    using Models;
    using Utilities;

    #endregion

    public class ChannelListUpdaterService : IChannelListUpdater
    {
        private readonly IChatConnection connection;

        private readonly IChatModel chatModel;

        private DateTime lastUpdate;

        public ChannelListUpdaterService(IChatConnection connection, IEventAggregator eventAggregator, IChatModel chatModel)
        {
            this.connection = connection;
            this.chatModel = chatModel;

            eventAggregator.GetEvent<ConnectionClosedEvent>().Subscribe(OnWipeState);

            var timer = new Timer(60*1000*1);
            timer.Elapsed += (s, e) =>
            {
                UpdateChannels();
                timer.Dispose();
            };
            timer.Start();
        }

        private void OnWipeState(string o)
        {
            lastUpdate = new DateTime();
        }

        public void UpdateChannels()
        {
            if (!ShouldUpdate()) return;

            connection.SendMessage(Constants.ClientCommands.PublicChannelList);
            connection.SendMessage(Constants.ClientCommands.PrivateChannelList);
            lastUpdate = DateTime.Now;
        }

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

    public interface IChannelListUpdater
    {
        void UpdateChannels();
    }
}
