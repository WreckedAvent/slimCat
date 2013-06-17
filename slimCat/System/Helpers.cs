/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace System
{
    public static class ExtensionMethods
    {
        public static T FirstByIdOrDefault<T>(this ICollection<T> model, string ID)
            where T : ChannelModel
        {
            return model.FirstOrDefault(param => param.ID.Equals(ID, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class StaticFunctions
    {
        #region ICharacter extensions
        public static bool CharacterIsInList(this ICollection<ICharacter> collection, ICharacter toFind)
        {
            return (collection.Any(character => character.Name.Equals(toFind.Name, StringComparison.OrdinalIgnoreCase)));
        }

        public static bool NameContains(this ICharacter character, string searchString)
        {
            return character.Name.ToLower().Contains(searchString);
        }
        #endregion

        #region Settings Functions
        public static bool MeetsFilters(this ICharacter character, GenderSettingsModel genders, GenericSearchSettingsModel search, IChatModel cm, GeneralChannelModel channel)
        {
            if (!character.NameContains(search.SearchString))
                return false;
            if (!genders.MeetsGenderFilter(character))
                return false;

            return character.MeetsChatModelLists(search, cm, channel);
        }

        public static bool MeetsFilters(this IMessage message, GenderSettingsModel genders, GenericSearchSettingsModel search, IChatModel cm, GeneralChannelModel channel)
        {
            if (!message.Poster.NameContains(search.SearchString) && !message.Message.ContainsOrd(search.SearchString, true))
                return false;
            if (!genders.MeetsGenderFilter(message.Poster))
                return false;

            return message.Poster.MeetsChatModelLists(search, cm, channel);
        }

        public static bool MeetsChatModelLists(this ICharacter character, GenericSearchSettingsModel search, IChatModel cm, GeneralChannelModel channel)
        {
            // notice the toListing, this is an attempt to fix EnumerationChanged errors

            if (cm.Ignored.ToList().Contains(character.Name))
                return search.ShowIgnored;

            if (cm.NotInterested.ToList().Contains(character.Name))
                return search.ShowNotInterested;

            if (cm.Mods.ToList().Contains(character.Name))
                return search.ShowMods;

            if (channel != null)
                if (channel.Moderators.ToList().Contains(character.Name))
                    return search.ShowMods;

            if (cm.Friends.ToList().Contains(character.Name))
                return search.ShowFriends;

            if (cm.Bookmarks.ToList().Contains(character.Name))
                return search.ShowBookmarks;

            return search.MeetsStatusFilter(character);
        }

        public static string RelationshipToUser(this ICharacter character, IChatModel cm, GeneralChannelModel channel)
        {
            // first, push friends, bookmarks, and moderators to the top of the list
            if (cm.OnlineFriends.Contains(character))
                return "a"; // Really important people!
            else if (cm.OnlineBookmarks.Contains(character))
                return "b"; // Important people!
            else if (cm.Interested.Contains(character.Name))
                return "c"; // interesting people!
            else if (cm.OnlineGlobalMods.Contains(character))
                return "d"; // Useful people!
            else if (channel != null && channel.Moderators.Contains(character.Name))
                return "d";
            else if (cm.Ignored.Contains(character.Name))
                return "z"; // "I don't want to see this person"
            else if (cm.NotInterested.Contains(character.Name))
                return "z"; // I also do not wish to see this person

            // then sort then by status
            else if (character.Status == StatusType.looking)
                return "e"; // People we want to bone!
            else if (character.Status == StatusType.busy)
                return "f"; // Not the most available, but still possible to play with
            else if (character.Status == StatusType.away || character.Status == StatusType.idle)
                return "g"; // probably not going to play with, lower on list
            else if (character.Status == StatusType.dnd)
                return "h"; // most likely not going to play with, lowest aside ignored
            else
                return "e"; // just normal joe user.
        }
        #endregion

        #region String functions
        /// <summary>
        /// returns the sentence (ish) around a word
        /// </summary>
        public static string GetStringContext(string fullContent, string specificWord)
        {
            const int maxDistance = 150;
            var needle = fullContent.ToLower().IndexOf(specificWord.ToLower());

            var start = Math.Max(0, needle - maxDistance);
            var end = Math.Min(fullContent.Length, needle + maxDistance);

            Func<int, int> findStartOfWord = (suspectIndex) =>
                {
                    while (suspectIndex != 0 && !Char.IsWhiteSpace(fullContent[suspectIndex]))
                        suspectIndex--; // find space before word
                    if (suspectIndex != 0)
                        suspectIndex++; // skip past space

                    return suspectIndex;
                };

            start = findStartOfWord(start);

            if (end != fullContent.Length)
                end = findStartOfWord(end);

            return (start > 0 ? "... " : "") + fullContent.Substring(start, (end - start)) + (end != fullContent.Length ? " ..." : "");
        }

        /// <summary>
        /// Strips the punctuation in a given string so long as it's at the end.
        /// Words like it's will not be affected.
        /// </summary>
        public static string StripPunctationAtEnd(this string fullString)
        {
            if (String.IsNullOrWhiteSpace(fullString) || fullString.Length <= 1)
                return fullString;

            var index = fullString.Length-1;

            while (char.IsPunctuation(fullString[index]) && index != 0)
                index--;

            if (index == 0)
                return string.Empty;
            else
                return fullString.Substring(0, index);
        }

        /// <summary>
        /// Checks if a string contains a term using ordinal string comparison
        /// </summary>
        public static bool ContainsOrd(this string fullString, string checkterm, bool ignoreCase)
        {
            StringComparison comparer = (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            return fullString.IndexOf(checkterm, comparer) >= 0;
        }

        /// <summary>
        /// Checks if a given collection has a matching word or phrase. Returns the word and its context in a tuple.
        /// Empty if no match.
        /// </summary>
        public static Tuple<string, string> FirstMatch(this string fullString, string checkAgainst)
        {
            var startIndex = fullString.IndexOf(checkAgainst, StringComparison.OrdinalIgnoreCase);

            bool hasMatch = false;

            if (startIndex != -1)
            {
                // this checks for if the match is a whole word
                if (startIndex != 0)
                {
                    // this weeds out matches such as 'big man' from matching 'i'
                    char prevChar = fullString[startIndex - 1];
                    hasMatch = char.IsWhiteSpace(prevChar) || char.IsPunctuation(prevChar) && !prevChar.Equals('\'');

                    if (!hasMatch)
                        return new Tuple<string, string>(string.Empty, string.Empty); // don't need to evaluate further if this failed
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

            if (hasMatch)
                return new Tuple<string, string>(checkAgainst, GetStringContext(fullString, checkAgainst));
            else
                return new Tuple<string, string>(string.Empty, string.Empty);
        }

        /// <summary>
        /// Checks if an IMessage is a message which trips our ding terms
        /// </summary>
        public static bool IsDingMessage(this IMessage message, ChannelSettingsModel settings, IEnumerable<string> dingTerms)
        {
            var safeMessage = System.Web.HttpUtility.HtmlDecode(message.Message);

            if (settings.NotifyIncludesCharacterNames)
                if (message.Poster.Name.HasDingTermMatch(dingTerms)) return true;
            if (safeMessage.HasDingTermMatch(dingTerms)) return true;

            return false;
        }

        /// <summary>
        /// Checks if checkAgainst contains any term in dingTerms
        /// </summary>
        public static bool HasDingTermMatch(this string checkAgainst, IEnumerable<string> dingTerms)
        {
            foreach (var term in dingTerms)
                if (FirstMatch(checkAgainst, term).Item1 != string.Empty)
                    return true;
            return false;
        }

        /// <summary>
        /// Makes a safe folder path to our channel
        /// </summary>
        public static string MakeSafeFolderPath(string character, string title, string ID)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderName;

            if (!title.Equals(ID))
            {
                string safeTitle = title;
                foreach (var c in Path.GetInvalidPathChars().Union(new List<char>() { ':' }))
                    safeTitle = safeTitle.Replace(c.ToString(), "");

                if (safeTitle[0].Equals('.'))
                    safeTitle = safeTitle.Remove(0, 1);

                folderName = string.Format("{0} ({1})", safeTitle, ID);
            }
            else
                folderName = ID;

            if (folderName.ContainsOrd(@"/", true) || folderName.ContainsOrd(@"\", true))
            {
                folderName = folderName.Replace('/', '-');
                folderName = folderName.Replace('\\', '-');
            }

            return Path.Combine(basePath, "slimCat", character, folderName);
        }
        #endregion
    }

    /// <summary>
    /// Keeps the user on the most recent page if things change and if they were on the latest page
    /// </summary>
    public class KeepToCurrentFlowDocument : IDisposable
    {
        #region fields
        bool _couldChangeBefore;
        bool _canChangeNow;
        FlowDocumentPageViewer _reader;
        #endregion

        #region constructor
        public KeepToCurrentFlowDocument(FlowDocumentPageViewer reader)
        {
            _reader = reader;

            _couldChangeBefore = _reader.CanGoToNextPage;

            _reader.Document.DocumentPaginator.PagesChanged += KeepToBottom;
        }
        #endregion

        #region methods
        public void KeepToBottom(object sender, PagesChangedEventArgs e)
        {
            _canChangeNow = _reader.CanGoToNextPage;
            if (_canChangeNow && !_couldChangeBefore)
                _reader.NextPage();

            _couldChangeBefore = _canChangeNow;
        }
        #endregion

        #region IDispose
        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool managed)
        {
            if (managed)
            {
                _reader.Document.DocumentPaginator.PagesChanged -= KeepToBottom;
                _reader = null;
            }
        }
        #endregion
    }

    /// <summary>
    /// Caches some int and fetches a new one every so often, displaying how this int has changed
    /// </summary>
    public class CacheCount : IDisposable
    {
        #region fields
        private int _oldCount;
        private int _newCount;
        private Func<int> _getNewCount;
        private Timers.Timer _updateTick;
        private IList<int> _oldCounts;
        private bool _intialized = false;
        private int _updateRes;
        #endregion

        #region constructors
        /// <summary>
        /// creates a new cached count of something
        /// </summary>
        /// <param name="label">label to display with the output</param>
        /// <param name="getNewCount">function used to get the new count</param>
        /// <param name="updateResolution">how often, in seconds, it is updated</param>
        public CacheCount(Func<int> getNewCount, int updateResolution)
        {
            _oldCounts = new List<int>();
            _getNewCount = getNewCount;
            _updateRes = updateResolution;

            _updateTick = new Timers.Timer(updateResolution * 1000);
            _updateTick.Elapsed += (s, e) => Update();
            _updateTick.Start();
        }
        #endregion

        #region methods
        public void Update()
        {
            if (_intialized)
            {
                _oldCount = _newCount;

                //60/updateres*30 returns how many update resolutions fit in 30 minutes
                if (_oldCounts.Count > ((60/_updateRes)*30))
                    _oldCounts.RemoveAt(0);
                _oldCounts.Add(_oldCount);

                _newCount = _getNewCount();
            }

            else
            {
                _oldCount = _newCount = _getNewCount();

                if (!(_oldCount == 0 || _newCount == 0))
                    _intialized = true;
            }
        }

        /// <summary>
        /// returns the average of the cached values
        /// </summary>
        public double Average()
        {
            return _oldCounts.Average();
        }

        /// <summary>
        /// returns the adjusted standard deviation for the cached values
        /// </summary>
        public double StdDev()
        {
            var squares = _oldCounts.Select(x => Math.Pow((x - Average()), 2)); // this is the squared distance from average
            return Math.Sqrt(squares.Sum() / (squares.Count() > 1 ? squares.Count()-1 : squares.Count())); // calculates population std dev from our sample
        }

        /// <summary>
        /// returns a measure of how stable the values are
        /// </summary>
        public double StabilityIndex()
        { 
            var threshold = Average()/10; // standard deviations above this are considered unstable
            // in this case, an average distance of 20% from our average is considered high

            return Math.Max(Math.Min((Math.Log10(threshold/StdDev())*100), 100), 0);
            // this scary looking thing just ensures that this value is in between 0 and 100
            // and becomes exponentially closer to 0 as the standard deviation approaches the threshold
        }

        public virtual string GetDisplayString()
        {
            if (!_intialized)
                return "";

            var change = _newCount - _oldCount;

            StringBuilder toReturn = new StringBuilder();
            if (change != 0)
            {
                toReturn.Append("Δ=");
                toReturn.Append(change);
            }

            if (_oldCounts.Count > 0)
            {
                if (Average() != 0)
                {
                    toReturn.Append(" μ=");
                    toReturn.Append(String.Format("{0:0}", Average()));
                }

                if (StdDev() != 0)
                {
                    toReturn.Append(" σ=");
                    toReturn.Append(String.Format("{0:0.##}", StdDev()));
                }

                toReturn.Append(String.Format(" Stability: {0:0.##}%", StabilityIndex()));
            }

            return toReturn.ToString().Trim();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _updateTick.Stop();
                _updateTick.Dispose();
                _updateTick = null;

                _getNewCount = null;
                _oldCounts.Clear();
                _oldCounts = null;
            }
        }
        #endregion
    }
}
