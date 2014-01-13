namespace Slimcat.Models
{
    using System.Collections.Generic;

    public class GlobalCharacterManager : CharacterManagerBase
    {
        private readonly CollectionPair bookmarks = new CollectionPair();
        private readonly CollectionPair friends = new CollectionPair();
        private readonly CollectionPair moderators = new CollectionPair();
        private readonly CollectionPair interested = new CollectionPair();
        private readonly CollectionPair notInterested = new CollectionPair();
        private readonly CollectionPair ignored = new CollectionPair();

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
