#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationSettings.cs">
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
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Utilities;

    #endregion

    /// <summary>
    ///     Settings for the entire application
    /// </summary>
    public static class ApplicationSettings
    {
        #region Constructors and Destructors
        static ApplicationSettings()
        {
            Volume = 1;
            ShowNotificationsGlobal = true;
            AllowLogging = true;
            AllowAdDedup = true;

            GlobalNotifyTerms = string.Empty;
            SavedChannels = new List<string>();
            Interested = new List<string>();
            NotInterested = new List<string>();
            IgnoreUpdates = new List<string>();
            RecentChannels = new List<string>(10);
            RecentCharacters = new List<string>(20);

            PortableMode = Environment.GetCommandLineArgs().Contains("portable", StringComparer.OrdinalIgnoreCase);
            FontSize = 13;
            GenderColorSettings = GenderColorSettings.GenderOnly;

            Langauge = Thread.CurrentThread.CurrentCulture.Name;
            if (!LanguageList.Contains(Langauge))
                Langauge = "en";

            SpellCheckEnabled = true;
            FriendsAreAccountWide = true;

            AutoAwayTime = 60;
            AutoIdleTime = 30;

            AllowAutoIdle = true;
            AllowStatusAutoReset = true;
            AllowAutoBusy = true;

            AllowMinimizeToTray = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets a value indicating whether allow logging.
        /// </summary>
        public static bool AllowLogging { get; set; }

        /// <summary>
        ///     Gets or sets a value indiciating whether to allow user-inputted colors to be displayed.
        /// </summary>
        public static bool AllowColors { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to allow automatic idle status update.
        /// </summary>
        public static bool AllowAutoIdle { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to allow automatic away status update.
        /// </summary>
        public static bool AllowAutoAway { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to allow idle and away to be automatically reset on activity.
        /// </summary>
        public static bool AllowStatusAutoReset { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to allow busy to be set automatically when the user is in a full-screen
        ///     application.
        /// </summary>
        public static bool AllowAutoBusy { get; set; }

        /// <summary>
        ///     Gets or sets the time before a status is automatically set to idle.
        /// </summary>
        public static int AutoIdleTime { get; set; }

        /// <summary>
        ///     Gets or sets the time before a status is automatically set to away.
        /// </summary>
        public static int AutoAwayTime { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to allow ad deduplication.
        /// </summary>
        public static bool AllowAdDedup { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to allow the client to minimize to tray on close.
        /// </summary>
        public static bool AllowMinimizeToTray { get; set; }

        /// <summary>
        ///     Gets or sets the global notify terms.
        /// </summary>
        public static string GlobalNotifyTerms { get; set; }

        /// <summary>
        ///     Gets the global notify terms list.
        /// </summary>
        public static IEnumerable<string> GlobalNotifyTermsList
        {
            get { return GlobalNotifyTerms.Split(',').Select(word => word.ToLower()); }
        }

        public static bool FriendsAreAccountWide { get; set; }

        public static int FontSize { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether show notifications global.
        /// </summary>
        public static bool ShowNotificationsGlobal { get; set; }

        public static bool PortableMode { get; set; }

        /// <summary>
        ///     Gets or sets the volume.
        /// </summary>
        public static double Volume { get; set; }

        /// <summary>
        ///     Gets the list of characters interesting to this user.
        /// </summary>
        public static IList<string> Interested { get; private set; }

        /// <summary>
        ///     Gets or sets the recent characters.
        /// </summary>
        public static IList<string> RecentCharacters { get; private set; }

        /// <summary>
        ///     Gets or sets the recent channels.
        /// </summary>
        public static IList<string> RecentChannels { get; private set; }

        /// <summary>
        ///     Gets the list of channels saved to our user .
        /// </summary>
        public static IList<string> SavedChannels { get; private set; }

        /// <summary>
        ///     Gets the list of characters not interesting to this user.
        /// </summary>
        public static IList<string> NotInterested { get; private set; }

        // Localization

        /// <summary>
        ///     Gets or sets a value indicate whether to spell check the users' input.
        /// </summary>
        public static bool SpellCheckEnabled { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating the users' language.
        /// </summary>
        public static string Langauge { get; set; }

        public static IEnumerable<string> LanguageList
        {
            get { return new[] {"en-US", "en-GB", "de", "es", "fr"}; }
        }

        /// <summary>
        ///     Gets or sets the gender color settings.
        /// </summary>
        public static GenderColorSettings GenderColorSettings { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether play sound even when the current tab is focused.
        /// </summary>
        public static bool PlaySoundEvenWhenTabIsFocused { get; set; }

        public static IList<string> IgnoreUpdates { get; set; }

        #endregion
    }

    public enum GenderColorSettings
    {
        /// <summary>
        ///     No characters are colored by gender.
        /// </summary>
        None,

        /// <summary>
        ///     Non gender options are coerced to genders, then colored.
        ///     Shemale, HermF, and Female are 'female'-colored.
        ///     Cuntboy, HermM, and Male are 'male'-colored.
        ///     Transgender and None are the default theme color.
        /// </summary>
        GenderOnly,

        /// <summary>
        ///     Male, Female, HermF, and HermM are colored.
        ///     Shemale and Cuntboy are coerced to gender then colored.
        ///     Transgender and none are the default theme color.
        /// </summary>
        GenderAndHerm,

        /// <summary>
        ///     All genders are colored uniquely.
        /// </summary>
        Full
    }
}