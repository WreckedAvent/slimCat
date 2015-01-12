#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchTermModel.cs">
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
    [XmlRoot("SearchTerms")]
    public class SearchTermModel
    {
        [XmlAttribute]
        public string DisplayName { get; set; }

        [XmlAttribute]
        public string Category { get; set; }

        [XmlAttribute]
        public string UnderlyingValue { get; set; }
    }

    [Serializable]
    [XmlInclude(typeof(SearchTermModel))]
    public class SearchTermsModel
    {
        public List<SearchTermModel> SelectedTerms { get; set; }

        public List<SearchTermModel> AvailableTerms { get; set; }
    }
}