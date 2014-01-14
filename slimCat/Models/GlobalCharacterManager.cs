#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalCharacterManager.cs">
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

namespace Slimcat.Models
{
    #region Usings

    using System.Collections.Generic;

    #endregion

    public class GlobalCharacterManager : CharacterManagerBase
    {
        private readonly CollectionPair bookmarks = new CollectionPair();
        private readonly CollectionPair friends = new CollectionPair();
        private readonly CollectionPair ignored = new CollectionPair();
        private readonly CollectionPair interested = new CollectionPair();
        private readonly CollectionPair moderators = new CollectionPair();
        private readonly CollectionPair notInterested = new CollectionPair();

        public GlobalCharacterManager()
        {
            Collections = new HashSet<CollectionPair>
                {
                    bookmarks,
                    friends,
                    moderators,
                    interested,
                    notInterested,
                    ignored
                };

            CollectionDictionary = new Dictionary<ListKind, CollectionPair>
                {
                    {ListKind.Bookmark, bookmarks},
                    {ListKind.Friend, friends},
                    {ListKind.Interested, interested},
                    {ListKind.Moderator, moderators},
                    {ListKind.NotInterested, notInterested},
                    {ListKind.Ignored, ignored}
                };

            OfInterestCollections = new HashSet<CollectionPair>
                {
                    bookmarks,
                    friends,
                    interested
                };
        }

        public override bool IsOfInterest(string name)
        {
            lock (Locker)
            {
                var isOfInterest = false;
                foreach (var list in OfInterestCollections)
                {
                    isOfInterest = list.OnlineList.Contains(name);
                    if (isOfInterest) break;
                }

                return isOfInterest;
            }
        }
    }
}