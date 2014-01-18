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

namespace Slimcat.Models
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Practices.Prism.Events;

    #endregion

    public class GlobalCharacterManager : CharacterManagerBase
    {
        private readonly IAccount account;
        private readonly IEventAggregator eventAggregator;
        private readonly CollectionPair bookmarks = new CollectionPair();
        private readonly CollectionPair friends = new CollectionPair();
        private readonly CollectionPair ignored = new CollectionPair();
        private readonly CollectionPair interested = new CollectionPair();
        private readonly CollectionPair moderators = new CollectionPair();
        private readonly CollectionPair notInterested = new CollectionPair();

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
                    ignored
                };

            CollectionDictionary = new Dictionary<ListKind, CollectionPair>
                {
                    {ListKind.Bookmark, bookmarks},
                    {ListKind.Friend, friends},
                    {ListKind.Interested, interested},
                    {ListKind.Moderator, moderators},
                    {ListKind.NotInterested, notInterested},
                    {ListKind.Ignored, ignored}
                };

            OfInterestCollections = new HashSet<CollectionPair>
                {
                    bookmarks,
                    friends,
                    interested
                };

            eventAggregator.GetEvent<CharacterSelectedLoginEvent>().Subscribe(InitializeBookmarks);
        }

        public override bool IsOfInterest(string name)
        {
            lock (Locker)
            {
                var isOfInterest = false;
                foreach (var list in OfInterestCollections)
                {
                    isOfInterest = list.OnlineList.Contains(name);
                    if (isOfInterest) break;
                }

                return isOfInterest;
            }
        }

        private void InitializeBookmarks(string _)
        {
            bookmarks.Set(account.Bookmarks);
            friends.Set(account.AllFriends.Select(x => x.Key)); // todo: manage if friends are global or not
            eventAggregator.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(InitializeBookmarks);
        }

        public override bool SignOn(ICharacter character)
        {
            var toReturn = base.SignOn(character);
            lock (Locker)
            {
                character.IsInteresting = IsOfInterest(character.Name);
            }
            return toReturn;
        }

        public override bool Add(string name, ListKind listKind)
        {
            var toReturn = base.Add(name, listKind);

            if (listKind == ListKind.Interested || listKind == ListKind.NotInterested)
                SyncInterestedMarks(name, listKind, true);

            return toReturn;
        }

        public override bool Remove(string name, ListKind listKind)
        {
            var toReturn = base.Remove(name, listKind);

            if (listKind == ListKind.Interested || listKind == ListKind.NotInterested)
                SyncInterestedMarks(name, listKind, false);

            return toReturn;
        }

        private void SyncInterestedMarks(string name, ListKind listKind, bool isAdd)
        {
            lock (Locker)
            {
                var isInteresting = listKind == ListKind.Interested;
                var oppositeList = isInteresting ? notInterested : interested;
                var sameList = isInteresting ? interested : notInterested;
                Action<CollectionPair> addRemove = c =>
                    {
                        if (isAdd)
                            c.Add(name);
                        else
                            c.Remove(name);
                    };

                if (isAdd) // if we're adding to one, then we have to remove from the other
                {
                    oppositeList.Remove(name);
                }

                // now we do the actual action on the list specified
                addRemove(sameList);

                ICharacter toModify;
                if (CharacterDictionary.TryGetValue(name, out toModify))
                {
                    toModify.IsInteresting = isInteresting && isAdd;
                }
            }
        }
    }
}