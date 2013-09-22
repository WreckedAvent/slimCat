namespace Slimcat.Models
{
    /// <summary>
    ///     The possible states of typing
    /// </summary>
    public enum TypingStatus
    {
        /// <summary>
        /// User is not typing and does not have anything typed.
        /// </summary>
        Clear, 

        /// <summary>
        /// User is not typing but has something typed out already.
        /// </summary>
        Paused, 

        /// <summary>
        /// User is typing actively.
        /// </summary>
        Typing
    }
}