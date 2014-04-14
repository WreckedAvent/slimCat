#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApiAuthResponse.cs">
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
    using System.Runtime.Serialization;

    [DataContract]
    public class ApiUploadLogResponse
    {
        [DataMember(Name = "log_id")]
        public string LogId { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }
    }
}
