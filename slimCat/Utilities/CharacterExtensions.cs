#region Copyright

// <copyright file="CharacterExtensions.cs">
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
    using System.Linq;
    using Models;

    #endregion

    public static class CharacterExtensions
    {
        public static bool NameEquals(this ICharacter character, string compare)
            => character.Name.Equals(compare, StringComparison.OrdinalIgnoreCase);

        public static bool NameContains(this ICharacter character, string searchString)
            => character.Name.ToLower().Contains(searchString.ToLower());

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
            foreach (var pair in map.Where(pair => cm.IsOnList(name, pair.Key, false)))
                return pair.Value;

            if (channel == null) return search.MeetsStatusFilter(character);

            return channel.CharacterManager.IsOnList(name, ListKind.Moderator, false)
                ? search.ShowMods
                : search.MeetsStatusFilter(character);
        }

        public static string RelationshipToUser(this ICharacter character, ICharacterManager cm,
            GeneralChannelModel channel)
        {
            foreach (var pair in GeneralExtensions.ListKindSet.Where(pair => cm.IsOnList(character.Name, pair.Key)))
                return pair.Value;

            if (channel != null && channel.CharacterManager.IsOnList(character.Name, ListKind.Moderator))
                return "d";

            string result;
            return GeneralExtensions.SortDictionary.TryGetValue(character.Status, out result)
                ? result
                : "f";
        }
    }
}