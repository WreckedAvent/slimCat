#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IAccount.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//  
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Models
{
    #region Usings

    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    #endregion

    /// <summary>
    ///     Represents data about an F-list account.
    /// </summary>
    public interface IAccount
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the account name.
        /// </summary>
        string AccountName { get; set; }

        /// <summary>
        ///     Gets all of the account's friends.
        /// </summary>
        IDictionary<string, IList<string>> AllFriends { get; }

        /// <summary>
        ///     Gets the account's bookmarks.
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