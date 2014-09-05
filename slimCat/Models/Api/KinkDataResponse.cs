#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KinkDataResponse.cs">
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

namespace slimCat.Models.Api
{
    #region Usings

    using System.Collections.Generic;
    using System.Runtime.Serialization;

    #endregion

    [DataContract]
    public class ApiKinkDataResponse
    {
        [DataMember(Name = "kinks")]
        public IDictionary<string, ApiKinkGroup> Kinks { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    [DataContract]
    public class ApiKinkGroup
    {
        [DataMember(Name = "group")]
        public string Group { get; set; }

        [DataMember(Name = "items")]
        public IList<ApiKink> Kinks { get; set; }
    }

    [DataContract]
    public class ApiKink
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "kink_id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}