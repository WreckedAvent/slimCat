namespace Slimcat.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.Practices.ObjectBuilder2;

    public class OnlineCharacterLists : IOnlineCharacterLists
    {
        private class CollectionPair
        {
            public HashSet<string> List { get; private set; }

            public HashSet<string> OnlineList { get; private set; }

            public CollectionPair()
            {
                List = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                OnlineList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            public void Add(string name)
            {
                List.Add(name);
                OnlineList.Add(name);
            }

            public void Set(IEnumerable<string> collection)
            {
                List.Clear();
                List.UnionWith(collection);
            }

            public void SignOff(string name)
            {
                OnlineList.Remove(name);
            }

            public void SignOn(string name)
            {
                if (List.Contains(name))
                    OnlineList.Add(name);
            }

            public void Remove(string name)
            {
                List.Remove(name);
                OnlineList.Remove(name);
            }
        }

        private readonly ConcurrentDictionary<string, ICharacter> characters = new ConcurrentDictionary<string, ICharacter>(StringComparer.OrdinalIgnoreCase);
        private readonly CollectionPair bookmarks = new CollectionPair();
        private readonly CollectionPair friends = new CollectionPair();
        private readonly CollectionPair moderators = new CollectionPair();
        private readonly CollectionPair interested = new CollectionPair();
        private readonly CollectionPair notInterested = new CollectionPair();
        private readonly CollectionPair ignored = new CollectionPair();

        private readonly HashSet<CollectionPair> collections;
        private readonly Dictionary<ListKind, CollectionPair> collectionDictionary;
        private readonly HashSet<CollectionPair> ofInterestCollections;  

        private readonly object locker = new object();

        public OnlineCharacterLists()
        {
            collections = new HashSet<CollectionPair>
                {
                    bookmarks,
                    friends,
                    moderators,
                    interested,
                    notInterested,
                    ignored
                };

            collectionDictionary = new Dictionary<ListKind, CollectionPair>
                {
                    {ListKind.Bookmark, bookmarks},
                    {ListKind.Friend, friends},
                    {ListKind.Interested, interested},
                    {ListKind.Moderator, moderators},
                    {ListKind.NotInterested, notInterested},
                    {ListKind.Ignored, ignored}
                };

            ofInterestCollections = new HashSet<CollectionPair>
                {
                    bookmarks,
                    friends,
                    interested
                };
        }

        public ConcurrentDictionary<string, ICharacter> CharacterDictionary
        {
            get { return characters; }
        }

        public int CharacterCount
        {
            get { return characters.Count; }
        }

        public ICollection<ICharacter> Characters
        {
            get { return characters.Values; }
        } 

        public ICharacter Find(string name)
        {
            ICharacter character;
            return characters.TryGetValue(name, out character) 
                ? character 
                : new CharacterModel{Name = name, Status = StatusType.Offline};
        }

        public void Remove(string name, ListKind listKind)
        {
            lock (locker)
            {
                ICharacter character;
                if (characters.TryRemove(name, out character))
                    collections.ForEach(x => x.Remove(name));
            }
        }

        public void Add(string name, ListKind listKind)
        {
            lock (locker)
            {
                CollectionPair toModify;
                if (collectionDictionary.TryGetValue(listKind, out toModify))
                    toModify.Add(name);
            }
        }

        public void SignOn(ICharacter character)
        {
            lock (locker)
            {
                var name = character.Name;
                if (characters.TryAdd(name, character))
                    collections.ForEach(x => x.SignOn(name));
            }
        }

        public void SignOff(string name)
        {
            lock (locker)
            {
                ICharacter character;
                if (characters.TryRemove(name, out character))
                    collections.ForEach(x => x.SignOff(name));
            }
        }

        public ICollection<string> Get(ListKind listKind, bool onlineOnly = true)
        {
            lock (locker)
            {
                CollectionPair list;
                if (collectionDictionary.TryGetValue(listKind, out list))
                    return onlineOnly ? list.OnlineList : list.List;

                return null;
            }
        }

        public void Set(ListKind listKind, IEnumerable<string> names)
        {
            lock (locker)
            {
                CollectionPair list;
                if (collectionDictionary.TryGetValue(listKind, out list))
                    list.Set(names);
            }
        }

        public bool IsOfInterest(string name)
        {
            lock (locker)
            {
                var isOfInterest = false;
                foreach (var list in ofInterestCollections)
                {
                    isOfInterest = list.OnlineList.Contains(name);
                    if (isOfInterest) break;
                }

                return isOfInterest;
            }
        }
    }
}
