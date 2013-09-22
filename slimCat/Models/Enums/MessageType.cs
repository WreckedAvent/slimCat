namespace Slimcat.Models
{
    /// <summary>
    ///     Used to represent possible types of message sent to the client
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Represents an ad
        /// </summary>
        Ad, 

        /// <summary>
        /// Represents a normal message
        /// </summary>
        Normal, 

        /// <summary>
        /// Represents a dice roll
        /// </summary>
        Roll, 
    }
}