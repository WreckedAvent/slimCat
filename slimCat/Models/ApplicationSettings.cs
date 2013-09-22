// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   Gender Settings Model provides basic settings for a gender filter
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Models
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Settings for the entire application
    /// </summary>
    public static class ApplicationSettings
    {
        #region Static Fields

        public static readonly IList<string> Interested;

        public static readonly IList<string> SavedChannels;

        public static readonly IList<string> NotInterested;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes static members of the <see cref="ApplicationSettings" /> class.
        /// </summary>
        static ApplicationSettings()
        {
            Volume = 0.5;
            ShowNotificationsGlobal = true;
            AllowLogging = true;

            BackLogMax = 300;
            GlobalNotifyTerms = string.Empty;
            SavedChannels = new List<string>();
            Interested = new List<string>();
            NotInterested = new List<string>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets a value indicating whether allow logging.
        /// </summary>
        public static bool AllowLogging { get; set; }

        /// <summary>
        ///     Gets or sets the back log max.
        /// </summary>
        public static int BackLogMax { get; set; }

        /// <summary>
        ///     Gets or sets the global notify terms.
        /// </summary>
        public static string GlobalNotifyTerms { get; set; }

        /// <summary>
        ///     Gets the global notify terms list.
        /// </summary>
        public static IEnumerable<string> GlobalNotifyTermsList
        {
            get
            {
                return GlobalNotifyTerms.Split(',').Select(word => word.ToLower());
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show notifications global.
        /// </summary>
        public static bool ShowNotificationsGlobal { get; set; }

        /// <summary>
        ///     Gets or sets the volume.
        /// </summary>
        public static double Volume { get; set; }

        #endregion
    }
}