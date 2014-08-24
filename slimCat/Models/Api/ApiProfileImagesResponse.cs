#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApiProfileImagesResponse.cs">
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

namespace slimCat.Models.Api
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class ApiProfileImagesResponse
    {
        [DataMember(Name = "images")]
        public IList<ApiProfileImage> Images { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    [DataContract]
    public class ApiProfileImage
    {
        [DataMember(Name = "image_id")]
        public string ImageId { get; set; }

        [DataMember(Name = "extension")]
        public string Extension { get; set; }

        [DataMember(Name = "width")]
        public string Width { get; set; }

        [DataMember(Name = "height")]
        public string Height { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }
    }
}