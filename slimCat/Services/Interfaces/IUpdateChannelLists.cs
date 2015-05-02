#region Copyright

// <copyright file="IChannelListUpdater.cs">
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
    /// <summary>
    ///     The channel list updater service is used to get new channel lists on the channel list tab in the right-expander.
    /// </summary>
    public interface IUpdateChannelLists
    {
        /// <summary>
        ///     Updates channel list.
        /// </summary>
        void UpdateChannels();
    }
}