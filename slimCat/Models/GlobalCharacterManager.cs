#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalCharacterManager.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Practices.Prism.Events;
    using Services;
    using Utilities;

    #endregion

    public class GlobalCharacterManager : CharacterManagerBase
    {
        #region Fields

        private readonly IAccount account;

        private readonly CollectionPair bookmarks = new CollectionPair();
        private readonly IEventAggregator eventAggregator;
        private readonly CollectionPair friends = new CollectionPair();
        private readonly CollectionPair ignoreUpdates = new CollectionPair();
        private readonly CollectionPair ignored = new CollectionPair();
        private readonly CollectionPair interested = new CollectionPair();
        private readonly CollectionPair localFriends = new CollectionPair();
        private readonly CollectionPair moderators = new CollectionPair();
        private readonly CollectionPair notInterested = new CollectionPair();
        private readonly IDictionary<ListKind, IList<string>> savedCollections;
        private string currentCharacter;

        #endregion

        #region Constructors

        public GlobalCharacterManager(IAccount account, IEventAggregator eventAggregator)
        {
            this.account = account;
            this.eventAggregator = eventAggregator;

            Collections = new HashSet<CollectionPair>
                {
                    bookmarks,
                    friends,
                    moderators,
                    interested,
                    notInterested,
                    ignored,
                    ignoreUpdates
                };

            CollectionDictionary = new Dictionary<ListKind, CollectionPair>
                {
                    {ListKind.Bookmark, bookmarks},
                    {ListKind.Friend, friends},
                    {ListKind.Interested, interested},
                    {ListKind.Moderator, moderators},
                    {ListKind.NotInterested, notInterested},
                    {ListKind.Ignored, ignored},
                    {ListKind.IgnoreUpdates, ignoreUpdates}
                };

            savedCollections = new Dictionary<ListKind, IList<string>>
                {
                    {ListKind.Interested, ApplicationSettings.Interested},
                    {ListKind.NotInterested, ApplicationSettings.NotInterested},
                    {ListKind.IgnoreUpdates, ApplicationSettings.IgnoreUpdates}
                };

            OfInterestCollections = new HashSet<CollectionPair>
                {
                    bookmarks,
                    friends,
                    interested
                };

            eventAggregator.GetEvent<CharacterSelectedLoginEvent>().Subscribe(Initialize);
        }

        #endregion

        #region Public Methods

        public override bool IsOfInterest(string name, bool onlineOnly = true)
        {
            if (name == null) return false;

            lock (Locker)
            {
                var isOfInterest = false;
                foreach (var list in OfInterestCollections)
                {
                    isOfInterest = onlineOnly
                        ? list.OnlineList.Contains(name)
                        : list.List.Contains(name);
                    if (isOfInterest) break;
                }

                if (isOfInterest && friends.List.Contains(name) && !ApplicationSettings.FriendsAreAccountWide)
                    isOfInterest = localFriends.List.Contains(name);

                return isOfInterest;
            }
        }

        public override bool IsOnList(string name, ListKind listKind, bool onlineOnly = true)
        {
            var toReturn = base.IsOnList(name, listKind, onlineOnly);
            if (listKind == ListKind.Friend && !ApplicationSettings.FriendsAreAccountWide)
                toReturn = localFriends.List.Contains(name);

            return toReturn;
        }

        public override ICollection<ICharacter> GetCharacters(ListKind listKind, bool isOnlineOnly = true)
        {
            var characters = base.GetCharacters(listKind, isOnlineOnly);
            if (listKind == ListKind.Friend && !ApplicationSettings.FriendsAreAccountWide)
                characters = characters.Where(x => localFriends.List.Contains(x.Name)).ToList();

            return characters;
        }

        public override bool SignOn(ICharacter character)
        {
            var toReturn = base.SignOn(character);
            lock (Locker)
            {
                character.IsInteresting = IsOfInterest(character.Name);
                character.IgnoreUpdates = IsOnList(character.Name, ListKind.IgnoreUpdates, false);
            }
            return toReturn;
        }

        public override bool Add(string name, ListKind listKind, bool isTemporary = false)
        {
            var toReturn = base.Add(name, listKind, isTemporary);

            if (listKind == ListKind.Interested || listKind == ListKind.NotInterested)
                SyncInterestedMarks(name, listKind, true, isTemporary);

            if (listKind == ListKind.IgnoreUpdates)
                UpdateIgnoreUpdatesMark(name, true);

            if (!isTemporary)
                TrySyncSavedLists(listKind);

            return toReturn;
        }

        public override bool Remove(string name, ListKind listKind, bool isTemporary = false)
        {
            var toReturn = base.Remove(name, listKind, isTemporary);

            if (listKind == ListKind.Interested || listKind == ListKind.NotInterested)
                SyncInterestedMarks(name, listKind, false, isTemporary);

            if (listKind == ListKind.IgnoreUpdates)
                UpdateIgnoreUpdatesMark(name, false);

            if (!isTemporary)
                TrySyncSavedLists(listKind);

            return toReturn;
        }

        public override void Set(IEnumerable<string> names, ListKind listKind)
        {
            base.Set(names, listKind);

            if (savedCollections.Select(x => x.Key).Contains(listKind))
                TrySyncSavedLists(listKind, false);
        }

        #endregion

        #region Methods
        private void Initialize(string name)
        {
            currentCharacter = name;
            bookmarks.Set(account.Bookmarks);
            friends.Set(account.AllFriends.Select(x => x.Key));
            localFriends.Set(account.AllFriends.Where(x => x.Value.Contains(name)).Select(x => x.Key));
            eventAggregator.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(Initialize);
        }

        private void TrySyncSavedLists(ListKind listKind, bool save = true)
        {
            IList<string> savedCollection;
            if (!savedCollections.TryGetValue(listKind, out savedCollection)) return;

            CollectionPair currentCollection;
            if (!CollectionDictionary.TryGetValue(listKind, out currentCollection)) return;

            savedCollection.Clear();
            currentCollection.List.Each(savedCollection.Add);

            if (save)
                SettingsService.SaveApplicationSettingsToXml(currentCharacter);
        }

        private void UpdateIgnoreUpdatesMark(string name, bool isAdd)
        {
            ICharacter toModify;
            if (CharacterDictionary.TryGetValue(name, out toModify))
                toModify.IgnoreUpdates = isAdd;
        }

        private void SyncInterestedMarks(string name, ListKind listKind, bool isAdd, bool isTemporary)
        {
            lock (Locker)
            {
                var isInteresting = listKind == ListKind.Interested;
                var oppositeList = isInteresting ? notInterested : interested;
                var sameList = isInteresting ? interested : notInterested;
                Action<CollectionPair> addRemove = c =>
                    {
                        if (isAdd)
                            c.Add(name, isTemporary);
                        else
                            c.Remove(name, isTemporary);
                    };

                if (isAdd) // if we're adding to one, then we have to remove from the other
                    oppositeList.Remove(name, isTemporary);

                // now we do the actual action on the list specified
                addRemove(sameList);

                ICharacter toModify;
                if (CharacterDictionary.TryGetValue(name, out toModify))
                {
                    toModify.IsInteresting = (isInteresting && isAdd) || IsOfInterest(name, false);
                }
            }
        }

        #endregion
    }
}