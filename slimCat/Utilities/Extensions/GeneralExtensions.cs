#region Copyright

// <copyright file="GeneralExtensions.cs">
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
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using System.Windows;
    using System.Windows.Documents;
    using Models;
    using Services;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Extension methods for types too specific to get their own class for them.
    /// </summary>
    public static class GeneralExtensions
    {
        public static HashSet<KeyValuePair<ListKind, string>> ListKindSet = new HashSet<KeyValuePair<ListKind, string>>
        {
            new KeyValuePair<ListKind, string>(ListKind.Friend, "a"),
            new KeyValuePair<ListKind, string>(ListKind.Bookmark, "b"),
            new KeyValuePair<ListKind, string>(ListKind.Interested, "c"),
            new KeyValuePair<ListKind, string>(ListKind.Moderator, "d"),
            new KeyValuePair<ListKind, string>(ListKind.Ignored, "z"),
            new KeyValuePair<ListKind, string>(ListKind.NotInterested, "z")
        };

        public static Dictionary<StatusType, string> AlphabeticalSortDictionary = new Dictionary<StatusType, string>
        {
            {StatusType.Looking, "f"},
            {StatusType.Online, "f"},
            {StatusType.Busy, "f"},
            {StatusType.Idle, "f"},
            {StatusType.Away, "f"},
            {StatusType.Dnd, "f"},
            {StatusType.Offline, "z"}
        };

        public static Dictionary<StatusType, string> DefaultSortDictionary = new Dictionary<StatusType, string>
        {
            {StatusType.Looking, "e"},
            {StatusType.Online, "f"},
            {StatusType.Busy, "g"},
            {StatusType.Idle, "h"},
            {StatusType.Away, "i"},
            {StatusType.Dnd, "y"},
            {StatusType.Offline, "z"}
        };

        private static bool isPortableMode { get; } = Environment
            .GetCommandLineArgs()
            .Any(x => x.Equals("portable", StringComparison.OrdinalIgnoreCase));

        public static string BaseFolderPath => isPortableMode
            ? Path.Combine("logs", "")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "slimCat");

        public static Dictionary<StatusType, string> SortDictionary
            => ApplicationSettings.SortUsersAlphabetically ? AlphabeticalSortDictionary : DefaultSortDictionary;

        public static bool IsDingMessage(
            this IMessage message, ChannelSettingsModel settings, IEnumerable<string> dingTerms)
        {
            var safeMessage = HttpUtility.HtmlDecode(message.Message);

            if (!settings.NotifyIncludesCharacterNames) return safeMessage.HasDingTermMatch(dingTerms);

            var enumeratedDingTerm = dingTerms as string[] ?? dingTerms.ToArray();
            return message.Poster.Name.HasDingTermMatch(enumeratedDingTerm)
                   || safeMessage.HasDingTermMatch(enumeratedDingTerm);
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

        public static void SaveSettings(this ViewModelBase vm)
        {
            ApplicationSettings.SettingsVersion = Constants.ClientVersion;
            SettingsService.SaveApplicationSettingsToXml(vm.ChatModel.CurrentCharacter.Name);
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

        public static double GetScrollDistance(int scrollTicks, int fontSize)
        {
            var linesPerTick = SystemParameters.WheelScrollLines;
            // This solves Windows 7 wheel setting "One screen at a time"
            if (linesPerTick < 1)
                linesPerTick = 10;

            // An estimation for the height of each line
            var lineSize = (fontSize*0.89d) + 9.72d;

            // 120 is standard for one mousewheel tick
            return ((scrollTicks/120.0d)*linesPerTick*lineSize);
        }

        public static void FireAndForget(this Task task)
        {
        }

        /// <summary>
        ///     Converts a POSIX/UNIX timecode to a <see cref="System.DateTimeOffset" />.
        /// </summary>
        public static DateTimeOffset UnixTimeToDateTime(this long time)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return new DateTimeOffset(epoch.AddSeconds(time));
        }

        #region Public Methods and Operators

        /// <summary>
        ///     Removes an item at a specific index for a block collection.
        /// </summary>
        public static void RemoveAt(this BlockCollection collection, int index)
        {
            if (index == -1 || collection.Count == 0)
                return;

            collection.Remove(collection.ElementAt(index));
        }

        /// <summary>
        ///     Adds an item at a specific index for a block collection.
        /// </summary>
        public static void AddAt(this BlockCollection collection, int index, Block item)
        {
            if (index == -1)
                return;

            if (collection.Count == 0)
            {
                collection.Add(item);
                return;
            }

            index = Math.Min(index, collection.Count - 1);

            collection.InsertAfter(collection.ElementAt(index), item);
        }

        #endregion
    }
}