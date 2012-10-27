using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace Models
{
    /// <summary>
    /// Contains most chat data which spans channels. Channel-wide UI binds to this.
    /// </summary>
    public class ChatModel : SysProp, IChatModel
    {
        #region Fields
        private IAccount _account;
        private ICharacter _thisCharacter; 
        private ChannelModel _thisChannel;
        public event EventHandler SelectedChannelChanged;

        // things that the UI binds to
        private ObservableCollection<PMChannelModel> _pms = new ObservableCollection<PMChannelModel>();
        private ObservableCollection<GeneralChannelModel> _channels = new ObservableCollection<GeneralChannelModel>();
        private ObservableCollection<GeneralChannelModel> _ourChannels = new ObservableCollection<GeneralChannelModel>();
        private ObservableCollection<NotificationModel> _notifications = new ObservableCollection<NotificationModel>();
        private bool _isAuth = false;

        private IDictionary<string, ICharacter> _onlineCharacters = new Dictionary<string, ICharacter>();
        private IList<string> _removeTemp = new List<string>();

        // things that we should keep a track of, yet not needed frequently
        private IList<string> _globalMods = new List<string>();
        private IList<string> _bookmarks = new List<string>();

        // caches for speed improvements in filtering
        private IList<ICharacter> _onlineBookmarkCache = null;
        private IList<ICharacter> _onlineFriendCache = null;
        private IList<ICharacter> _onlineModsCache = null;

        private IList<string> _notInterested = new List<string>();
        private IList<string> _interestedIn = new List<string>();
        private IList<string> _ignored = new List<string>();
        #endregion

        #region Properties
        public IAccount OurAccount
        {
            get { return _account; }
            set { _account = value; OnPropertyChanged("OurAccount"); }
        }

        public IDictionary<string, ICharacter> OnlineCharactersDictionary { get { return _onlineCharacters; } }

        public ICollection<ICharacter> OnlineCharacters
        {
            get { return OnlineCharactersDictionary.Values; }
        }

        public IList<ICharacter> OnlineFriends
        {
            get
            {
                try
                {
                    if (_onlineFriendCache == null && Friends != null)
                        _onlineFriendCache = OnlineCharacters
                            .Where(character => Friends.Any(friend => character.Name.Equals(friend, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                }
                catch { } // if we run into an issue calling this, simply wait until next update

                return _onlineFriendCache;
            }
        }

        public IList<ICharacter> OnlineBookmarks
        {
            get
            {
                try
                {
                    if (_onlineBookmarkCache == null)
                        _onlineBookmarkCache = OnlineCharacters
                            .Where(character => _bookmarks.Any(bookmark => (character != null ? character.Name.Equals(bookmark, StringComparison.OrdinalIgnoreCase) : false)))
                            .ToList();
                }
                catch { } // sometimes this will result with threading issues, simply wait until next time it's called

                return _onlineBookmarkCache;
            }
        }

        public IList<ICharacter> OnlineGlobalMods
        {
            get
            {
                if (_onlineModsCache == null)
                        _onlineModsCache = OnlineCharacters
                            .Where(character => (character != null ? (_globalMods.Any(mod => mod.Equals(character.Name, StringComparison.OrdinalIgnoreCase))) : false))
                            .ToList();
                return _onlineModsCache;
            }
        }

        public ICharacter SelectedCharacter
        {
            get { return _thisCharacter; }
            set { _thisCharacter = value; OnPropertyChanged("SelectedCharacter"); }
        }

        // channels is all of the channels,
        public ObservableCollection<GeneralChannelModel> AllChannels { get { return _channels; } }

        // our channels is the ones the user has opened
        public ObservableCollection<GeneralChannelModel> CurrentChannels { get { return _ourChannels; } }

        // and PMs
        public ObservableCollection<PMChannelModel> CurrentPMs { get { return _pms; } }

        // and finally our notifications
        public ObservableCollection<NotificationModel> Notifications { get { return _notifications; } }

        public IList<string> Friends
        {
            get
            {
                return
                    OurAccount.AllFriends
                    .Where(pair => pair.Value.Contains(SelectedCharacter.Name))
                    .Select(pair => pair.Key).ToList();
            }
        }

        public IList<string> Bookmarks { get { return _bookmarks; } }
        public IList<string> Mods { get { return _globalMods; } }
        public IList<string> Ignored { get { return _ignored; } }

        public ChannelModel SelectedChannel
        {
            get { return _thisChannel; }
            set 
            {
                if (_thisChannel != value && value != null)
                {
                    _thisChannel = value;

                    if (SelectedChannelChanged != null)
                        SelectedChannelChanged(this, new EventArgs());

                    OnPropertyChanged("SelectedChannel");
                }
            }
        }

        public DateTimeOffset ClientUptime { get; set; }
        public DateTimeOffset ServerUpTime { get; set; }

        public bool IsAuthenticated
        {
            get { return _isAuth; }
            set { _isAuth = value; OnPropertyChanged("IsAuthenticated"); }
        }
        #endregion

        #region Constructors
        public ChatModel() { }
        #endregion

        #region Methods
        public void AddCharacter(ICharacter character)
        {
            try
            {
                OnlineCharactersDictionary.Add(character.Name, character);
                OnPropertyChanged("OnlineCharacters");
                UpdateBindings(character.Name);
            }

            catch
            {
                Console.WriteLine("Error: Unable to add character: " + character.Name);
            }
        }

        public bool IsOfInterest(string character)
        {
            return (Bookmarks.Any(bookmark => bookmark.Equals(character, StringComparison.OrdinalIgnoreCase))
                || Friends.Any(friend => friend.Equals(character, StringComparison.OrdinalIgnoreCase))
                || _interestedIn.Any(interest => interest.Equals(character, StringComparison.OrdinalIgnoreCase)))
                || CurrentPMs.Any(pm => pm.ID.Equals(character, StringComparison.OrdinalIgnoreCase));
        }

        public void RemoveCharacter(string character)
        {
            try
            {
                OnlineCharactersDictionary.Remove(character);
                OnPropertyChanged("OnlineCharacters");
                UpdateBindings(character);
            }

            catch
            {
                Console.WriteLine("Error: Unable to remove character: " + character + " ( is he/she online? )");
            }
        }

        public ICharacter FindCharacter(string name)
        {
            if (IsOnline(name))
                return OnlineCharactersDictionary[name];

            else
            {
                Console.WriteLine("Unknown character: " + name);
                return new CharacterModel() { Name = name };
            }
        }

        public bool IsOnline(string name)
        {
            return OnlineCharactersDictionary.ContainsKey(name);
        }

        private void UpdateBindings(string name)
        {
            if (Bookmarks.Contains(name))
            {
                _onlineBookmarkCache = null;
                OnPropertyChanged("OnlineBookmarks");
            }

            if (Friends.Contains(name))
            {
                _onlineFriendCache = null;
                OnPropertyChanged("OnlineFriends");
            }

            if (Mods.Contains(name))
            {
                _onlineModsCache = null;
                OnPropertyChanged("OnlineGlobalMods");
            }
        }
        #endregion
    }

    public interface IChatModel
    {
        /// <summary>
        /// Information relating to the currently selected account
        /// </summary>
        IAccount OurAccount { get; set;}

        /// <summary>
        /// A list of all online characters
        /// </summary>
        ICollection<ICharacter> OnlineCharacters { get; }

        /// <summary>
        /// A list of all online characters who are friends
        /// </summary>
        IList<ICharacter> OnlineFriends { get; }

        /// <summary>
        /// A list of all online characters who are bookmarked
        /// </summary>
        IList<ICharacter> OnlineBookmarks { get; }

        /// <summary>
        /// A list of all online global moderators
        /// </summary>
        IList<ICharacter> OnlineGlobalMods { get; }

        IList<string> Friends { get; }

        IList<string> Ignored { get; }

        /// <summary>
        /// A list of all bookmarked characters
        /// </summary>
        IList<string> Bookmarks { get; }

        /// <summary>
        /// A list of all global moderators
        /// </summary>
        IList<string> Mods { get; }

        /// <summary>
        /// If we're actively connected and authenticated through F-Chat
        /// </summary>
        bool IsAuthenticated { get; set; }

        /// <summary>
        /// The Channel we have selected as the 'active' one
        /// </summary>
        ChannelModel SelectedChannel { get; set; }

        /// <summary>
        /// The Character we've chosen to enter chat with
        /// </summary>
        ICharacter SelectedCharacter { get; set; }

        /// <summary>
        /// A collection of all opened PMs
        /// </summary>
        ObservableCollection<PMChannelModel> CurrentPMs { get; }

        /// <summary>
        /// A colleciton of all opened channels
        /// </summary>
        ObservableCollection<GeneralChannelModel> CurrentChannels { get; }

        /// <summary>
        /// A collection of ALL channels, public or private
        /// </summary>
        ObservableCollection<GeneralChannelModel> AllChannels { get; }

        /// <summary>
        /// A collection of all of our notifications
        /// </summary>
        ObservableCollection<NotificationModel> Notifications { get; }

        DateTimeOffset ClientUptime { get; set; }
        DateTimeOffset ServerUpTime { get; set; }

        void AddCharacter(ICharacter character);
        void RemoveCharacter(string name);
        bool IsOfInterest(string name);

        /// <summary>
        /// Checks if a given user is online
        /// </summary>
        bool IsOnline(string name);

        /// <summary>
        /// Returns the ICharacter value of a given string, if online
        /// </summary>
        ICharacter FindCharacter(string name);

        event EventHandler SelectedChannelChanged;
    }
}
