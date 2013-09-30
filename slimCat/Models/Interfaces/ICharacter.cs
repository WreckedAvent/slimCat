namespace Slimcat.Models
{
    /// <summary>
    ///     For everything that interacts directly with character data
    /// </summary>
    public interface ICharacter
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the gender.
        /// </summary>
        Gender Gender { get; set; }

        /// <summary>
        ///     Gets a value indicating whether the character has an open report.
        /// </summary>
        bool HasReport { get; }

        /// <summary>
        ///     Gets or sets the last report.
        /// </summary>
        ReportModel LastReport { get; set; }

        /// <summary>
        ///     Gets or sets the character's name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///     Gets or sets the status.
        /// </summary>
        StatusType Status { get; set; }

        /// <summary>
        ///     Gets or sets the status message.
        /// </summary>
        string StatusMessage { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the user is interesting to our user.
        /// </summary>
        bool IsInteresting { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The get avatar.
        /// </summary>
        void GetAvatar();

        #endregion
    }
}