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
    ///     The ChannelManager interface.
    /// </summary>
    public interface IChannelManager
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Used to join a channel but not switch to it automatically
        /// </summary>
        /// <param name="type">
        ///     The type.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        void AddChannel(ChannelType type, string id, string name = "");

        /// <summary>
        ///     Used to add a message to a given channel
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="channelName">
        ///     The channel Name.
        /// </param>
        /// <param name="poster">
        ///     The poster.
        /// </param>
        /// <param name="messageType">
        ///     The message Type.
        /// </param>
        void AddMessage(string message, string channelName, string poster, MessageType messageType = MessageType.Normal);

        /// <summary>
        ///     Used to join or switch to a channel
        /// </summary>
        /// <param name="type">
        ///     The type.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
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