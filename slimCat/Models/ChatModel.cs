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

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
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

        // things that we should keep a track of, yet not needed frequently
        private IList<string> _globalMods = new List<string>();

        // caches for speed improvements in filtering
        private IList<ICharacter> _onlineBookmarkCache = null;
        private IList<ICharacter> _onlineFriendCache = null;
        private IList<ICharacter> _onlineModsCache = null;
        private IEnumerable<ICharacter> _onlineCharactersCache = null;
        private DateTime _lastCharacterListCache = new DateTime();
        private IList<string> _ignored = new List<string>();
        #endregion

        #region Properties
        public IAccount OurAccount
        {
            get { return _account; }
            set { _account = value; OnPropertyChanged("OurAccount"); }
        }

        public IDictionary<string, ICharacter> OnlineCharactersDictionary { get { return _onlineCharacters; } }

        public IEnumerable<ICharacter> OnlineCharacters
        {
            get
            {
                if (_onlineCharactersCache == null)
                    _onlineCharactersCache = OnlineCharactersDictionary.Values.ToList(); // force pulling a non-changing cache

                return _onlineCharactersCache;
            }
        }

        public IEnumerable<ICharacter> OnlineFriends
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

        public IEnumerable<ICharacter> OnlineBookmarks
        {
            get
            {
                try
                {
                    if (_onlineBookmarkCache == null)
                        _onlineBookmarkCache = OnlineCharacters
                            .Where(character => Bookmarks.Any(bookmark => (character != null ? character.Name.Equals(bookmark, StringComparison.OrdinalIgnoreCase) : false)))
                            .ToList();
                }
                catch { } // sometimes this will result with threading issues, simply wait until next time it's called

                return _onlineBookmarkCache;
            }
        }

        public IEnumerable<ICharacter> OnlineGlobalMods
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

        public IList<string> Bookmarks { get { return OurAccount.Bookmarks; } }

        public IList<string> Mods { get { return _globalMods; } }
        public IList<string> Ignored { get { return _ignored; } }
        public IList<string> Interested { get { return ApplicationSettings.Interested; } }
        public IList<string> NotInterested { get { return ApplicationSettings.NotInterested; } }

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
        public DateTimeOffset LastMessageReceived { get; set; }

        public bool IsAuthenticated
        {
            get { return _isAuth; }
            set { _isAuth = value; OnPropertyChanged("IsAuthenticated"); }
        }

        public bool IsGlobalModerator { get; set; }
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
                UpdateCharacterList(IsOfInterest(character.Name));
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
                || Interested.Any(interest => interest.Equals(character, StringComparison.OrdinalIgnoreCase)))
                || CurrentPMs.Any(pm => pm.ID.Equals(character, StringComparison.OrdinalIgnoreCase));
        }

        public void RemoveCharacter(string character)
        {
            try
            {
                OnlineCharactersDictionary.Remove(character);
                UpdateCharacterList(IsOfInterest(character));
                UpdateBindings(character);
            }

            catch
            {
                Console.WriteLine("Error: Unable to remove character: " + character + " ( is he/she online? )");
            }
        }

        private void UpdateCharacterList(bool force)
        {
            if (force || _lastCharacterListCache.AddSeconds(15) < DateTime.Now)
            {
                _onlineCharactersCache = _onlineCharacters.Values.ToList();
                _lastCharacterListCache = DateTime.Now;
                OnPropertyChanged("OnlineCharacters");
            }
        }

        public ICharacter FindCharacter(string name)
        {
            if (IsOnline(name))
                return OnlineCharactersDictionary[name];

            else
            {
                Console.WriteLine("Unknown character: " + name);
                return new CharacterModel() { Name = name, Status = StatusType.offline };
            }
        }

        public bool IsOnline(string name)
        {
            if (name == null)
                return false;

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

        public void ToggleInterestedMark(string character)
        {
            if (!Interested.Contains(character))
            {
                Interested.Add(character);
                if (NotInterested.Contains(character))
                    NotInterested.Remove(character);
            }
            else
                Interested.Remove(character);

            Services.SettingsDaemon.SaveApplicationSettingsToXML(SelectedCharacter.Name);
        }

        public void ToggleNotInterestedMark(string character)
        {
            if (!NotInterested.Contains(character))
            {
                NotInterested.Add(character);
                if (Interested.Contains(character))
                    Interested.Remove(character);
            }
            else
                NotInterested.Remove(character);

            Services.SettingsDaemon.SaveApplicationSettingsToXML(SelectedCharacter.Name);
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
        IEnumerable<ICharacter> OnlineCharacters { get; }

        /// <summary>
        /// A list of all online characters who are friends
        /// </summary>
        IEnumerable<ICharacter> OnlineFriends { get; }

        /// <summary>
        /// A list of all online characters who are bookmarked
        /// </summary>
        IEnumerable<ICharacter> OnlineBookmarks { get; }

        /// <summary>
        /// A list of all online global moderators
        /// </summary>
        IEnumerable<ICharacter> OnlineGlobalMods { get; }

        IList<string> Interested { get; }

        IList<string> NotInterested { get; }

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
        DateTimeOffset LastMessageReceived { get; set; }

        /// <summary>
        /// Whether or not the current user has permissions to act like a moderator
        /// </summary>
        bool IsGlobalModerator { get; set; }

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

        /// <summary>
        /// Toggle our interest in a character
        /// </summary>
        void ToggleInterestedMark(string name);

        /// <summary>
        /// Toggle our disinterested in a character
        /// </summary>
        void ToggleNotInterestedMark(string name);

        event EventHandler SelectedChannelChanged;
    }
}
