namespace Slimcat.Models
{
    using System.Windows.Media.Imaging;

    /// <summary>
    ///     For everything that interacts directly with character data
    /// </summary>
    public interface ICharacter
    {
        #region Public Properties

        /// <summary>
        ///     Call GetAvatar before this is used
        /// </summary>
        BitmapImage Avatar { get; set; }

        /// <summary>
        ///     Gets or sets the gender.
        /// </summary>
        Gender Gender { get; set; }

        /// <summary>
        ///     Gets a value indicating whether has report.
        /// </summary>
        bool HasReport { get; }

        /// <summary>
        ///     Gets or sets the last report.
        /// </summary>
        ReportModel LastReport { get; set; }

        /// <summary>
        ///     The full name is the character's gender, op status, and name in one line
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

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The get avatar.
        /// </summary>
        void GetAvatar();

        #endregion
    }
}