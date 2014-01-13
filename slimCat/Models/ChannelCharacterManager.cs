namespace Slimcat.Models
{
    using System.Collections.Generic;

    public class ChannelCharacterManager : CharacterManagerBase
    {
        private readonly CollectionPair moderators = new CollectionPair();
        private readonly CollectionPair banned = new CollectionPair();

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

        public override bool IsOfInterest(string name)
        {
            return false;
        }
    }
}
