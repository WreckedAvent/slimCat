﻿#region Copyright

// <copyright file="ChannelCharacterManager.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System.Collections.Generic;
    using Utilities;

    #endregion

    public class ChannelCharacterManager : CharacterManagerBase
    {
        private readonly CollectionPair banned = new CollectionPair();
        private readonly CollectionPair moderators = new CollectionPair();

        public ChannelCharacterManager()
        {
            Collections = new HashSet<CollectionPair>
            {
                moderators,
                banned
            };

            CollectionDictionary = new Dictionary<ListKind, CollectionPair>
            {
                {ListKind.Moderator, moderators},
                {ListKind.Banned, banned}
            };
        }

        public override bool IsOfInterest(string name, bool onlineOnly = true)
        {
            return false;
        }

        public override bool IsIgnored(string name, bool onlineOnly = true)
        {
            return false;
        }

        public override void Clear()
        {
            Collections.Each(x => x.Set(new List<string>()));
            base.Clear();
        }
    }
}