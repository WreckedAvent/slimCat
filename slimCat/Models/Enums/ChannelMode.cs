namespace Slimcat.Models
{
    /// <summary>
    /// Represents possible channel modes
    /// </summary>
    public enum ChannelMode
    {
        /// <summary>
        /// Channels that only allow ads.
        /// </summary>
        Ads, 

        /// <summary>
        /// Channels that only allow chatting.
        /// </summary>
        Chat, 

        /// <summary>
        /// Channels that allow ads and chatting.
        /// </summary>
        Both, 
    }
}