#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GenericSearchSettingsModel.cs">
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
    using System.Windows.Input;
    using Libraries;
    using Utilities;

    #endregion

    /// <summary>
    ///     Search settings to be used with a search text box tool chain
    /// </summary>
    public class GenericSearchSettingsModel
    {
        #region Fields

        private RelayCommand expandSearch;

        private bool isChangingSettings;

        private string search = string.Empty;

        private bool showBookmarks = true;

        private bool showBusyAway = true;

        private bool showDnd = true;

        private bool showFriends = true;

        private bool showIgnored;

        private bool showLooking = true;

        private bool showMods = true;

        private bool showNormal = true;

        private bool showNotInterested;

        #endregion

        #region Public Events

        /// <summary>
        ///     Called when our search settings are updated
        /// </summary>
        public event EventHandler Updated;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets a value indicating whether is changing settings.
        /// </summary>
        public bool IsChangingSettings
        {
            get { return isChangingSettings; }

            set
            {
                if (isChangingSettings == value)
                    return;

                Log(value ? "showing modal" : "hiding modal");
                isChangingSettings = value;
                CallUpdate();
            }
        }

        public bool ShowOffline { get; set; }

        /// <summary>
        ///     Gets the open search settings command.
        /// </summary>
        public ICommand OpenSearchSettingsCommand
        {
            get
            {
                return expandSearch
                       ?? (expandSearch =
                           new RelayCommand(param => IsChangingSettings = !IsChangingSettings));
            }
        }

        /// <summary>
        ///     Gets or sets the search string.
        /// </summary>
        public string SearchString
        {
            get { return search?.ToLower() ?? search; }

            set
            {
                if (search == value)
                    return;

                search = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show bookmarks.
        /// </summary>
        public bool ShowBookmarks
        {
            get { return showBookmarks; }

            set
            {
                if (showBookmarks == value)
                    return;

                showBookmarks = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show busy away.
        /// </summary>
        public bool ShowBusyAway
        {
            get { return showBusyAway; }

            set
            {
                if (showBusyAway == value)
                    return;

                showBusyAway = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show dnd.
        /// </summary>
        public bool ShowDnd
        {
            get { return showDnd; }

            set
            {
                if (showDnd == value)
                    return;

                showDnd = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show friends.
        /// </summary>
        public bool ShowFriends
        {
            get { return showFriends; }

            set
            {
                if (showFriends == value)
                    return;

                showFriends = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show ignored.
        /// </summary>
        public bool ShowIgnored
        {
            get { return showIgnored; }

            set
            {
                if (showIgnored == value)
                    return;

                showIgnored = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show looking.
        /// </summary>
        public bool ShowLooking
        {
            get { return showLooking; }

            set
            {
                if (showLooking == value)
                    return;

                showLooking = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show mods.
        /// </summary>
        public bool ShowMods
        {
            get { return showMods; }

            set
            {
                if (showMods == value)
                    return;

                showMods = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show normal.
        /// </summary>
        public bool ShowNormal
        {
            get { return showNormal; }

            set
            {
                if (showNormal == value)
                    return;

                showNormal = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show not interested.
        /// </summary>
        public bool ShowNotInterested
        {
            get { return showNotInterested; }

            set
            {
                if (showNotInterested == value)
                    return;

                showNotInterested = value;
                CallUpdate();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The meets search string.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool MeetsSearchString(ICharacter character)
        {
            return character.NameContains(SearchString);
        }

        /// <summary>
        ///     The meets status filter.
        /// </summary>
        /// <param name="character">
        ///     The character.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool MeetsStatusFilter(ICharacter character)
        {
            switch (character.Status)
            {
                case StatusType.Idle:
                case StatusType.Away:
                case StatusType.Busy:
                    return showBusyAway;

                case StatusType.Dnd:
                    return showDnd;

                case StatusType.Looking:
                    return showLooking;

                case StatusType.Crown:
                case StatusType.Online:
                    return showNormal;

                default:
                    return ShowOffline;
            }
        }

        #endregion

        #region Methods

        private void CallUpdate()
        {
            Updated?.Invoke(this, new EventArgs());
        }

        private void Log(string text)
        {
            Logging.Log(text, "channel setting");
        }

        #endregion

        // dev note: I haven't really found a better way to do a lot of properties like this. UI can't access dictionaries.
    }
}