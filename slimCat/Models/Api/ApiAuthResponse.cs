#region Copyright

// <copyright file="ApiAuthResponse.cs">
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

namespace slimCat.Models.Api
{
    #region Usings

    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Services;

    #endregion

    [DataContract]
    public class ApiAuthResponse : IHaveAnErrorMaybe
    {
        [DataMember(Name = "ticket")]
        public string Ticket { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "default_character")]
        public string DefaultCharacter { get; set; }

        [DataMember(Name = "characters")]
        public IList<string> Characters { get; set; }

        [DataMember(Name = "bookmarks")]
        public IList<Bookmark> Bookmarks { get; set; }

        [DataMember(Name = "friends")]
        public IList<Friend> Friends { get; set; }
    }

    [DataContract]
    public class Friend
    {
        [DataMember(Name = "source_name")]
        public string From { get; set; }

        [DataMember(Name = "dest_name")]
        public string To { get; set; }
    }

    [DataContract]
    public class Bookmark
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}