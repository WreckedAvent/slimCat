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

        public void Add(string name)
        {
            List.Add(name);
            OnlineList.Add(name);
        }

        public void Set(IEnumerable<string> collection)
        {
            List.Clear();
            List.UnionWith(collection);
        }

        public void SignOff(string name)
        {
            OnlineList.Remove(name);
        }

        public void SignOn(string name)
        {
            if (List.Contains(name))
                OnlineList.Add(name);
        }

        public void Remove(string name)
        {
            List.Remove(name);
            OnlineList.Remove(name);
        }
    }
}