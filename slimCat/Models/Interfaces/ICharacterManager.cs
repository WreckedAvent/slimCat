namespace Slimcat.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// A generic way to
    /// </summary>
    public interface ICharacterManager : IDisposable
    {
        ConcurrentDictionary<string, ICharacter> CharacterDictionary { get; }

        ICollection<ICharacter> Characters { get; }

        ICollection<ICharacter> SortedCharacters { get; } 

        int CharacterCount { get; }

        ICharacter Find(string name);

        /// <summary>
        /// Removes a character from both offline and online lists, where applicable. Thread-safe.
        /// </summary>
        /// <param name="name">The name of the character to remove</param>
        /// <param name="listKind">Which kind of list to remove from</param>
        void Remove(string name, ListKind listKind);

        /// <summary>
        /// Adds a character to both the offline and online lists. Thread-safe.
        /// </summary>
        /// <param name="name">The name of the character to add</param>
        /// <param name="listKind">Which kind of list to add to</param>
        void Add(string name, ListKind listKind);

        /// <summary>
        /// Adds a character to online lists if they are on the offline ones. Thread-safe.
        /// </summary>
        /// <param name="character">The character to sign on</param>
        void SignOn(ICharacter character);

        /// <summary>
        /// Removes a character from online lists, but not from the offline ones. Thread-safe.
        /// </summary>
        /// <param name="name">The name of the character to sign off</param>
        void SignOff(string name);

        /// <summary>
        /// Returns all characters on a specified list. Thread-safe.
        /// </summary>
        /// <param name="listKind">The kind of list to read. Cannot be 'Online'.</param>
        /// <param name="onlineOnly">Whether or not to only include online characters</param>
        /// <returns>A collection of the characters on the specified list</returns>
        ICollection<string> Get(ListKind listKind, bool onlineOnly = true);

        /// <summary>
        /// Sets the backing list of offline names for a given list to the supplied value. Thread-safe.
        /// </summary>
        /// <param name="listKind">The kind of list to set. Cannot be 'Online'</param>
        /// <param name="names">The list of names to set the offline list to</param>
        void Set(IEnumerable<string> names, ListKind listKind);

        /// <summary>
        /// Evaluates if a given name is of interest to a user. Thread-safe.
        /// </summary>
        /// <param name="name">The character name to check</param>
        /// <returns>Whether or not the character is of interest</returns>
        bool IsOfInterest(string name);

        /// <summary>
        /// Evaluates if a given name is on a given list. Thread-safe.
        /// </summary>
        /// <param name="name">The character name to check</param>
        /// <param name="listKind">The kind of list to check</param>
        /// <param name="onlineOnly">Whether to check only the online list or not</param>
        /// <returns>Whether or not the character is on the given list</returns>
        bool IsOnList(string name, ListKind listKind, bool onlineOnly = true);
    }
}
