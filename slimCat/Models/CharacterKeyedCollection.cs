#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterKeyedCollection.cs">
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

    using System.Collections.Generic;
    using lib;

    #endregion

    public class CharacterKeyedCollection : ObservableKeyedCollection<string, ICharacter>
    {
        public CharacterKeyedCollection()
        {
        }

        public CharacterKeyedCollection(IEqualityComparer<string> comparer) : base(comparer)
        {
        }

        protected override string GetKeyForItem(ICharacter item)
        {
            return item.Name;
        }
    }
}