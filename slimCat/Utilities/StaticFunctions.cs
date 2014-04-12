#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StaticFunctions.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Windows;
    using System.Windows.Media;
    using Newtonsoft.Json;
    using Models;

    #endregion

    /// <summary>
    ///     The static functions.
    /// </summary>
    public static class StaticFunctions
    {
        #region Public Methods and Operators

        /// <summary>
        ///     The character is in list.
        /// </summary>
        public static bool CharacterIsInList(this ICollection<ICharacter> collection, ICharacter toFind)
        {
            return collection.Any(character => character.Name.Equals(toFind.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Checks if a string contains a term using ordinal string comparison
        /// </summary>
        public static bool ContainsOrdinal(this string fullString, string checkterm, bool ignoreCase = true)
        {
            var comparer = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return fullString.IndexOf(checkterm, comparer) >= 0;
        }

        /// <summary>
        ///     Checks if a given collection has a matching word or phrase. Returns the word and its context in a tuple.
        ///     Empty if no match.
        /// </summary>
        /// <param name="fullString">
        ///     The full String.
        /// </param>
        /// <param name="checkAgainst">
        ///     The check Against.
        /// </param>
        /// <returns>
        ///     The <see cref="Tuple" />.
        /// </returns>
        public static Tuple<string, string> FirstMatch(this string fullString, string checkAgainst)
        {
            var startIndex = fullString.IndexOf(checkAgainst, StringComparison.OrdinalIgnoreCase);

            var hasMatch = false;

            if (startIndex == -1)
                return new Tuple<string, string>(string.Empty, string.Empty);

            // this checks for if the match is a whole word
            if (startIndex != 0)
            {
                // this weeds out matches such as 'big man' from matching 'i'
                var prevChar = fullString[startIndex - 1];
                hasMatch = char.IsWhiteSpace(prevChar) || (char.IsPunctuation(prevChar) && !prevChar.Equals('\''));

                if (!hasMatch)
                {
                    return new Tuple<string, string>(string.Empty, string.Empty);

                    // don't need to evaluate further if this failed
                }
            }

            if (startIndex + checkAgainst.Length < fullString.Length)
            {
                // this weeds out matches such as 'its' from matching 'i'
                var nextIndex = startIndex + checkAgainst.Length;
                var nextChar = fullString[nextIndex];
                hasMatch = char.IsWhiteSpace(nextChar) || char.IsPunctuation(nextChar);

                // we only want the ' to match sometimes, such as <match word>'s
                if (nextChar == '\'' && fullString.Length >= nextIndex++)
                {
                    nextChar = fullString[nextIndex];
                    hasMatch = char.ToLower(nextChar) == 's';
                }
            }

            if (checkAgainst.Length == fullString.Length && checkAgainst == fullString)
                return new Tuple<string, string>(checkAgainst, checkAgainst);

            return hasMatch
                ? new Tuple<string, string>(checkAgainst, GetStringContext(fullString, checkAgainst))
                : new Tuple<string, string>(string.Empty, string.Empty);
        }

        /// <summary>
        ///     returns the sentence (ish) around a word
        /// </summary>
        /// <param name="fullContent">
        ///     The full Content.
        /// </param>
        /// <param name="specificWord">
        ///     The specific Word.
        /// </param>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public static string GetStringContext(string fullContent, string specificWord)
        {
            const int maxDistance = 150;
            var needle = fullContent.ToLower().IndexOf(specificWord.ToLower(), StringComparison.Ordinal);

            var start = Math.Max(0, needle - maxDistance);
            var end = Math.Min(fullContent.Length, needle + maxDistance);

            Func<int, int> findStartOfWord = suspectIndex =>
                {
                    while (suspectIndex != 0 && !char.IsWhiteSpace(fullContent[suspectIndex]))
                    {
                        suspectIndex--; // find space before word
                    }

                    if (suspectIndex != 0)
                        suspectIndex++; // skip past space

                    return suspectIndex;
                };

            start = findStartOfWord(start);

            if (end != fullContent.Length)
                end = findStartOfWord(end);

            return (start > 0 ? "... " : string.Empty) + fullContent.Substring(start, end - start)
                   + (end != fullContent.Length ? " ..." : string.Empty);
        }

        /// <summary>
        ///     Checks if checkAgainst contains any term in dingTerms
        /// </summary>
        /// <param name="checkAgainst">
        ///     The check Against.
        /// </param>
        /// <param name="dingTerms">
        ///     The ding Terms.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool HasDingTermMatch(this string checkAgainst, IEnumerable<string> dingTerms)
        {
            return dingTerms.Any(term => FirstMatch(checkAgainst, term).Item1 != string.Empty);
        }

        /// <summary>
        ///     Checks if an IMessage is a message which trips our ding terms
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="settings">
        ///     The settings.
        /// </param>
        /// <param name="dingTerms">
        ///     The ding Terms.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool IsDingMessage(
            this IMessage message, ChannelSettingsModel settings, IEnumerable<string> dingTerms)
        {
            var safeMessage = HttpUtility.HtmlDecode(message.Message);

            if (!settings.NotifyIncludesCharacterNames) return safeMessage.HasDingTermMatch(dingTerms);

            var enumeratedDingTerm = dingTerms as string[] ?? dingTerms.ToArray();
            return message.Poster.Name.HasDingTermMatch(enumeratedDingTerm)
                   || safeMessage.HasDingTermMatch(enumeratedDingTerm);
        }

        /// <summary>
        ///     Makes a safe folder path to our channel
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <param name="title">
        ///     The title.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public static string MakeSafeFolderPath(string character, string title, string id)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderName;

            if (!title.Equals(id))
            {
                var safeTitle = 
                    Path.GetInvalidPathChars()
                    .Union(Path.GetInvalidFileNameChars())
                    .Except(new [] {'/', '\\'})
                    .Aggregate(title,
                        (current, c) => current.Replace(c.ToString(CultureInfo.InvariantCulture), string.Empty));

                if (safeTitle[0].Equals('.'))
                    safeTitle = safeTitle.Remove(0, 1);

                folderName = string.Format("{0} ({1})", safeTitle, id);
            }
            else
                folderName = id;

            if (folderName.ContainsOrdinal(@"/") || folderName.ContainsOrdinal(@"\"))
            {
                folderName = folderName.Replace('/', '-');
                folderName = folderName.Replace('\\', '-');
            }

            return ApplicationSettings.PortableMode
                ? Path.Combine("logs", character, folderName)
                : Path.Combine(basePath, "slimCat", character, folderName);
        }

        /// <summary>
        ///     The meets chat model lists.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <param name="search">
        ///     The search.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="channel">
        ///     The channel.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool MeetsChatModelLists(
            this ICharacter character, GenericSearchSettingsModel search, ICharacterManager cm,
            GeneralChannelModel channel)
        {
            var name = character.Name;

            var map = new HashSet<KeyValuePair<ListKind, bool>>
                {
                    new KeyValuePair<ListKind, bool>(ListKind.Ignored, search.ShowIgnored),
                    new KeyValuePair<ListKind, bool>(ListKind.NotInterested, search.ShowNotInterested),
                    new KeyValuePair<ListKind, bool>(ListKind.Moderator, search.ShowMods),
                    new KeyValuePair<ListKind, bool>(ListKind.Friend, search.ShowFriends),
                    new KeyValuePair<ListKind, bool>(ListKind.Bookmark, search.ShowBookmarks)
                };

            // weee thread-safe functions
            foreach (var pair in map.Where(pair => cm.IsOnList(name, pair.Key)))
                return pair.Value;

            if (channel == null) return search.MeetsStatusFilter(character);

            return channel.CharacterManager.IsOnList(name, ListKind.Moderator)
                ? search.ShowMods
                : search.MeetsStatusFilter(character);
        }

        /// <summary>
        ///     The meets filters.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <param name="genders">
        ///     The genders.
        /// </param>
        /// <param name="search">
        ///     The search.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="channel">
        ///     The channel.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool MeetsFilters(
            this ICharacter character,
            GenderSettingsModel genders,
            GenericSearchSettingsModel search,
            ICharacterManager cm,
            GeneralChannelModel channel)
        {
            if (!character.NameContains(search.SearchString))
                return false;

            return genders.MeetsGenderFilter(character)
                   && character.MeetsChatModelLists(search, cm, channel);
        }

        /// <summary>
        ///     The meets filters.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="genders">
        ///     The genders.
        /// </param>
        /// <param name="search">
        ///     The search.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="channel">
        ///     The channel.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool MeetsFilters(
            this IMessage message,
            GenderSettingsModel genders,
            GenericSearchSettingsModel search,
            ICharacterManager cm,
            GeneralChannelModel channel)
        {
            if (!message.Poster.NameContains(search.SearchString)
                && !message.Message.ContainsOrdinal(search.SearchString))
                return false;

            return genders.MeetsGenderFilter(message.Poster)
                   && message.Poster.MeetsChatModelLists(search, cm, channel);
        }

        /// <summary>
        ///     The name contains.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <param name="searchString">
        ///     The search string.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool NameContains(this ICharacter character, string searchString)
        {
            return character.Name.ToLower().Contains(searchString.ToLower());
        }

        /// <summary>
        ///     The name equals.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <param name="compare">
        ///     The compare.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool NameEquals(this ICharacter character, string compare)
        {
            return character.Name.Equals(compare, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     The relationship to user.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="channel">
        ///     The channel.
        /// </param>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public static string RelationshipToUser(this ICharacter character, ICharacterManager cm,
            GeneralChannelModel channel)
        {
            var map = new HashSet<KeyValuePair<ListKind, string>>
                {
                    new KeyValuePair<ListKind, string>(ListKind.Friend, "a"),
                    new KeyValuePair<ListKind, string>(ListKind.Bookmark, "b"),
                    new KeyValuePair<ListKind, string>(ListKind.Interested, "c"),
                    new KeyValuePair<ListKind, string>(ListKind.Moderator, "d"),
                    new KeyValuePair<ListKind, string>(ListKind.Ignored, "z"),
                    new KeyValuePair<ListKind, string>(ListKind.NotInterested, "z"),
                };

            var statusMap = new Dictionary<StatusType, string>
                {
                    {StatusType.Looking, "e"},
                    {StatusType.Busy, "g"},
                    {StatusType.Idle, "h"},
                    {StatusType.Away, "i"},
                    {StatusType.Dnd, "y"}
                };

            foreach (var pair in map.Where(pair => cm.IsOnList(character.Name, pair.Key)))
                return pair.Value;

            if (channel != null && channel.CharacterManager.IsOnList(character.Name, ListKind.Moderator))
                return "d";

            string result;
            return statusMap.TryGetValue(character.Status, out result)
                ? result
                : "f";
        }

        /// <summary>
        ///     Strips the punctuation in a given string so long as it's at the end.
        ///     Words like it's will not be affected.
        /// </summary>
        /// <param name="fullString">
        ///     The full String.
        /// </param>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public static string StripPunctationAtEnd(this string fullString)
        {
            if (string.IsNullOrWhiteSpace(fullString) || fullString.Length <= 1)
                return fullString;

            var index = fullString.Length - 1;

            while (char.IsPunctuation(fullString[index]) && index != 0)
            {
                index--;
            }

            return index == 0 ? string.Empty : fullString.Substring(0, index);
        }

        public static T FindChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T)
                    return child as T;

                var grandchild = FindChild<T>(child);

                if (grandchild != null)
                    return grandchild;
            }

            return default(T);
        }

        /// <summary>
        ///     Deserializes the object.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of object to deserialize to.
        /// </typeparam>
        /// <param name="objectString">
        ///     The object string.
        /// </param>
        public static T DeserializeTo<T>(this string objectString)
        {
            return JsonConvert.DeserializeObject<T>(objectString);
        }
        #endregion
    }
}