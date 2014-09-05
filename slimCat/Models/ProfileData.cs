#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfileData.cs">
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
    using System.Xml.Serialization;

    #endregion

    [Serializable]
    [XmlInclude(typeof (ProfileKink))]
    [XmlInclude(typeof (ProfileImage))]
    [XmlInclude(typeof (ProfileTag))]
    [XmlRoot(ElementName = "Profile")]
    public class ProfileData
    {
        public string Age { get; set; }

        public string DomSubRole { get; set; }

        public string Species { get; set; }

        public string ProfileText { get; set; }

        public string Orientation { get; set; }

        public string Build { get; set; }

        public string Height { get; set; }

        public string BodyType { get; set; }

        public string Position { get; set; }

        public List<ProfileImage> Images { get; set; }

        public List<ProfileKink> Kinks { get; set; }

        [XmlAttribute]
        public DateTime LastRetrieved { get; set; }

        public List<ProfileTag> AdditionalTags { get; set; }
    }

    [Serializable]
    public class ProfileTag
    {
        [XmlAttribute]
        public string Label { get; set; }

        [XmlAttribute]
        public string Value { get; set; }
    }
}