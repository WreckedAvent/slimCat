namespace Slimcat.Models
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    ///     For everything that needs to interact with the user's account
    /// </summary>
    public interface IAccount
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the account name.
        /// </summary>
        string AccountName { get; set; }

        /// <summary>
        ///     Gets the all friends.
        /// </summary>
        IDictionary<string, IList<string>> AllFriends { get; }

        /// <summary>
        ///     Gets the character's bookmarks.
        /// </summary>
        IList<string> Bookmarks { get; }

        /// <summary>
        ///     Gets the characters associated with this account.
        /// </summary>
        ObservableCollection<string> Characters { get; }

        /// <summary>
        ///     Gets or sets the error.
        /// </summary>
        string Error { get; set; }

        /// <summary>
        ///     Gets or sets the password.
        /// </summary>
        string Password { get; set; }

        /// <summary>
        ///     Gets or sets the ticket.
        /// </summary>
        string Ticket { get; set; }

        #endregion
    }
}