namespace Slimcat.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using SimpleJson;
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

        public bool Remove(string name, ListKind listKind)
        {
            lock (Locker)
            {
                CollectionPair toModify;
                return CollectionDictionary.TryGetValue(listKind, out toModify) && toModify.Remove(name);
            }
        }

        public bool Add(string name, ListKind listKind)
        {
            lock (Locker)
            {
                CollectionPair toModify;
                return CollectionDictionary.TryGetValue(listKind, out toModify) && toModify.Add(name);
            }
        }

        public bool SignOn(ICharacter character)
        {
            lock (Locker)
            {
                var name = character.Name;
                if (!characters.TryAdd(name, character)) return false;

                Collections.Each(x => x.SignOn(name));
                return true;
            }
        }

        public bool SignOff(string name)
        {
            lock (Locker)
            {
                ICharacter character;
                if (!characters.TryRemove(name, out character)) return false;

                Collections.Each(x => x.SignOff(name));
                return true;
            }
        }

        public ICollection<string> GetNames(ListKind listKind, bool onlineOnly = true)
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

        public void Set(JsonArray array, ListKind listKind)
        {
            var names = array.ConvertAll(x => x.ToString());
            Set(names, listKind);
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

        public ICollection<ICharacter> GetCharacters(ListKind listKind, bool isOnlineOnly = true)
        {
            lock (Locker)
            {
                var names = GetNames(listKind, isOnlineOnly);
                return names.Select(Find).ToList();
            }
        } 

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool isManaged)
        {
            characters.Clear();
        }
    }
}