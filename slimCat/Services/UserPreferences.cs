#region Copyright

// <copyright file="UserPreferences.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Xml.Serialization;

    #endregion

    [Serializable]
    // What is the difference between preferences and settings?
    // Preferences are application-wide. Also, not a freaking static class
    public class UserPreferences
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }

        [XmlIgnore]
        public bool IsPortable { get; set; }

        public bool SaveLogin { get; set; }
        public bool ShowStillRunning { get; set; }

        [XmlIgnore]
        public string BasePath { get; set; }

        public bool IsAdvanced { get; set; }
        public string Version { get; set; }
    }
}