#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StaticFunctions.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

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
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Newtonsoft.Json;
    using Services;
    using ViewModels;

    #endregion

    /// <summary>
    ///     The static functions.
    /// </summary>
    public static class StaticFunctions
    {
        public static bool CharacterIsInList(this ICollection<ICharacter> collection, ICharacter toFind)
        {
            return collection.Any(character => character.Name.Equals(toFind.Name, StringComparison.OrdinalIgnoreCase));
        }


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
            return dingTerms.Any(term => FirstMatch(checkAgainst, term).Item1 != string.Empty);
        }


        public static bool IsDingMessage(
            this IMessage message, ChannelSettingsModel settings, IEnumerable<string> dingTerms)
        {
            var safeMessage = HttpUtility.HtmlDecode(message.Message);

            if (!settings.NotifyIncludesCharacterNames) return safeMessage.HasDingTermMatch(dingTerms);

            var enumeratedDingTerm = dingTerms as string[] ?? dingTerms.ToArray();
            return message.Poster.Name.HasDingTermMatch(enumeratedDingTerm)
                   || safeMessage.HasDingTermMatch(enumeratedDingTerm);
        }


        public static string MakeSafeFolderPath(string character, string title, string id)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
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

        public static bool NameContains(this ICharacter character, string searchString)
        {
            return character.Name.ToLower().Contains(searchString.ToLower());
        }

        public static bool NameEquals(this ICharacter character, string compare)
        {
            return character.Name.Equals(compare, StringComparison.OrdinalIgnoreCase);
        }

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


        public static T DeserializeTo<T>(this string objectString)
        {
            return JsonConvert.DeserializeObject<T>(objectString);
        }

        public static void NewCharacterUpdate(this IEventAggregator events, ICharacter character,
            CharacterUpdateEventArgs e)
        {
            events.GetEvent<NewUpdateEvent>().Publish(new CharacterUpdateModel(character, e));
        }

        public static void NewChannelUpdate(this IEventAggregator events, ChannelModel channel, ChannelUpdateEventArgs e)
        {
            events.GetEvent<NewUpdateEvent>().Publish(new ChannelUpdateModel(channel, e));
        }

        public static void SaveSettings(this ViewModelBase vm)
        {
            ApplicationSettings.SettingsVersion = Constants.ClientVer;
            SettingsService.SaveApplicationSettingsToXml(vm.ChatModel.CurrentCharacter.Name);
        }

        public static bool IsUpdate(string arg)
        {
            var versionString = arg.Substring(arg.LastIndexOf(' '));
            var version = Convert.ToDouble(versionString, CultureInfo.InvariantCulture);

            var toReturn = version > Constants.Version;

            if (!toReturn && Math.Abs(version - Constants.Version) < 0.001)
                toReturn = Constants.ClientVer.Contains("dev");

            return toReturn;
        }

        public static void NotifyWithSettings(this IManageToasts toasts, NotificationModel notification,
            ChannelSettingsModel.NotifyLevel notifyLevel)
        {
            switch (notifyLevel)
            {
                case ChannelSettingsModel.NotifyLevel.NoNotification:
                    break;
                case ChannelSettingsModel.NotifyLevel.NotificationOnly:
                    toasts.AddNotification(notification);
                    break;
                case ChannelSettingsModel.NotifyLevel.NotificationAndToast:
                    toasts.AddNotification(notification);
                    toasts.FlashWindow();
                    toasts.ShowToast();
                    break;
                case ChannelSettingsModel.NotifyLevel.NotificationAndSound:
                    toasts.AddNotification(notification);
                    toasts.FlashWindow();
                    toasts.PlaySound();
                    toasts.ShowToast();
                    break;
            }
        }

        public static GeneralChannelModel GetChannelById(this IChatState chatState, string id)
        {
            return chatState.ChatModel.AllChannels.FirstByIdOrNull(id);
        }

        public static ChannelSettingsModel GetChannelSettingsById(this IChatState chatState, string id)
        {
            var channel = chatState.GetChannelById(id);

            return channel == null ? null : channel.Settings;
        }

        public static bool IsInteresting(this IChatState chatState, string character, bool onlineOnly = false)
        {
            return chatState.CharacterManager.IsOfInterest(character, onlineOnly) ||
                   chatState.ChatModel.CurrentPms.FirstByIdOrNull(character) != null;
        }
    }
}