/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using slimCat.Properties;

namespace Models
{
    /// <summary>
    /// A model which stores information relevant to accessing an F-list account, 
    /// as well as needed results from the ticket request.
    /// </summary>
    public class AccountModel : IAccount
    {
        #region Fields
        private string _accountName;
        private string _password;
        private string _error;

        private string _ticket;
        private ObservableCollection<string> _characters = new ObservableCollection<string>();
        private IList<String> _bookmarks = new List<string>();
        private IDictionary<string, IList<string>> _friends = new Dictionary<string, IList<string>>();
        #endregion

        #region Properties
        public string AccountName
        {
            get { return _accountName; }
            set { _accountName = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public string Error
        {
            get { return _error; }
            set { _error = value; }
        }

        public string Ticket
        {
            get { return _ticket; }
            set { _ticket = value; }
        }

        public ObservableCollection<string> Characters { get { return _characters; } }

        public IDictionary<string, IList<string>> AllFriends { get { return _friends; } }

        public IList<string> Bookmarks { get { return _bookmarks; } }
        #endregion

        public AccountModel()
        {
            if (!String.IsNullOrWhiteSpace(Settings.Default.Password))
                _password = Settings.Default.Password;
            if (!string.IsNullOrWhiteSpace(Settings.Default.UserName))
                _accountName = Settings.Default.UserName;
        }
    }

    /// <summary>
    /// For everything that needs to interact with the user's account
    /// </summary>
    public interface IAccount
    {
        string AccountName { get; set; }
        string Password { get; set; }
        string Error { get; set; }

        string Ticket { get; set; }
        ObservableCollection<string> Characters { get; }
        IDictionary<string, IList<string>> AllFriends { get; }
        IList<string> Bookmarks { get; }
    }
}
