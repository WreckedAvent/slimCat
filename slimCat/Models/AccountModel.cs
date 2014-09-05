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
    using Properties;

    #endregion

    /// <summary>
    ///     A model which stores information relevant to accessing an F-list account,
    ///     as well as needed results from the ticket request.
    /// </summary>
    public class AccountModel : IAccount
    {
        #region Fields

        private readonly IList<string> bookmarks = new List<string>();

        private readonly ObservableCollection<string> characters = new ObservableCollection<string>();

        private readonly IDictionary<string, IList<string>> friends = new Dictionary<string, IList<string>>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountModel" /> class.
        /// </summary>
        public AccountModel()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.Password))
                Password = Settings.Default.Password;

            if (!string.IsNullOrWhiteSpace(Settings.Default.UserName))
                AccountName = Settings.Default.UserName;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the account name.
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        ///     Gets the all friends.
        /// </summary>
        public IDictionary<string, IList<string>> AllFriends
        {
            get { return friends; }
        }

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IList<string> Bookmarks
        {
            get { return bookmarks; }
        }

        /// <summary>
        ///     Gets the characters.
        /// </summary>
        public ObservableCollection<string> Characters
        {
            get { return characters; }
        }

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