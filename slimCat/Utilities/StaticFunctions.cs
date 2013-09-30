namespace Slimcat.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Windows;
    using System.Windows.Media;

    using Slimcat.Models;

    /// <summary>
    ///     The static functions.
    /// </summary>
    public static class StaticFunctions
    {
        #region Public Methods and Operators

        /// <summary>
        ///     The character is in list.
        /// </summary>
        /// <param name="collection">
        ///     The collection.
        /// </param>
        /// <param name="toFind">
        ///     The to find.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool CharacterIsInList(this ICollection<ICharacter> collection, ICharacter toFind)
        {
            return collection.Any(character => character.Name.Equals(toFind.Name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Checks if a string contains a term using ordinal string comparison
        /// </summary>
        /// <param name="fullString">
        ///     The full String.
        /// </param>
        /// <param name="checkterm">
        ///     The checkterm.
        /// </param>
        /// <param name="ignoreCase">
        ///     The ignore Case.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
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

            if (startIndex != -1)
            {
                // this checks for if the match is a whole word
                if (startIndex != 0)
                {
                    // this weeds out matches such as 'big man' from matching 'i'
                    var prevChar = fullString[startIndex - 1];
                    hasMatch = char.IsWhiteSpace(prevChar) || char.IsPunctuation(prevChar) && !prevChar.Equals('\'');

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
            }

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
            const int MaxDistance = 150;
            var needle = fullContent.ToLower().IndexOf(specificWord.ToLower(), StringComparison.Ordinal);

            var start = Math.Max(0, needle - MaxDistance);
            var end = Math.Min(fullContent.Length, needle + MaxDistance);

            Func<int, int> findStartOfWord = suspectIndex =>
                {
                    while (suspectIndex != 0 && !char.IsWhiteSpace(fullContent[suspectIndex]))
                    {
                        suspectIndex--; // find space before word
                    }

                    if (suspectIndex != 0)
                    {
                        suspectIndex++; // skip past space
                    }

                    return suspectIndex;
                };

            start = findStartOfWord(start);

            if (end != fullContent.Length)
            {
                end = findStartOfWord(end);
            }

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

            if (settings.NotifyIncludesCharacterNames)
            {
                if (message.Poster.Name.HasDingTermMatch(dingTerms))
                {
                    return true;
                }
            }

            return safeMessage.HasDingTermMatch(dingTerms);
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
                var safeTitle = Path.GetInvalidPathChars()
                                    .Union(new List<char> { ':' })
                                    .Aggregate(title, (current, c) => current.Replace(c.ToString(CultureInfo.InvariantCulture), string.Empty));

                if (safeTitle[0].Equals('.'))
                {
                    safeTitle = safeTitle.Remove(0, 1);
                }

                folderName = string.Format("{0} ({1})", safeTitle, id);
            }
            else
            {
                folderName = id;
            }

            if (folderName.ContainsOrdinal(@"/") || folderName.ContainsOrdinal(@"\"))
            {
                folderName = folderName.Replace('/', '-');
                folderName = folderName.Replace('\\', '-');
            }

            return Path.Combine(basePath, "slimCat", character, folderName);
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
            this ICharacter character, GenericSearchSettingsModel search, IChatModel cm, GeneralChannelModel channel)
        {
            // notice the toListing, this is an attempt to fix EnumerationChanged errors
            if (cm.Ignored.ToList().Contains(character.Name))
            {
                return search.ShowIgnored;
            }

            if (cm.NotInterested.ToList().Contains(character.Name))
            {
                return search.ShowNotInterested;
            }

            if (cm.Mods.ToList().Contains(character.Name))
            {
                return search.ShowMods;
            }

            if (channel != null)
            {
                if (channel.Moderators.ToList().Contains(character.Name))
                {
                    return search.ShowMods;
                }
            }

            if (cm.Friends.ToList().Contains(character.Name))
            {
                return search.ShowFriends;
            }

            if (cm.Bookmarks.ToList().Contains(character.Name))
            {
                return search.ShowBookmarks;
            }

            return search.MeetsStatusFilter(character);
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
            IChatModel cm, 
            GeneralChannelModel channel)
        {
            if (!character.NameContains(search.SearchString))
            {
                return false;
            }

            if (!genders.MeetsGenderFilter(character))
            {
                return false;
            }

            return character.MeetsChatModelLists(search, cm, channel);
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
            IChatModel cm, 
            GeneralChannelModel channel)
        {
            if (!message.Poster.NameContains(search.SearchString)
                && !message.Message.ContainsOrdinal(search.SearchString))
            {
                return false;
            }

            if (!genders.MeetsGenderFilter(message.Poster))
            {
                return false;
            }

            return message.Poster.MeetsChatModelLists(search, cm, channel);
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
        public static string RelationshipToUser(this ICharacter character, IChatModel cm, GeneralChannelModel channel)
        {
            // first, push friends, bookmarks, and moderators to the top of the list
            if (cm.OnlineFriends.Contains(character))
            {
                return "a"; // Really important people!
            }

            if (cm.OnlineBookmarks.Contains(character))
            {
                return "b"; // Important people!
            }

            if (cm.Interested.Contains(character.Name))
            {
                return "c"; // interesting people!
            }

            if (cm.OnlineGlobalMods.Contains(character))
            {
                return "d"; // Useful people!
            }

            if (channel != null && channel.Moderators.Contains(character.Name))
            {
                return "d";
            }

            if (cm.Ignored.Contains(character.Name))
            {
                return "z"; // "I don't want to see this person"
            }

            if (cm.NotInterested.Contains(character.Name))
            {
                return "z"; // I also do not wish to see this person
            }

            // then sort then by status
            switch (character.Status)
            {
                case StatusType.Looking:
                    return "e"; // People we want to bone!
                case StatusType.Busy:
                    return "f"; // Not the most available, but still possible to play with
                case StatusType.Idle:
                case StatusType.Away:
                    return "g"; // probably not going to play with, lower on list
                case StatusType.Dnd:
                    return "h"; // most likely not going to play with, lowest aside ignored
                default:
                    return "e";
            }
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
            {
                return fullString;
            }

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
                {
                    return child as T;
                }

                var grandchild = FindChild<T>(child);

                if (grandchild != null)
                {
                    return grandchild;
                }
            }

            return default(T);
        }

        #endregion
    }
}