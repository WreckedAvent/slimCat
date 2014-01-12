namespace Slimcat.Services
{
    using Models;

    /// <summary>
    ///     The ChannelManager interface.
    /// </summary>
    public interface IChannelManager
    {
        #region Public Methods and Operators

        /// <summary>
        /// Used to join a channel but not switch to it automatically
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="id">
        /// The ID.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        void AddChannel(ChannelType type, string id, string name = "");

        /// <summary>
        /// Used to add a message to a given channel
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="channelName">
        /// The channel Name.
        /// </param>
        /// <param name="poster">
        /// The poster.
        /// </param>
        /// <param name="messageType">
        /// The message Type.
        /// </param>
        void AddMessage(string message, string channelName, string poster, MessageType messageType = MessageType.Normal);

        /// <summary>
        /// Used to join or switch to a channel
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="id">
        /// The ID.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        void JoinChannel(ChannelType type, string id, string name = "");

        /// <summary>
        /// Used to leave a channel
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        void RemoveChannel(string name);

        #endregion
    }
}