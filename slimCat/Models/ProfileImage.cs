#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfileImage.cs">
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
    using System.Net;
    using System.Xml.Serialization;
    using Api;
    using Utilities;

    [Serializable]
    [XmlRoot(ElementName = "Image")]
    public class ProfileImage
    {
        public const string ThumbUrl = Constants.UrlConstants.StaticDomain + "/images/charthumb/{0}.{1}";

        public const string FullImageUrl = Constants.UrlConstants.StaticDomain + "/images/charimage/{0}.{1}";

        public ProfileImage(ApiProfileImage image)
        {
            ThumbnailUri = new Uri(ThumbUrl.FormatWith(image.ImageId, image.Extension));
            FullImageUri = new Uri(FullImageUrl.FormatWith(image.ImageId, image.Extension));
            Description = WebUtility.HtmlDecode(WebUtility.HtmlDecode(image.Description));
            Width = image.Width;
            Height = image.Height;
        }

        public ProfileImage()
        {
            
        }

        [XmlIgnore]
        public Uri ThumbnailUri { get; set; }

        [XmlAttribute("ThumbnailUri")]
        public string ThumbnailString
        {
            get { return ThumbnailUri.AbsolutePath; }
            set { ThumbnailUri = new Uri(Constants.UrlConstants.StaticDomain + value, UriKind.Absolute);}
        }

        [XmlIgnore]
        public Uri FullImageUri { get; set; }

        [XmlAttribute("FullImageUri")]
        public string FullImageString
        {
            get { return FullImageUri.AbsolutePath; }
            set { FullImageUri = new Uri(Constants.UrlConstants.StaticDomain + value, UriKind.Absolute); }
        }

        [XmlAttribute]
        public string Description { get; set; }

        [XmlAttribute]
        public string Width { get; set; }

        [XmlAttribute]
        public string Height { get; set; }
    }
}