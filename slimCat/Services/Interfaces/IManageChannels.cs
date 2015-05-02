#region Copyright

// <copyright file="IManageChannels.cs">
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

    using Models;

    #endregion

    /// <summary>
    ///     The channel service is responsible for adding/removing channels, switching to them, as well as adding new messages
    ///     to them.
    /// </summary>
    public interface IManageChannels
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Adds the channel to the chat model.
        /// </summary>
        void AddChannel(ChannelType type, string id, string name = "");

        /// <summary>
        ///     Adds the message to a channel.
        /// </summary>
        void AddMessage(string message, string channelName, string poster, MessageType messageType = MessageType.Normal);

        /// <summary>
        ///     Joins the channel, and then switches the tab to it.
        /// </summary>
        void JoinChannel(ChannelType type, string id, string name = "");

        /// <summary>
        ///     Leaves a channel.
        /// </summary>
        void RemoveChannel(string name, bool force = false, bool fromServer = false);

        /// <summary>
        ///     Adds the channel model if it doesn't exist, but doesn't select it or pull down history/settings.
        /// </summary>
        void QuickJoinChannel(string id, string name);

        #endregion
    }
}