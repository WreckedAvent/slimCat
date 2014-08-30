#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfileKink.cs">
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

namespace slimCat.Models
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    [XmlInclude(typeof(KinkListKind))]
    [XmlRoot(ElementName = "Kink")]
    public class ProfileKink
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsCustomKink { get; set; }

        public KinkListKind KinkListKind { get; set; }
    }

    public enum KinkListKind
    {
        Fave,
        Yes,
        Maybe,
        No
    }
}