using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static bool MeetsChatModelLists(this ICharacter character, GenericSearchSettingsModel search, IChatModel cm, GeneralChannelModel channel)
        {
            // notice the toListing, this is an attempt to fix EnumerationChanged errors
            if (search.ShowIgnored && cm.Ignored.ToList().Contains(character.Name)) return true;
            if (search.ShowMods && cm.Mods.ToList().Contains(character.Name)) return true;
            if (channel != null)
                if (search.ShowMods && channel.Moderators.ToList().Contains(character.Name)) return true;
            if (search.ShowFriends && cm.Friends.ToList().Contains(character.Name)) return true;
            if (search.ShowBookmarks && cm.Bookmarks.ToList().Contains(character.Name)) return true;
            
            if (search.MeetsStatusFilter(character)) return true;
            return false;
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

        #region misc.
        /// <summary>
        /// returns the sentence around a word
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

        public static string GetStringContextOld(string fullContent, string specificWord)
        {
            const int contextLength = 150;
            var trippedItIndex = fullContent.ToLower().IndexOf(specificWord);

            if (trippedItIndex == -1)
                return string.Empty; // derp

            var startIndex = trippedItIndex;

            while (
                (!char.IsPunctuation(fullContent[startIndex]) || fullContent[startIndex] == '\'') 
                && startIndex != 0 
                && (trippedItIndex-startIndex < contextLength)
            )
                startIndex--; // pack-peddle until we find us our start, but we only want to go back so many words


            if (startIndex != 0)
            {
                if (!char.IsPunctuation(fullContent[startIndex]))
                {
                    while (!char.IsWhiteSpace(fullContent[startIndex]))
                        startIndex++;
                }
                    startIndex++; // offset the punctuation
                    while (char.IsWhiteSpace(fullContent[startIndex]))
                        startIndex++; // get the nex character
            }

            // now we have the start of our sentence, let's find a suitable end

            var workingString = fullContent.Substring(startIndex);

            if (workingString.Length < 50)
                return (startIndex == 0 ? "" : "... ") + workingString + " ...";

            var endIndex = workingString.IndexOf(','); // try to get a comma first

            if (endIndex == -1)
            {
                endIndex = workingString.IndexOf('.'); // then go for a period
                if (endIndex == -1)
                {
                    if (startIndex == 0)
                    {
                        endIndex = Math.Min(contextLength, workingString.Length);
                        while (endIndex < workingString.Length && !char.IsWhiteSpace(workingString[endIndex]))
                            endIndex++;
                    }
                    else
                        endIndex = workingString.Length; // just show the whole thing
                }
            }

            if (endIndex != workingString.Length)
                endIndex++; // include that punctuation we grabbed

            if (startIndex == 0)
                return workingString.Substring(0, endIndex).Trim() + (endIndex != workingString.Length ? " ..." : "");

            else
            {
                var startOffset = (Math.Max(0, endIndex - contextLength)); // only show the 50 characters around it
                return "... " + workingString.Substring(startOffset, endIndex - startOffset).Trim() + " ...";
            }
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
