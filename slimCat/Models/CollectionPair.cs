namespace Slimcat.Models
{
    using System;
    using System.Collections.Generic;

    public class CollectionPair
    {
        public CollectionPair()
        {
            List = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            OnlineList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public HashSet<string> List { get; private set; }
        public HashSet<string> OnlineList { get; private set; }

        public bool Add(string name)
        {
            OnlineList.Add(name);
            return List.Add(name);
        }

        public void Set(IEnumerable<string> collection)
        {
            List.Clear();
            List.UnionWith(collection);
        }

        public bool SignOff(string name)
        {
            return OnlineList.Remove(name);
        }

        public bool SignOn(string name)
        {
            return List.Contains(name) && OnlineList.Add(name);
        }

        public bool Remove(string name)
        {
            OnlineList.Remove(name);
            return List.Remove(name);
        }
    }
}