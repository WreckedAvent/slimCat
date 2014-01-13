namespace Slimcat.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Utilities;

    public abstract class CharacterManagerBase : ICharacterManager
    {
        private readonly ConcurrentDictionary<string, ICharacter> characters = new ConcurrentDictionary<string, ICharacter>(StringComparer.OrdinalIgnoreCase);

        protected HashSet<CollectionPair> Collections = new HashSet<CollectionPair>();
        protected Dictionary<ListKind, CollectionPair> CollectionDictionary = new Dictionary<ListKind, CollectionPair>();
        protected HashSet<CollectionPair> OfInterestCollections = new HashSet<CollectionPair>();
        protected readonly object Locker = new object();

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

        public virtual ICollection<ICharacter> SortedCharacters
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
            lock (Locker)
            {
                ICharacter character;
                if (characters.TryRemove(name, out character))
                    Collections.Each(x => x.Remove(name));
            }
        }

        public void Add(string name, ListKind listKind)
        {
            lock (Locker)
            {
                CollectionPair toModify;
                if (CollectionDictionary.TryGetValue(listKind, out toModify))
                    toModify.Add(name);
            }
        }

        public void SignOn(ICharacter character)
        {
            lock (Locker)
            {
                var name = character.Name;
                if (characters.TryAdd(name, character))
                    Collections.Each(x => x.SignOn(name));
            }
        }

        public void SignOff(string name)
        {
            lock (Locker)
            {
                ICharacter character;
                if (characters.TryRemove(name, out character))
                    Collections.Each(x => x.SignOff(name));
            }
        }

        public ICollection<string> Get(ListKind listKind, bool onlineOnly = true)
        {
            lock (Locker)
            {
                CollectionPair list;
                if (CollectionDictionary.TryGetValue(listKind, out list))
                    return onlineOnly ? list.OnlineList : list.List;

                return null;
            }
        }

        public void Set(IEnumerable<string> names, ListKind listKind)
        {
            lock (Locker)
            {
                CollectionPair list;
                if (CollectionDictionary.TryGetValue(listKind, out list))
                    list.Set(names);
            }
        }

        public bool IsOnList(string name, ListKind listKind, bool onlineOnly = true)
        {
            lock (Locker)
            {
                CollectionPair list;
                if (CollectionDictionary.TryGetValue(listKind, out list))
                    return onlineOnly 
                        ? list.OnlineList.Contains(name) 
                        : list.List.Contains(name);
                return false;
            }
        }

        public abstract bool IsOfInterest(string name);

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool isManaged)
        {
            characters.Clear();
            Collections.Clear();
            CollectionDictionary.Clear();
            OfInterestCollections.Clear();
        }
    }
}