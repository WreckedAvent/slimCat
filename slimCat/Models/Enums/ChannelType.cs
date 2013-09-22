namespace Slimcat.Models
{
    /// <summary>
    /// Represents the possible channel types
    /// </summary>
    public enum ChannelType
    {
        /// <summary>
        /// Public channels are official channels which are open to the public and abide by F-list's rules and moderation.
        /// </summary>
        Public, 

        /// <summary>
        /// Privately-owned channels which are open to the public but abide by their own moderation.
        /// </summary>
        Private, 

        /// <summary>
        /// Private Message channels are personal between two users.
        /// </summary>
        PrivateMessage, 

        /// <summary>
        /// InviteOnly channels are private channels which can only be joined with an outstanding invite.
        /// </summary>
        InviteOnly, 

        /// <summary>
        /// Utility channels are special channels which do not send or recieve messages.
        /// </summary>
        Utility, 
    }
}