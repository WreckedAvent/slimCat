#region Copyright

// <copyright file="ProfileKink.cs">
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

    using System;
    using System.Xml.Serialization;

    #endregion

    [Serializable]
    [XmlInclude(typeof (KinkListKind))]
    [XmlRoot(ElementName = "Kink")]
    public class ProfileKink
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool IsCustomKink { get; set; }

        [XmlAttribute]
        public KinkListKind KinkListKind { get; set; }

        [XmlAttribute]
        public string Tooltip { get; set; }

        public bool ShouldSerializedName()
        {
            return IsCustomKink || KinkListKind == KinkListKind.MasterList;
        }

        public bool ShouldSerializeIsCustomKink()
        {
            return IsCustomKink;
        }

        public bool ShouldSerializeTooltip()
        {
            return IsCustomKink || KinkListKind == KinkListKind.MasterList;
        }
    }

    public enum KinkListKind
    {
        Fave,
        Yes,
        Maybe,
        No,
        MasterList
    }
}