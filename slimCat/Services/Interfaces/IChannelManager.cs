#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IChannelManager.cs">
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

    using Models;

    #endregion

    /// <summary>
    ///     Represents several operations to manage channels.
    /// </summary>
    public interface IChannelManager
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Adds the channel to the chat model.
        /// </summary>
        /// <param name="type">The channel's type.</param>
        /// <param name="id">The channel's identifier.</param>
        /// <param name="name">The channel's name.</param>
        void AddChannel(ChannelType type, string id, string name = "");


        /// <summary>
        ///     Adds the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="channelName">Name of the channel.</param>
        /// <param name="poster">The poster of the message.</param>
        /// <param name="messageType">Type of the message.</param>
        void AddMessage(string message, string channelName, string poster, MessageType messageType = MessageType.Normal);

        /// <summary>
        ///     Joins the channel, and then switches the tab to it.
        /// </summary>
        /// <remarks>The channel is created if it does not exist.</remarks>
        /// <param name="type">The channel's type.</param>
        /// <param name="id">The channel's identifier.</param>
        /// <param name="name">The channel's name.</param>
        void JoinChannel(ChannelType type, string id, string name = "");

        /// <summary>
        ///     Used to leave a channel
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        void RemoveChannel(string name);

        #endregion
    }
}