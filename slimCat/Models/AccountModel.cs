#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AccountModel.cs">
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
    using Services;
    using Utilities;

    #endregion

    /// <summary>
    ///     A model which stores information relevant to accessing an F-list account,
    ///     as well as needed results from the ticket request.
    /// </summary>
    public class AccountModel : IAccount
    {
        #region Fields

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountModel" /> class.
        /// </summary>
        public AccountModel()
        {
            var preferences = SettingsService.Preferences;
            if (!string.IsNullOrWhiteSpace(preferences.Password))
                Password = preferences.Password;

            if (!string.IsNullOrWhiteSpace(preferences.Username))
                AccountName = preferences.Username;

            ServerHost = string.IsNullOrWhiteSpace(preferences.Host) 
                ? Constants.ServerHost 
                : preferences.Host;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the account name.
        /// </summary>
        public string ServerHost { get; set; }

        /// <summary>
        ///     Gets or sets the account name.
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        ///     Gets the all friends.
        /// </summary>
        public IDictionary<string, IList<string>> AllFriends { get; } = new Dictionary<string, IList<string>>();

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IList<string> Bookmarks { get; } = new List<string>();

        /// <summary>
        ///     Gets the characters.
        /// </summary>
        public ObservableCollection<string> Characters { get; } = new ObservableCollection<string>();

        /// <summary>
        ///     Gets or sets the error.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        ///     Gets or sets the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     Gets or sets the ticket.
        /// </summary>
        public string Ticket { get; set; }

        #endregion
    }
}