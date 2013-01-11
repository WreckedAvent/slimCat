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
