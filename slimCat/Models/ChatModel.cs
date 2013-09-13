// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatModel.cs" company="Justin Kadrovach">
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
//   Contains most chat data which spans channels. Channel-wide UI binds to this.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Services;

    /// <summary>
    ///     Contains most chat data which spans channels. Channel-wide UI binds to this.
    /// </summary>
    public class ChatModel : SysProp, IChatModel
    {
        #region Fields

        private readonly ObservableCollection<GeneralChannelModel> _channels =
            new ObservableCollection<GeneralChannelModel>();

        private readonly IList<string> _globalMods = new List<string>();

        private readonly IList<string> _ignored = new List<string>();

        private readonly ObservableCollection<NotificationModel> _notifications =
            new ObservableCollection<NotificationModel>();

        private readonly IDictionary<string, ICharacter> _onlineCharacters = new Dictionary<string, ICharacter>(StringComparer.OrdinalIgnoreCase);

        private readonly ObservableCollection<GeneralChannelModel> _ourChannels =
            new ObservableCollection<GeneralChannelModel>();

        private readonly ObservableCollection<PMChannelModel> _pms = new ObservableCollection<PMChannelModel>();

        private IAccount _account;

        private bool _isAuth;

        private DateTime _lastCharacterListCache;

        // things that we should keep a track of, yet not needed frequently

        // caches for speed improvements in filtering
        private IList<ICharacter> _onlineBookmarkCache;

        private IEnumerable<ICharacter> _onlineCharactersCache;

        private IList<ICharacter> _onlineFriendCache;

        private IList<ICharacter> _onlineModsCache;

        private ChannelModel _thisChannel;

        private ICharacter _thisCharacter;

        #endregion

        #region Public Events

        /// <summary>
        ///     The selected channel changed.
        /// </summary>
        public event EventHandler SelectedChannelChanged;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the all channels.
        /// </summary>
        public ObservableCollection<GeneralChannelModel> AllChannels
        {
            get
            {
                return this._channels;
            }
        }

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IList<string> Bookmarks
        {
            get
            {
                return this.OurAccount.Bookmarks;
            }
        }

        /// <summary>
        ///     Gets or sets the client uptime.
        /// </summary>
        public DateTimeOffset ClientUptime { get; set; }

        /// <summary>
        ///     Gets the current channels.
        /// </summary>
        public ObservableCollection<GeneralChannelModel> CurrentChannels
        {
            get
            {
                return this._ourChannels;
            }
        }

        /// <summary>
        ///     Gets the current private messages.
        /// </summary>
        public ObservableCollection<PMChannelModel> CurrentPMs
        {
            get
            {
                return this._pms;
            }
        }

        // and finally our notifications

        /// <summary>
        ///     Gets the friends.
        /// </summary>
        public IList<string> Friends
        {
            get
            {
                return
                    this.OurAccount.AllFriends.Where(pair => pair.Value.Contains(this.SelectedCharacter.Name))
                        .Select(pair => pair.Key)
                        .ToList();
            }
        }

        /// <summary>
        ///     Gets the ignored.
        /// </summary>
        public IList<string> Ignored
        {
            get
            {
                return this._ignored;
            }
        }

        /// <summary>
        ///     Gets the interested.
        /// </summary>
        public IList<string> Interested
        {
            get
            {
                return ApplicationSettings.Interested;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get
            {
                return this._isAuth;
            }

            set
            {
                this._isAuth = value;
                this.OnPropertyChanged("IsAuthenticated");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is global moderator.
        /// </summary>
        public bool IsGlobalModerator { get; set; }

        /// <summary>
        ///     Gets or sets the last message received.
        /// </summary>
        public DateTimeOffset LastMessageReceived { get; set; }

        /// <summary>
        ///     Gets the mods.
        /// </summary>
        public IList<string> Mods
        {
            get
            {
                return this._globalMods;
            }
        }

        /// <summary>
        ///     Gets the not interested.
        /// </summary>
        public IList<string> NotInterested
        {
            get
            {
                return ApplicationSettings.NotInterested;
            }
        }

        /// <summary>
        ///     Gets the notifications.
        /// </summary>
        public ObservableCollection<NotificationModel> Notifications
        {
            get
            {
                return this._notifications;
            }
        }

        /// <summary>
        ///     Gets the online bookmarks.
        /// </summary>
        public IEnumerable<ICharacter> OnlineBookmarks
        {
            get
            {
                return this._onlineBookmarkCache
                       ?? (this._onlineBookmarkCache =
                           this.OnlineCharacters.Where(
                               character =>
                               this.Bookmarks.Any(
                                   bookmark =>
                                   (character != null
                                    && character.Name.Equals(bookmark, StringComparison.OrdinalIgnoreCase)))).ToList());
            }
        }

        /// <summary>
        ///     Gets the online characters.
        /// </summary>
        public IEnumerable<ICharacter> OnlineCharacters
        {
            get
            {
                return this._onlineCharactersCache
                       ?? (this._onlineCharactersCache = this.OnlineCharactersDictionary.Values.ToList());
            }
        }

        /// <summary>
        ///     Gets the online characters dictionary.
        /// </summary>
        public IDictionary<string, ICharacter> OnlineCharactersDictionary
        {
            get
            {
                return this._onlineCharacters;
            }
        }

        /// <summary>
        ///     Gets the online friends.
        /// </summary>
        public IEnumerable<ICharacter> OnlineFriends
        {
            get
            {
                if (this._onlineFriendCache == null && this.Friends != null)
                {
                    this._onlineFriendCache =
                        this.OnlineCharacters.Where(
                            character =>
                            this.Friends.Any(
                                friend => character.Name.Equals(friend, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                }
                return this._onlineFriendCache;
            }
        }

        /// <summary>
        ///     Gets the online global mods.
        /// </summary>
        public IEnumerable<ICharacter> OnlineGlobalMods
        {
            get
            {
                return this._onlineModsCache
                       ?? (this._onlineModsCache =
                           this.OnlineCharacters.Where(
                               character =>
                               (character != null
                                && this._globalMods.Any(
                                    mod => mod.Equals(character.Name, StringComparison.OrdinalIgnoreCase)))).ToList());
            }
        }

        /// <summary>
        ///     Gets or sets the our account.
        /// </summary>
        public IAccount OurAccount
        {
            get
            {
                return this._account;
            }

            set
            {
                this._account = value;
                this.OnPropertyChanged("OurAccount");
            }
        }

        /// <summary>
        ///     Gets or sets the selected channel.
        /// </summary>
        public ChannelModel SelectedChannel
        {
            get
            {
                return this._thisChannel;
            }

            set
            {
                if (this._thisChannel != value && value != null)
                {
                    this._thisChannel = value;

                    if (this.SelectedChannelChanged != null)
                    {
                        this.SelectedChannelChanged(this, new EventArgs());
                    }

                    this.OnPropertyChanged("SelectedChannel");
                }
            }
        }

        /// <summary>
        ///     Gets or sets the selected character.
        /// </summary>
        public ICharacter SelectedCharacter
        {
            get
            {
                return this._thisCharacter;
            }

            set
            {
                this._thisCharacter = value;
                this.OnPropertyChanged("SelectedCharacter");
            }
        }

        /// <summary>
        ///     Gets or sets the server up time.
        /// </summary>
        public DateTimeOffset ServerUpTime { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The add character.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        public void AddCharacter(ICharacter character)
        {
            try
            {
                this.OnlineCharactersDictionary.Add(character.Name, character);
                this.UpdateCharacterList(this.IsOfInterest(character.Name));
                this.UpdateBindings(character.Name);
            }
            catch
            {
                Console.WriteLine("Error: Unable to add character: " + character.Name);
            }
        }

        /// <summary>
        /// The find character.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="ICharacter"/>.
        /// </returns>
        public ICharacter FindCharacter(string name)
        {
            if (this.IsOnline(name))
            {
                return this.OnlineCharactersDictionary[name];
            }
            else
            {
                Console.WriteLine("Unknown character: " + name);
                return new CharacterModel { Name = name, Status = StatusType.offline };
            }
        }

        /// <summary>
        /// The is of interest.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsOfInterest(string character)
        {
            return (this.Bookmarks.Any(bookmark => bookmark.Equals(character, StringComparison.OrdinalIgnoreCase))
                    || this.Friends.Any(friend => friend.Equals(character, StringComparison.OrdinalIgnoreCase))
                    || this.Interested.Any(interest => interest.Equals(character, StringComparison.OrdinalIgnoreCase)))
                   || this.CurrentPMs.Any(pm => pm.ID.Equals(character, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The is online.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsOnline(string name)
        {
            return name != null && this.OnlineCharactersDictionary.ContainsKey(name);
        }

        /// <summary>
        /// The remove character.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        public void RemoveCharacter(string character)
        {
            try
            {
                this.OnlineCharactersDictionary.Remove(character);
                this.UpdateCharacterList(this.IsOfInterest(character));
                this.UpdateBindings(character);
            }
            catch
            {
                Console.WriteLine("Error: Unable to remove character: " + character + " ( is he/she online? )");
            }
        }

        /// <summary>
        /// The toggle interested mark.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        public void ToggleInterestedMark(string character)
        {
            if (!this.Interested.Contains(character))
            {
                this.Interested.Add(character);
                if (this.NotInterested.Contains(character))
                {
                    this.NotInterested.Remove(character);
                }
            }
            else
            {
                this.Interested.Remove(character);
            }

            SettingsDaemon.SaveApplicationSettingsToXML(this.SelectedCharacter.Name);
        }

        /// <summary>
        /// The toggle not interested mark.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        public void ToggleNotInterestedMark(string character)
        {
            if (!this.NotInterested.Contains(character))
            {
                this.NotInterested.Add(character);
                if (this.Interested.Contains(character))
                {
                    this.Interested.Remove(character);
                }
            }
            else
            {
                this.NotInterested.Remove(character);
            }

            SettingsDaemon.SaveApplicationSettingsToXML(this.SelectedCharacter.Name);
        }

        #endregion

        #region Methods

        private void UpdateBindings(string name)
        {
            if (this.Bookmarks.Contains(name))
            {
                this._onlineBookmarkCache = null;
                this.OnPropertyChanged("OnlineBookmarks");
            }

            if (this.Friends.Contains(name))
            {
                this._onlineFriendCache = null;
                this.OnPropertyChanged("OnlineFriends");
            }

            if (!this.Mods.Contains(name))
            {
                return;
            }

            this._onlineModsCache = null;
            this.OnPropertyChanged("OnlineGlobalMods");
        }

        private void UpdateCharacterList(bool force)
        {
            if (!force && this._lastCharacterListCache.AddSeconds(15) >= DateTime.Now)
            {
                return;
            }

            this._onlineCharactersCache = this._onlineCharacters.Values.ToList();
            this._lastCharacterListCache = DateTime.Now;
            this.OnPropertyChanged("OnlineCharacters");
        }

        #endregion
    }

    /// <summary>
    ///     The ChatModel interface.
    /// </summary>
    public interface IChatModel
    {
        #region Public Events

        /// <summary>
        ///     The selected channel changed.
        /// </summary>
        event EventHandler SelectedChannelChanged;

        #endregion

        #region Public Properties

        /// <summary>
        ///     A collection of ALL channels, public or private
        /// </summary>
        ObservableCollection<GeneralChannelModel> AllChannels { get; }

        /// <summary>
        ///     A list of all bookmarked characters
        /// </summary>
        IList<string> Bookmarks { get; }

        /// <summary>
        ///     Gets or sets the client uptime.
        /// </summary>
        DateTimeOffset ClientUptime { get; set; }

        /// <summary>
        ///     A colleciton of all opened channels
        /// </summary>
        ObservableCollection<GeneralChannelModel> CurrentChannels { get; }

        /// <summary>
        ///     A collection of all opened PMs
        /// </summary>
        ObservableCollection<PMChannelModel> CurrentPMs { get; }

        /// <summary>
        ///     Gets the friends.
        /// </summary>
        IList<string> Friends { get; }

        /// <summary>
        ///     Gets the ignored.
        /// </summary>
        IList<string> Ignored { get; }

        /// <summary>
        ///     Gets the interested.
        /// </summary>
        IList<string> Interested { get; }

        /// <summary>
        ///     If we're actively connected and authenticated through F-Chat
        /// </summary>
        bool IsAuthenticated { get; set; }

        /// <summary>
        ///     Whether or not the current user has permissions to act like a moderator
        /// </summary>
        bool IsGlobalModerator { get; set; }

        /// <summary>
        ///     Gets or sets the last message received.
        /// </summary>
        DateTimeOffset LastMessageReceived { get; set; }

        /// <summary>
        ///     A list of all global moderators
        /// </summary>
        IList<string> Mods { get; }

        /// <summary>
        ///     Gets the not interested.
        /// </summary>
        IList<string> NotInterested { get; }

        /// <summary>
        ///     A collection of all of our notifications
        /// </summary>
        ObservableCollection<NotificationModel> Notifications { get; }

        /// <summary>
        ///     A list of all online characters who are bookmarked
        /// </summary>
        IEnumerable<ICharacter> OnlineBookmarks { get; }

        /// <summary>
        ///     A list of all online characters
        /// </summary>
        IEnumerable<ICharacter> OnlineCharacters { get; }

        /// <summary>
        ///     A list of all online characters who are friends
        /// </summary>
        IEnumerable<ICharacter> OnlineFriends { get; }

        /// <summary>
        ///     A list of all online global moderators
        /// </summary>
        IEnumerable<ICharacter> OnlineGlobalMods { get; }

        /// <summary>
        ///     Information relating to the currently selected account
        /// </summary>
        IAccount OurAccount { get; set; }

        /// <summary>
        ///     The Channel we have selected as the 'active' one
        /// </summary>
        ChannelModel SelectedChannel { get; set; }

        /// <summary>
        ///     The Character we've chosen to enter chat with
        /// </summary>
        ICharacter SelectedCharacter { get; set; }

        /// <summary>
        ///     Gets or sets the server up time.
        /// </summary>
        DateTimeOffset ServerUpTime { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The add character.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        void AddCharacter(ICharacter character);

        /// <summary>
        /// Returns the ICharacter value of a given string, if online
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="ICharacter"/>.
        /// </returns>
        ICharacter FindCharacter(string name);

        /// <summary>
        /// The is of interest.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        bool IsOfInterest(string name);

        /// <summary>
        /// Checks if a given user is online
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        bool IsOnline(string name);

        /// <summary>
        /// The remove character.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        void RemoveCharacter(string name);

        /// <summary>
        /// Toggle our interest in a character
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        void ToggleInterestedMark(string name);

        /// <summary>
        /// Toggle our disinterested in a character
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        void ToggleNotInterestedMark(string name);

        #endregion
    }
}