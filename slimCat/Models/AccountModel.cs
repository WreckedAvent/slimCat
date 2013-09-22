// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AccountModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   A model which stores information relevant to accessing an F-list account,
//   as well as needed results from the ticket request.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Models
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Slimcat.Properties;

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
            {
                this.Password = Settings.Default.Password;
            }

            if (!string.IsNullOrWhiteSpace(Settings.Default.UserName))
            {
                this.AccountName = Settings.Default.UserName;
            }
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
            get
            {
                return this.friends;
            }
        }

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IList<string> Bookmarks
        {
            get
            {
                return this.bookmarks;
            }
        }

        /// <summary>
        ///     Gets the characters.
        /// </summary>
        public ObservableCollection<string> Characters
        {
            get
            {
                return this.characters;
            }
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