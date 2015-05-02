#region Copyright

// <copyright file="StringExtensions.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;

    #endregion

    public static class StringExtensions
    {
        public static bool ContainsOrdinal(this string fullString, string checkterm, bool ignoreCase = true)
        {
            var comparer = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return fullString.IndexOf(checkterm, comparer) >= 0;
        }

        public static Tuple<string, string> FirstMatch(this string fullString, string checkAgainst)
        {
            var startIndex = fullString.IndexOf(checkAgainst, StringComparison.OrdinalIgnoreCase);

            var hasMatch = false;

            if (startIndex == -1)
                return new Tuple<string, string>(string.Empty, string.Empty);

            // there is a subtle issue somewhere in here
            // where the string we're checking against can become empty
            // after we've done the check that our indexing is within bounds
            // creating an out-of-bounds exception.
            // this is a fix to resolve the symptom of crashing until the
            // underlying problem can be found
            try
            {
                // this checks for if the match is a whole word
                if (startIndex > 0 && startIndex + 1 < fullString.Length)
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
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }

            if (checkAgainst.Length == fullString.Length &&
                checkAgainst.Equals(fullString, StringComparison.OrdinalIgnoreCase))
                return new Tuple<string, string>(checkAgainst, checkAgainst);

            return hasMatch
                ? new Tuple<string, string>(checkAgainst, GetStringContext(fullString, checkAgainst))
                : new Tuple<string, string>(string.Empty, string.Empty);
        }

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

        public static bool HasDingTermMatch(this string checkAgainst, IEnumerable<string> dingTerms)
        {
            return dingTerms.Any(term => term.FirstMatch(checkAgainst).Item1 != string.Empty);
        }

        public static string MakeSafeFolderPath(string character, string title, string id)
        {
            string folderName;

            if (!title.Equals(id))
            {
                var safeTitle =
                    Path.GetInvalidPathChars()
                        .Union(Path.GetInvalidFileNameChars())
                        .Union(new[] {'/', '\\'})
                        .Aggregate(title,
                            (current, c) => current.Replace(c.ToString(CultureInfo.InvariantCulture), string.Empty));

                if (safeTitle[0].Equals('.'))
                    safeTitle = safeTitle.Remove(0, 1);

                folderName = $"{safeTitle} ({id})";
            }
            else
                folderName = id;

            if (folderName.ContainsOrdinal(@"/") || folderName.ContainsOrdinal(@"\"))
            {
                folderName = folderName.Replace('/', '-');
                folderName = folderName.Replace('\\', '-');
            }

            return Path.Combine(GeneralExtensions.BaseFolderPath, character, folderName);
        }

        /// <summary>
        ///     Uses a string as a format provider with the given arguments.
        /// </summary>
        public static string FormatWith(this string toFormat, params object[] args)
        {
            return string.Format(toFormat, args);
        }

        public static T ToEnum<T>(this string str)
        {
            return (T) Enum.Parse(typeof (T), str, true);
        }

        public static string StripPunctuationAtEnd(this string fullString)
        {
            if (string.IsNullOrWhiteSpace(fullString) || fullString.Length <= 1)
                return fullString;

            var index = fullString.Length - 1;

            while (char.IsPunctuation(fullString[index]) && index != 0)
            {
                index--;
            }

            return index == 0 ? string.Empty : fullString.Substring(0, index + 1);
        }

        public static T DeserializeTo<T>(this string objectString)
        {
            return JsonConvert.DeserializeObject<T>(objectString);
        }

        public static bool IsUpdate(string arg)
        {
            var versionString = arg.Substring(arg.LastIndexOf(' '));
            var version = Convert.ToDouble(versionString, CultureInfo.InvariantCulture);

            var toReturn = version > Constants.Version;

            if (!toReturn && Math.Abs(version - Constants.Version) < 0.001)
                toReturn = Constants.ClientVersion.Contains("dev");

            return toReturn;
        }
    }
}