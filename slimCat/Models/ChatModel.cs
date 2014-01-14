#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatModel.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace Slimcat.Models
{
    #region Usings

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Services;
    using Utilities;
    using ViewModels;

    #endregion

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

        private readonly ConcurrentDictionary<string, ICharacter> onlineCharacters =
            new ConcurrentDictionary<string, ICharacter>(StringComparer.OrdinalIgnoreCase);

        private readonly ObservableCollection<GeneralChannelModel> ourChannels =
            new ObservableCollection<GeneralChannelModel>();

        private readonly ObservableCollection<PmChannelModel> pms = new ObservableCollection<PmChannelModel>();

        private IAccount account;
        private ChannelModel currentChannel;

        private ICharacter currentCharacter;

        private bool isAuthenticated;

        private DateTime lastCharacterListCache;

        // caches for speed improvements in filtering
        private IList<ICharacter> onlineBookmarkCache;

        private IList<ICharacter> onlineCharactersCache;

        private IList<ICharacter> onlineFriendCache;

        private IList<ICharacter> onlineModsCache;

        #endregion

        #region Public Events

        /// <summary>
        ///     The selected channel changed.
        /// </summary>
        public event EventHandler SelectedChannelChanged;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the online characters dictionary.
        /// </summary>
        private IDictionary<string, ICharacter> OnlineCharactersDictionary
        {
            get { return onlineCharacters; }
        }

        /// <summary>
        ///     Gets the bookmarks.
        /// </summary>
        public IList<string> Bookmarks
        {
            get { return CurrentAccount.Bookmarks; }
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
                    return CurrentAccount.AllFriends
                        .Select(pair => pair.Key)
                        .Distinct()
                        .ToList();
                }

                return
                    CurrentAccount.AllFriends
                        .Where(pair => pair.Value.Contains(CurrentCharacter.Name))
                        .Select(pair => pair.Key)
                        .ToList();
            }
        }

        /// <summary>
        ///     Gets the ignored.
        /// </summary>
        public IList<string> Ignored
        {
            get { return ignored; }
        }

        /// <summary>
        ///     Gets the interested.
        /// </summary>
        public IList<string> Interested
        {
            get { return ApplicationSettings.Interested; }
        }

        /// <summary>
        ///     Gets the mods.
        /// </summary>
        public IList<string> Mods
        {
            get { return globalMods; }
        }

        /// <summary>
        ///     Gets the not interested.
        /// </summary>
        public IList<string> NotInterested
        {
            get { return ApplicationSettings.NotInterested; }
        }

        /// <summary>
        ///     Gets the online bookmarks.
        /// </summary>
        public IList<ICharacter> OnlineBookmarks
        {
            get
            {
                return onlineBookmarkCache
                       ?? (onlineBookmarkCache =
                           OnlineCharacters.Where(
                               character =>
                                   Bookmarks.Any(
                                       bookmark =>
                                           (character != null
                                            && character.Name.Equals(bookmark, StringComparison.OrdinalIgnoreCase))))
                               .ToList());
            }
        }

        /// <summary>
        ///     Gets the online characters.
        /// </summary>
        public IList<ICharacter> OnlineCharacters
        {
            get
            {
                return onlineCharactersCache
                       ?? (onlineCharactersCache = OnlineCharactersDictionary.Values.ToList());
            }
        }

        public int OnlineCharacterCount
        {
            get { return onlineCharacters.Count; }
        }

        /// <summary>
        ///     Gets the online friends.
        /// </summary>
        public IList<ICharacter> OnlineFriends
        {
            get
            {
                if (onlineFriendCache == null && Friends != null)
                {
                    onlineFriendCache =
                        OnlineCharacters.Where(
                            character =>
                                Friends.Any(
                                    friend => character.Name.Equals(friend, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                }

                return onlineFriendCache;
            }
        }

        /// <summary>
        ///     Gets the online global mods.
        /// </summary>
        public IList<ICharacter> OnlineGlobalMods
        {
            get
            {
                return onlineModsCache
                       ?? (onlineModsCache =
                           OnlineCharacters.Where(
                               character =>
                                   (character != null
                                    && globalMods.Any(
                                        mod => mod.Equals(character.Name, StringComparison.OrdinalIgnoreCase))))
                               .ToList());
            }
        }

        /// <summary>
        ///     Gets the all channels.
        /// </summary>
        public ObservableCollection<GeneralChannelModel> AllChannels
        {
            get { return channels; }
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
            get { return ourChannels; }
        }

        /// <summary>
        ///     Gets the current private messages.
        /// </summary>
        public ObservableCollection<PmChannelModel> CurrentPms
        {
            get { return pms; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get { return isAuthenticated; }

            set
            {
                isAuthenticated = value;
                OnPropertyChanged("IsAuthenticated");
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
        ///     Gets the notifications.
        /// </summary>
        public ObservableCollection<NotificationModel> Notifications
        {
            get { return notifications; }
        }

        /// <summary>
        ///     Gets or sets the our account.
        /// </summary>
        public IAccount CurrentAccount
        {
            get { return account; }

            set
            {
                account = value;
                OnPropertyChanged("OurAccount");
            }
        }

        /// <summary>
        ///     Gets or sets the current channel.
        /// </summary>
        public ChannelModel CurrentChannel
        {
            get { return currentChannel; }

            set
            {
                if (currentChannel == value || value == null)
                    return;

                currentChannel = value;

                if (SelectedChannelChanged != null)
                    SelectedChannelChanged(this, new EventArgs());

                OnPropertyChanged("CurrentChannel");
            }
        }

        /// <summary>
        ///     Gets or sets the current character.
        /// </summary>
        public ICharacter CurrentCharacter
        {
            get { return currentCharacter; }

            set
            {
                currentCharacter = value;
                OnPropertyChanged("CurrentCharacter");
            }
        }

        /// <summary>
        ///     Gets or sets the server up time.
        /// </summary>
        public DateTimeOffset ServerUpTime { get; set; }

        #endregion

        #region Public Methods and Operators

        public ChannelModel FindChannel(string id, string title = null)
        {
            var channel = AllChannels.FirstByIdOrDefault(id);

            return channel ?? new GeneralChannelModel(id, ChannelType.InviteOnly) {Title = title};
        }

        public void Wipe()
        {
            Dispatcher.Invoke(
                (Action) delegate
                    {
                        onlineCharactersCache = null;

                        channels.Clear();
                        onlineCharacters.Clear();

                        onlineModsCache = null;
                        onlineBookmarkCache = null;
                        onlineFriendCache = null;
                    });
        }

        /// <summary>
        ///     The add character.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        public void AddCharacter(ICharacter character)
        {
            try
            {
                character.IsInteresting = IsOfInterest(character.Name);
                OnlineCharactersDictionary.Add(character.Name, character);
                UpdateCharacterList(IsOfInterest(character.Name));
                UpdateBindings(character.Name);
            }
            catch
            {
                Console.WriteLine("Error: Unable to add character: " + character.Name);
            }
        }

        /// <summary>
        ///     The find character.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     The <see cref="ICharacter" />.
        /// </returns>
        public ICharacter FindCharacter(string name)
        {
            ICharacter toReturn;
            return OnlineCharactersDictionary.TryGetValue(name, out toReturn)
                ? toReturn
                : new CharacterModel {Name = name, Status = StatusType.Offline};
        }

        /// <summary>
        ///     The is of interest.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool IsOfInterest(string character)
        {
            return (Bookmarks.Any(bookmark => bookmark.Equals(character, StringComparison.OrdinalIgnoreCase))
                    || Friends.Any(friend => friend.Equals(character, StringComparison.OrdinalIgnoreCase))
                    || Interested.Any(interest => interest.Equals(character, StringComparison.OrdinalIgnoreCase)))
                   || CurrentPms.Any(pm => pm.Id.Equals(character, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     The is online.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool IsOnline(string name)
        {
            return name != null && OnlineCharactersDictionary.ContainsKey(name);
        }

        /// <summary>
        ///     The remove character.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
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

        /// <summary>
        ///     The toggle interested mark.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        public void ToggleInterestedMark(string character)
        {
            var target = FindCharacter(character);
            if (!Interested.Contains(character))
            {
                Interested.Add(character);
                target.IsInteresting = true;
                if (NotInterested.Contains(character))
                    NotInterested.Remove(character);
            }
            else
            {
                Interested.Remove(character);
                target.IsInteresting = IsOfInterest(character);
            }

            SettingsDaemon.SaveApplicationSettingsToXml(CurrentCharacter.Name);
        }

        /// <summary>
        ///     The toggle not interested mark.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        public void ToggleNotInterestedMark(string character)
        {
            if (!NotInterested.Contains(character))
            {
                NotInterested.Add(character);
                if (Interested.Contains(character))
                {
                    Interested.Remove(character);
                    FindCharacter(character).IsInteresting = IsOfInterest(character);
                }
            }
            else
            {
                NotInterested.Remove(character);
                FindCharacter(character).IsInteresting = IsOfInterest(character);
            }

            SettingsDaemon.SaveApplicationSettingsToXml(CurrentCharacter.Name);
        }

        public void FriendsChanged()
        {
            onlineFriendCache = null;
            OnPropertyChanged("Friends");
            OnPropertyChanged("OnlineFriends");
        }

        #endregion

        #region Methods

        private void UpdateBindings(string name)
        {
            if (Bookmarks.Contains(name))
            {
                onlineBookmarkCache = null;
                OnPropertyChanged("OnlineBookmarks");
            }

            if (Friends.Contains(name))
            {
                onlineFriendCache = null;
                OnPropertyChanged("OnlineFriends");
            }

            if (!Mods.Contains(name))
                return;

            onlineModsCache = null;
            OnPropertyChanged("OnlineGlobalMods");
        }

        private void UpdateCharacterList(bool force)
        {
            if (!force && lastCharacterListCache.AddSeconds(15) >= DateTime.Now)
                return;

            onlineCharactersCache = onlineCharacters.Values.ToList();
            lastCharacterListCache = DateTime.Now;
            OnPropertyChanged("OnlineCharacters");
        }

        #endregion
    }
}