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
    using Api;
    using Utilities;

    public class ProfileImage
    {
        public const string ThumbUrl = Constants.UrlConstants.StaticDomain + "/images/charthumb/{0}.{1}";

        public const string FullImageUrl = Constants.UrlConstants.StaticDomain + "/images/charimage/{0}.{1}";

        public ProfileImage(ApiProfileImage image)
        {
            ThumbnailUri = new Uri(ThumbUrl.FormatWith(image.ImageId, image.Extension));
            FullImageUri = new Uri(FullImageUrl.FormatWith(image.ImageId, image.Extension));
            Description = image.Description;
        }

        public Uri ThumbnailUri { get; set; }

        public Uri FullImageUri { get; set; }

        public string Description { get; set; }
    }
}