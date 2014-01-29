#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionPair.cs">
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

    using System;
    using System.Collections.Generic;

    #endregion

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
            return List.Contains(name) && OnlineList.Add(name);
        }

        public bool Remove(string name)
        {
            OnlineList.Remove(name);
            return List.Remove(name);
        }
    }
}