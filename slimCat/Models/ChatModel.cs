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

namespace Slimcat.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Slimcat.Services;
    using Slimcat.Utilities;
    using Slimcat.ViewModels;

    /// <summary>
    ///     Contains most chat data which spans channels. Channel-wide UI binds to this.
    /// </summary>
    public class ChatModel : SysProp, IChatModel
    {
        #region Fields

        private readonly ObservableCollection<GeneralChannelModel> channels =
            new ObservableCollection<GeneralChannelModel>();

        private readonly IList<string> globalMods = new List<string>();

        private readonly IList<string> ignored = new List<string>();

        private readonly ObservableCollection<NotificationModel> notifications =
            new ObservableCollection<NotificationModel>();

        private readonly IDictionary<string, ICharacter> onlineCharacters = new ConcurrentDictionary<string, ICharacter>(StringComparer.OrdinalIgnoreCase);

        private readonly ObservableCollection<GeneralChannelModel> ourChannels =
            new ObservableCollection<GeneralChannelModel>();

        private readonly ObservableCollection<PMChannelModel> pms = new ObservableCollection<PMChannelModel>();

        private IAccount account;

        private bool isAuthenticated;

        private DateTime lastCharacterListCache;

        // caches for speed improvements in filtering
        private IList<ICharacter> onlineBookmarkCache;

        private IEnumerable<ICharacter> onlineCharactersCache;

        private IList<ICharacter> onlineFriendCache;

        private IList<ICharacter> onlineModsCache;

        private ChannelModel currentChannel;

        private ICharacter currentCharacter;

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
                return this.channels;
            }
        }

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IList<string> Bookmarks
        {
            get
            {
                return this.CurrentAccount.Bookmarks;
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
                return this.ourChannels;
            }
        }

        /// <summary>
        ///     Gets the current private messages.
        /// </summary>
        public ObservableCollection<PMChannelModel> CurrentPMs
        {
            get
            {
                return this.pms;
            }
        }

        /// <summary>
        ///     Gets the friends.
        /// </summary>
        public IList<string> Friends
        {
            get
            {
                if (ApplicationSettings.FriendsAreAccountWide)
                {
                    return this.CurrentAccount.AllFriends
                        .Select(pair => pair.Key)
                        .Distinct()
                        .ToList();
                }

                return
                    this.CurrentAccount.AllFriends
                        .Where(pair => pair.Value.Contains(this.CurrentCharacter.Name))
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
                return this.ignored;
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
                return this.isAuthenticated;
            }

            set
            {
                this.isAuthenticated = value;
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
                return this.globalMods;
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
                return this.notifications;
            }
        }

        /// <summary>
        ///     Gets the online bookmarks.
        /// </summary>
        public IEnumerable<ICharacter> OnlineBookmarks
        {
            get
            {
                return this.onlineBookmarkCache
                       ?? (this.onlineBookmarkCache =
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
                return this.onlineCharactersCache
                       ?? (this.onlineCharactersCache = this.OnlineCharactersDictionary.Values.ToList());
            }
        }

        /// <summary>
        ///     Gets the online friends.
        /// </summary>
        public IEnumerable<ICharacter> OnlineFriends
        {
            get
            {
                if (this.onlineFriendCache == null && this.Friends != null)
                {
                    this.onlineFriendCache =
                        this.OnlineCharacters.Where(
                            character =>
                            this.Friends.Any(
                                friend => character.Name.Equals(friend, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                }

                return this.onlineFriendCache;
            }
        }

        /// <summary>
        ///     Gets the online global mods.
        /// </summary>
        public IEnumerable<ICharacter> OnlineGlobalMods
        {
            get
            {
                return this.onlineModsCache
                       ?? (this.onlineModsCache =
                           this.OnlineCharacters.Where(
                               character =>
                               (character != null
                                && this.globalMods.Any(
                                    mod => mod.Equals(character.Name, StringComparison.OrdinalIgnoreCase)))).ToList());
            }
        }

        /// <summary>
        ///     Gets or sets the our account.
        /// </summary>
        public IAccount CurrentAccount
        {
            get
            {
                return this.account;
            }

            set
            {
                this.account = value;
                this.OnPropertyChanged("OurAccount");
            }
        }

        /// <summary>
        ///     Gets or sets the current channel.
        /// </summary>
        public ChannelModel CurrentChannel
        {
            get
            {
                return this.currentChannel;
            }

            set
            {
                if (this.currentChannel == value || value == null)
                {
                    return;
                }

                this.currentChannel = value;

                if (this.SelectedChannelChanged != null)
                {
                    this.SelectedChannelChanged(this, new EventArgs());
                }

                this.OnPropertyChanged("CurrentChannel");
            }
        }

        /// <summary>
        ///     Gets or sets the current character.
        /// </summary>
        public ICharacter CurrentCharacter
        {
            get
            {
                return this.currentCharacter;
            }

            set
            {
                this.currentCharacter = value;
                this.OnPropertyChanged("CurrentCharacter");
            }
        }

        /// <summary>
        ///     Gets or sets the server up time.
        /// </summary>
        public DateTimeOffset ServerUpTime { get; set; }

        /// <summary>
        ///     Gets the online characters dictionary.
        /// </summary>
        private IDictionary<string, ICharacter> OnlineCharactersDictionary
        {
            get
            {
                return this.onlineCharacters;
            }
        }
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
                character.IsInteresting = this.IsOfInterest(character.Name);
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

            Console.WriteLine("Unknown character: " + name);
            return new CharacterModel { Name = name, Status = StatusType.offline };
        }

        public ChannelModel FindChannel(string id, string title = null)
        {
            var channel = this.AllChannels.FirstByIdOrDefault(id);

            return channel ?? new GeneralChannelModel(id, ChannelType.InviteOnly) { Title = title };
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
                   || this.CurrentPMs.Any(pm => pm.Id.Equals(character, StringComparison.OrdinalIgnoreCase));
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
            var target = this.FindCharacter(character);
            if (!this.Interested.Contains(character))
            {
                this.Interested.Add(character);
                target.IsInteresting = true;
                if (this.NotInterested.Contains(character))
                {
                    this.NotInterested.Remove(character);
                }
            }
            else
            {
                this.Interested.Remove(character);
                target.IsInteresting = this.IsOfInterest(character);
            }

            SettingsDaemon.SaveApplicationSettingsToXml(this.CurrentCharacter.Name);
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
                    this.FindCharacter(character).IsInteresting = this.IsOfInterest(character);
                }
            }
            else
            {
                this.NotInterested.Remove(character);
                this.FindCharacter(character).IsInteresting = this.IsOfInterest(character);
            }

            SettingsDaemon.SaveApplicationSettingsToXml(this.CurrentCharacter.Name);
        }

        public void FriendsChanged()
        {
            this.onlineFriendCache = null;
            this.OnPropertyChanged("Friends");
            this.OnPropertyChanged("OnlineFriends");
        }
        #endregion

        #region Methods

        private void UpdateBindings(string name)
        {
            if (this.Bookmarks.Contains(name))
            {
                this.onlineBookmarkCache = null;
                this.OnPropertyChanged("OnlineBookmarks");
            }

            if (this.Friends.Contains(name))
            {
                this.onlineFriendCache = null;
                this.OnPropertyChanged("OnlineFriends");
            }

            if (!this.Mods.Contains(name))
            {
                return;
            }

            this.onlineModsCache = null;
            this.OnPropertyChanged("OnlineGlobalMods");
        }

        private void UpdateCharacterList(bool force)
        {
            if (!force && this.lastCharacterListCache.AddSeconds(15) >= DateTime.Now)
            {
                return;
            }

            this.onlineCharactersCache = this.onlineCharacters.Values.ToList();
            this.lastCharacterListCache = DateTime.Now;
            this.OnPropertyChanged("OnlineCharacters");
        }

        #endregion
    }
}