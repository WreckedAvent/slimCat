#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionPair.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Collections.Generic;

    #endregion

    public class CollectionPair
    {
        public CollectionPair()
        {
            List = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            OnlineList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            TemporaryAdd = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            TemporaryRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public HashSet<string> List { get; private set; }

        public HashSet<string> OnlineList { get; private set; }

        private HashSet<string> TemporaryAdd { get; set; }

        private HashSet<string> TemporaryRemove { get; set; }

        public bool Add(string name, bool isTemporary = false)
        {
            OnlineList.Add(name);

            return isTemporary
                ? TemporaryRemove.Remove(name) || TemporaryAdd.Add(name)
                : List.Add(name);
        }

        public void Set(IEnumerable<string> collection)
        {
            List.Clear();
            OnlineList.Clear();

            if (collection != null)
                List.UnionWith(collection);
        }

        public bool SignOff(string name)
        {
            return OnlineList.Remove(name);
        }

        public bool SignOn(string name)
        {
            return IsOnList(name)
                   && OnlineList.Add(name);
        }

        public bool Remove(string name, bool isTemporary = false)
        {
            OnlineList.Remove(name);

            return isTemporary
                ? TemporaryAdd.Remove(name) || TemporaryRemove.Add(name)
                : List.Remove(name);
        }

        public bool IsOnList(string name, bool onlineOnly = false)
        {
            var isOnList = onlineOnly
                ? OnlineList.Contains(name)
                : List.Contains(name) || TemporaryAdd.Contains(name);

            return isOnList && !TemporaryRemove.Contains(name);
        }
    }
}