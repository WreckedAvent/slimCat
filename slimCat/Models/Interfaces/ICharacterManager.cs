#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICharacterManager.cs">
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

    using SimpleJson;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    ///     Represents several lists for managing and interacting with characters.
    /// </summary>
    public interface ICharacterManager : IDisposable
    {
        /// <summary>
        ///     Gets the character dictionary.
        /// </summary>
        ConcurrentDictionary<string, ICharacter> CharacterDictionary { get; }

        /// <summary>
        ///     Gets the characters.
        /// </summary>
        ICollection<ICharacter> Characters { get; }

        /// <summary>
        ///     Gets the sorted characters.
        /// </summary>
        /// <remarks>
        ///     Missing a sort implementation.
        /// </remarks>
        ICollection<ICharacter> SortedCharacters { get; }

        /// <summary>
        ///     Gets the current character count.
        /// </summary>
        int CharacterCount { get; }

        /// <summary>
        ///     Finds the character with the specified name.
        /// </summary>
        ICharacter Find(string name);

        /// <summary>
        ///     Removes a character from both offline and online lists of a specific list. Thread-safe.
        /// </summary>
        /// <param name="name">The name of the character to remove</param>
        /// <param name="listKind">Which kind of list to remove from</param>
        /// <param name="isTemporary">If the remove is saved to the character's settings</param>
        bool Remove(string name, ListKind listKind, bool isTemporary = false);

        /// <summary>
        ///     Adds a character to both the offline and online lists for a specific list. Thread-safe.
        /// </summary>
        /// <param name="name">The name of the character to add</param>
        /// <param name="listKind">Which kind of list to add to</param>
        /// <param name="isTemporary">If the add is saved to the character's settings</param>
        bool Add(string name, ListKind listKind, bool isTemporary = false);

        /// <summary>
        ///     Adds a character to online lists if they are on the offline ones. Thread-safe.
        /// </summary>
        /// <param name="character">The character to sign on</param>
        bool SignOn(ICharacter character);

        /// <summary>
        ///     Removes a character from online lists, but not from the offline ones. Thread-safe.
        /// </summary>
        /// <param name="name">The name of the character to sign off</param>
        bool SignOff(string name);

        /// <summary>
        ///     Returns all character names on a specified list. Thread-safe.
        /// </summary>
        /// <param name="listKind">The kind of list to read. Cannot be 'Online'.</param>
        /// <param name="onlineOnly">Whether or not to only include online characters</param>
        /// <returns>A collection of the characters on the specified list</returns>
        ICollection<string> GetNames(ListKind listKind, bool onlineOnly = true);

        /// <summary>
        ///     Sets the backing list of offline names for a given list to the supplied value. Thread-safe.
        /// </summary>
        /// <param name="listKind">The kind of list to set. Cannot be 'Online'</param>
        /// <param name="names">The list of names to set the offline list to</param>
        void Set(IEnumerable<string> names, ListKind listKind);

        void Set(JsonArray names, ListKind listKind);

        /// <summary>
        ///     Evaluates if a given name is of interest to a user. Thread-safe.
        /// </summary>
        /// <param name="name">The character name to check</param>
        /// <param name="onlineOnly">If only the online list should be checked</param>
        /// <returns>Whether or not the character is of interest</returns>
        bool IsOfInterest(string name, bool onlineOnly = true);

        /// <summary>
        ///     Evaluates if a given name is on a given list. Thread-safe.
        /// </summary>
        /// <param name="name">The character name to check</param>
        /// <param name="listKind">The kind of list to check</param>
        /// <param name="onlineOnly">Whether to check only the online list or not</param>
        /// <returns>Whether or not the character is on the given list</returns>
        bool IsOnList(string name, ListKind listKind, bool onlineOnly = true);

        /// <summary>
        ///     Returns all character names joined to a character model on a given list. Thread-safe.
        /// </summary>
        /// <param name="listKind"></param>
        /// <param name="onlineOnly"></param>
        /// <returns></returns>
        ICollection<ICharacter> GetCharacters(ListKind listKind, bool onlineOnly = true);

        /// <summary>
        ///     Clears this instance.
        /// </summary>
        void Clear();
    }
}