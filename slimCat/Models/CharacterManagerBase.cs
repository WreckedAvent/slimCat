#region Copyright

// <copyright file="CharacterManagerBase.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
// 
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>

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
        #region Fields

        protected readonly object Locker = new object();

        protected Dictionary<ListKind, CollectionPair> CollectionDictionary = new Dictionary<ListKind, CollectionPair>();
        protected HashSet<CollectionPair> Collections = new HashSet<CollectionPair>();
        protected HashSet<CollectionPair> OfInterestCollections = new HashSet<CollectionPair>();

        #endregion

        #region Properties

        public ConcurrentDictionary<string, ICharacter> CharacterDictionary { get; } =
            new ConcurrentDictionary<string, ICharacter>(StringComparer.OrdinalIgnoreCase);

        public int CharacterCount => CharacterDictionary.Count;

        public ICollection<ICharacter> Characters => CharacterDictionary.Values;

        public virtual ICollection<ICharacter> SortedCharacters => CharacterDictionary.Values;

        #endregion

        #region Public Methods

        public ICharacter Find(string name)
        {
            ICharacter character;
            return CharacterDictionary.TryGetValue(name, out character)
                ? character
                : new CharacterModel {Name = name, Status = StatusType.Offline};
        }

        public virtual bool Remove(string name, ListKind listKind, bool isTemporary = false)
        {
            lock (Locker)
            {
                CollectionPair toModify;
                var toReturn = CollectionDictionary.TryGetValue(listKind, out toModify) &&
                               toModify.Remove(name, isTemporary);

                if (toReturn && listKind != ListKind.Online)
                    toModify.SignOff(name);

                return toReturn;
            }
        }

        public virtual bool Add(string name, ListKind listKind, bool isTemporary = false)
        {
            lock (Locker)
            {
                CollectionPair toModify;
                var toReturn = CollectionDictionary.TryGetValue(listKind, out toModify) &&
                               toModify.Add(name, isTemporary);

                if (toReturn && listKind != ListKind.Online && IsOnList(name, ListKind.Online))
                    toModify.SignOn(name);

                return toReturn;
            }
        }

        public virtual bool SignOn(ICharacter character)
        {
            lock (Locker)
            {
                var name = character.Name;
                if (!CharacterDictionary.TryAdd(name, character)) return false;

                Collections.Each(x => x.SignOn(name));
                return true;
            }
        }

        public bool SignOff(string name)
        {
            lock (Locker)
            {
                ICharacter character;
                var toReturn = CharacterDictionary.TryRemove(name, out character);

                Collections.Each(x => toReturn = toReturn | x.SignOff(name));
                return toReturn;
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

        public virtual void Set(IEnumerable<string> names, ListKind listKind)
        {
            lock (Locker)
            {
                CollectionPair list;
                if (!CollectionDictionary.TryGetValue(listKind, out list)) return;

                var namesCollection = names as IList<string> ?? names.ToList();
                list.Set(namesCollection);

                if (CharacterCount > 0 && listKind != ListKind.Online)
                    namesCollection.Each(name => list.SignOn(name));
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
                    return CharacterDictionary.ContainsKey(name);

                CollectionPair list;
                return CollectionDictionary.TryGetValue(listKind, out list) && list.IsOnList(name, onlineOnly);
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
            CharacterDictionary.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool isManaged)
        {
            CharacterDictionary.Clear();
        }

        #endregion
    }
}