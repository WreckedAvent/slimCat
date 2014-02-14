#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterManagerBase.cs">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using SimpleJson;
    using Utilities;

    #endregion

    public abstract class CharacterManagerBase : ICharacterManager
    {
        protected readonly object Locker = new object();

        private readonly ConcurrentDictionary<string, ICharacter> characters =
            new ConcurrentDictionary<string, ICharacter>(StringComparer.OrdinalIgnoreCase);

        protected Dictionary<ListKind, CollectionPair> CollectionDictionary = new Dictionary<ListKind, CollectionPair>();
        protected HashSet<CollectionPair> Collections = new HashSet<CollectionPair>();
        protected HashSet<CollectionPair> OfInterestCollections = new HashSet<CollectionPair>();

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
                : new CharacterModel {Name = name, Status = StatusType.Offline};
        }

        public virtual bool Remove(string name, ListKind listKind, bool isTemporary = false)
        {
            lock (Locker)
            {
                CollectionPair toModify;
                return CollectionDictionary.TryGetValue(listKind, out toModify) && toModify.Remove(name);
            }
        }

        public virtual bool Add(string name, ListKind listKind, bool isTemporary = false)
        {
            lock (Locker)
            {
                CollectionPair toModify;
                return CollectionDictionary.TryGetValue(listKind, out toModify) && toModify.Add(name);
            }
        }

        public virtual bool SignOn(ICharacter character)
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
            var names = array.ConvertAll(x => x.ToString()).Where(x => !string.IsNullOrWhiteSpace(x));
            Set(names, listKind);
        }

        public virtual bool IsOnList(string name, ListKind listKind, bool onlineOnly = true)
        {
            lock (Locker)
            {
                if (listKind == ListKind.Online)
                    return characters.ContainsKey(name);

                CollectionPair list;
                if (CollectionDictionary.TryGetValue(listKind, out list))
                {
                    return onlineOnly
                        ? list.OnlineList.Contains(name)
                        : list.List.Contains(name);
                }
                return false;
            }
        }

        public abstract bool IsOfInterest(string name, bool onlineOnly = true);

        public virtual ICollection<ICharacter> GetCharacters(ListKind listKind, bool isOnlineOnly = true)
        {
            lock (Locker)
            {
                var names = GetNames(listKind, isOnlineOnly);
                return names.Select(Find).ToList();
            }
        }

        public virtual void Clear()
        {
            characters.Clear();
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