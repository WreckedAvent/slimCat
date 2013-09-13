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

namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using lib;

    /// <summary>
    ///     Gender Settings Model provides basic settings for a gender filter
    /// </summary>
    public class GenderSettingsModel
    {
        #region Fields

        private readonly IDictionary<Gender, bool> _genderFilter = new Dictionary<Gender, bool>
                                                                       {
                                                                           { Gender.Male, true }, 
                                                                           {
                                                                               Gender.Female, true
                                                                           }, 
                                                                           {
                                                                               Gender.Herm_F, true
                                                                           }, 
                                                                           {
                                                                               Gender.Herm_M, true
                                                                           }, 
                                                                           {
                                                                               Gender.Cuntboy, true
                                                                           }, 
                                                                           {
                                                                               Gender.Shemale, true
                                                                           }, 
                                                                           { Gender.None, true }, 
                                                                           {
                                                                               Gender.Transgender, 
                                                                               true
                                                                           }, 
                                                                       };

        #endregion

        #region Public Events

        /// <summary>
        ///     Called whenever the UI updates one of the genders
        /// </summary>
        public event EventHandler Updated;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the filtered genders.
        /// </summary>
        public IEnumerable<Gender> FilteredGenders
        {
            get
            {
                return this.GenderFilter.Where(x => x.Value == false).Select(x => x.Key);
            }
        }

        /// <summary>
        ///     Gets the gender filter.
        /// </summary>
        public IDictionary<Gender, bool> GenderFilter
        {
            get
            {
                return this._genderFilter;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show cuntboys.
        /// </summary>
        public bool ShowCuntboys
        {
            get
            {
                return this._genderFilter[Gender.Cuntboy];
            }

            set
            {
                if (this._genderFilter[Gender.Cuntboy] != value)
                {
                    this._genderFilter[Gender.Cuntboy] = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show female herms.
        /// </summary>
        public bool ShowFemaleHerms
        {
            get
            {
                return this._genderFilter[Gender.Herm_F];
            }

            set
            {
                if (this._genderFilter[Gender.Herm_F] != value)
                {
                    this._genderFilter[Gender.Herm_F] = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show females.
        /// </summary>
        public bool ShowFemales
        {
            get
            {
                return this._genderFilter[Gender.Female];
            }

            set
            {
                if (this._genderFilter[Gender.Female] != value)
                {
                    this._genderFilter[Gender.Female] = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show male herms.
        /// </summary>
        public bool ShowMaleHerms
        {
            get
            {
                return this._genderFilter[Gender.Herm_M];
            }

            set
            {
                if (this._genderFilter[Gender.Herm_M] != value)
                {
                    this._genderFilter[Gender.Herm_M] = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show males.
        /// </summary>
        public bool ShowMales
        {
            get
            {
                return this._genderFilter[Gender.Male];
            }

            set
            {
                if (this._genderFilter[Gender.Male] != value)
                {
                    this._genderFilter[Gender.Male] = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show no genders.
        /// </summary>
        public bool ShowNoGenders
        {
            get
            {
                return this._genderFilter[Gender.None];
            }

            set
            {
                if (this._genderFilter[Gender.None] != value)
                {
                    this._genderFilter[Gender.None] = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show shemales.
        /// </summary>
        public bool ShowShemales
        {
            get
            {
                return this._genderFilter[Gender.Shemale];
            }

            set
            {
                if (this._genderFilter[Gender.Shemale] != value)
                {
                    this._genderFilter[Gender.Shemale] = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show transgenders.
        /// </summary>
        public bool ShowTransgenders
        {
            get
            {
                return this._genderFilter[Gender.Transgender];
            }

            set
            {
                if (this._genderFilter[Gender.Transgender] != value)
                {
                    this._genderFilter[Gender.Transgender] = value;
                    this.Updated(this, new EventArgs());
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The meets gender filter.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool MeetsGenderFilter(ICharacter character)
        {
            return this._genderFilter[character.Gender];
        }

        #endregion

        #region Methods

        private void CallUpdate()
        {
            if (this.Updated != null)
            {
                this.Updated(this, new EventArgs());
            }
        }

        #endregion
    }

    /// <summary>
    ///     Search settings to be used with a search text box tool cahin
    /// </summary>
    public class GenericSearchSettingsModel
    {
        // dev note: I haven't really found a better way to do a lot of properties like this. UI can't access dictionaries.
        #region Fields

        private RelayCommand _expandSearch;

        private bool _isChangingSettings;

        private string _search = string.Empty;

        private bool _showBookmarks = true;

        private bool _showBusyAway = true;

        private bool _showDND = true;

        private bool _showFriends = true;

        private bool _showIgnored;

        private bool _showLooking = true;

        private bool _showMods = true;

        private bool _showNormal = true;

        private bool _showNotInterested;

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
            get
            {
                return this._isChangingSettings;
            }

            set
            {
                if (this._isChangingSettings != value)
                {
                    this._isChangingSettings = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets the open search settings command.
        /// </summary>
        public ICommand OpenSearchSettingsCommand
        {
            get
            {
                if (this._expandSearch == null)
                {
                    this._expandSearch = new RelayCommand(param => this.IsChangingSettings = !this.IsChangingSettings);
                }

                return this._expandSearch;
            }
        }

        /// <summary>
        ///     Gets or sets the search string.
        /// </summary>
        public string SearchString
        {
            get
            {
                return this._search == null ? this._search : this._search.ToLower();
            }

            set
            {
                if (this._search != value)
                {
                    this._search = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show bookmarks.
        /// </summary>
        public bool ShowBookmarks
        {
            get
            {
                return this._showBookmarks;
            }

            set
            {
                if (this._showBookmarks != value)
                {
                    this._showBookmarks = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show busy away.
        /// </summary>
        public bool ShowBusyAway
        {
            get
            {
                return this._showBusyAway;
            }

            set
            {
                if (this._showBusyAway != value)
                {
                    this._showBusyAway = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show dnd.
        /// </summary>
        public bool ShowDND
        {
            get
            {
                return this._showDND;
            }

            set
            {
                if (this._showDND != value)
                {
                    this._showDND = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show friends.
        /// </summary>
        public bool ShowFriends
        {
            get
            {
                return this._showFriends;
            }

            set
            {
                if (this._showFriends != value)
                {
                    this._showFriends = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show ignored.
        /// </summary>
        public bool ShowIgnored
        {
            get
            {
                return this._showIgnored;
            }

            set
            {
                if (this._showIgnored != value)
                {
                    this._showIgnored = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show looking.
        /// </summary>
        public bool ShowLooking
        {
            get
            {
                return this._showLooking;
            }

            set
            {
                if (this._showLooking != value)
                {
                    this._showLooking = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show mods.
        /// </summary>
        public bool ShowMods
        {
            get
            {
                return this._showMods;
            }

            set
            {
                if (this._showMods != value)
                {
                    this._showMods = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show normal.
        /// </summary>
        public bool ShowNormal
        {
            get
            {
                return this._showNormal;
            }

            set
            {
                if (this._showNormal != value)
                {
                    this._showNormal = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show not interested.
        /// </summary>
        public bool ShowNotInterested
        {
            get
            {
                return this._showNotInterested;
            }

            set
            {
                if (this._showNotInterested != value)
                {
                    this._showNotInterested = value;
                    this.CallUpdate();
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The meets search string.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool MeetsSearchString(ICharacter character)
        {
            return character.NameContains(this.SearchString);
        }

        /// <summary>
        /// The meets status filter.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool MeetsStatusFilter(ICharacter character)
        {
            switch (character.Status)
            {
                case StatusType.idle:
                case StatusType.away:
                case StatusType.busy:
                    return this._showBusyAway;

                case StatusType.dnd:
                    return this._showDND;

                case StatusType.looking:
                    return this._showLooking;

                case StatusType.crown:
                case StatusType.online:
                    return this._showNormal;

                default:
                    return false;
            }
        }

        #endregion

        #region Methods

        private void CallUpdate()
        {
            if (this.Updated != null)
            {
                this.Updated(this, new EventArgs());
            }
        }

        #endregion
    }

    /// <summary>
    ///     Channel settings specific to each channel
    /// </summary>
    public class ChannelSettingsModel
    {
        #region Fields

        private bool _enableLogging = true;

        private RelayCommand _expandSettings;

        private bool _isChangingSettings;

        private int _joinLeaveLevel = (int)NotifyLevel.NotificationOnly;

        private bool _joinLeaveNotifyOnlyForInteresting = true;

        private int _messageLevel = (int)NotifyLevel.NotificationOnly;

        private bool _messageOnlyForInteresting;

        private IEnumerable<string> _notifyEnumerate;

        private bool _notifyIncludesCharacterNames;

        private string _notifyOnTheseTerms = string.Empty;

        private bool _notifyTermsChanged;

        private int _promoteDemoteLevel = (int)NotifyLevel.NotificationAndToast;

        private bool _promoteDemoteNotifyOnlyForInteresting;

        private int _shouldFlashInterval = 1;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelSettingsModel" /> class.
        /// </summary>
        public ChannelSettingsModel()
            : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSettingsModel"/> class.
        /// </summary>
        /// <param name="isPM">
        /// The is pm.
        /// </param>
        public ChannelSettingsModel(bool isPM = false)
        {
            if (isPM)
            {
                this.MessageNotifyLevel = (int)NotifyLevel.NotificationAndSound;
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     The updated.
        /// </summary>
        public event EventHandler Updated;

        #endregion

        #region Enums

        /// <summary>
        ///     The notify level.
        /// </summary>
        public enum NotifyLevel
        {
            /// <summary>
            ///     The no notification.
            /// </summary>
            NoNotification, 

            /// <summary>
            ///     The notification only.
            /// </summary>
            NotificationOnly, 

            /// <summary>
            ///     The notification and toast.
            /// </summary>
            NotificationAndToast, 

            /// <summary>
            ///     The notification and sound.
            /// </summary>
            NotificationAndSound
        };

        #endregion

        #region Public Properties

        /// <summary>
        ///     NotifyTerms processed into an array
        /// </summary>
        public IEnumerable<string> EnumerableTerms
        {
            get
            {
                if (!this._notifyTermsChanged || this._notifyEnumerate != null)
                {
                    this._notifyTermsChanged = false;

                    this._notifyEnumerate =
                        this._notifyOnTheseTerms.Split(',')
                            .Select(word => word.Trim())
                            .Where(word => !string.IsNullOrWhiteSpace(word));

                    // tokenizes our terms
                }

                return this._notifyEnumerate;
            }
        }

        /// <summary>
        ///     How many unread messages must be accumulated before the channel tab flashes
        /// </summary>
        public int FlashInterval
        {
            get
            {
                return this._shouldFlashInterval;
            }

            set
            {
                if (this._shouldFlashInterval != value || value < 1)
                {
                    this._shouldFlashInterval = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is changing settings.
        /// </summary>
        public bool IsChangingSettings
        {
            get
            {
                return this._isChangingSettings;
            }

            set
            {
                if (this._isChangingSettings != value)
                {
                    this._isChangingSettings = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets or sets the join leave notify level.
        /// </summary>
        public int JoinLeaveNotifyLevel
        {
            get
            {
                return this._joinLeaveLevel;
            }

            set
            {
                this._joinLeaveLevel = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether join leave notify only for interesting.
        /// </summary>
        public bool JoinLeaveNotifyOnlyForInteresting
        {
            get
            {
                return this._joinLeaveNotifyOnlyForInteresting;
            }

            set
            {
                this._joinLeaveNotifyOnlyForInteresting = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     If we log each message
        /// </summary>
        public bool LoggingEnabled
        {
            get
            {
                return this._enableLogging;
            }

            set
            {
                this._enableLogging = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     All Notify levels perform fairly simply:
        ///     0 for no notification ever
        ///     1 for a simple notification
        ///     2 for a simple notification and a toast
        ///     3 for a simple notification, a toast, and a sound
        /// </summary>
        public int MessageNotifyLevel
        {
            get
            {
                return this._messageLevel;
            }

            set
            {
                this._messageLevel = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether message notify only for interesting.
        /// </summary>
        public bool MessageNotifyOnlyForInteresting
        {
            get
            {
                return this._messageOnlyForInteresting;
            }

            set
            {
                this._messageOnlyForInteresting = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     If a term notification dings when the term appears in a character's name
        /// </summary>
        public bool NotifyIncludesCharacterNames
        {
            get
            {
                return this._notifyIncludesCharacterNames;
            }

            set
            {
                this._notifyIncludesCharacterNames = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Raw terms which will ding the user when mentioned
        /// </summary>
        public string NotifyTerms
        {
            get
            {
                return this._notifyOnTheseTerms.Trim().ToLower();
            }

            set
            {
                if (this._notifyOnTheseTerms != value)
                {
                    this._notifyOnTheseTerms = value;
                    this.CallUpdate();
                }
            }
        }

        /// <summary>
        ///     Gets the open channel settings command.
        /// </summary>
        public ICommand OpenChannelSettingsCommand
        {
            get
            {
                if (this._expandSettings == null)
                {
                    this._expandSettings = new RelayCommand(param => this.IsChangingSettings = !this.IsChangingSettings);
                }

                return this._expandSettings;
            }
        }

        /// <summary>
        ///     Gets or sets the promote demote notify level.
        /// </summary>
        public int PromoteDemoteNotifyLevel
        {
            get
            {
                return this._promoteDemoteLevel;
            }

            set
            {
                this._promoteDemoteLevel = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether promote demote notify only for interesting.
        /// </summary>
        public bool PromoteDemoteNotifyOnlyForInteresting
        {
            get
            {
                return this._promoteDemoteNotifyOnlyForInteresting;
            }

            set
            {
                this._promoteDemoteNotifyOnlyForInteresting = value;
                this.CallUpdate();
            }
        }

        #endregion

        #region Methods

        private void CallUpdate()
        {
            if (this.Updated != null)
            {
                this.Updated(this, new EventArgs());
            }
        }

        #endregion
    }

    /// <summary>
    ///     Settings for the entire application
    /// </summary>
    public static class ApplicationSettings
    {
        #region Static Fields

        private static readonly IList<string> _interested;

        private static readonly IList<string> _savedChannels;

        private static readonly IList<string> _uninterested;

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
            _savedChannels = new List<string>();
            _interested = new List<string>();
            _uninterested = new List<string>();
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
        ///     Gets the interested.
        /// </summary>
        public static IList<string> Interested
        {
            get
            {
                return _interested;
            }
        }

        /// <summary>
        ///     Gets the not interested.
        /// </summary>
        public static IList<string> NotInterested
        {
            get
            {
                return _uninterested;
            }
        }

        /// <summary>
        ///     Gets the saved channels.
        /// </summary>
        public static IList<string> SavedChannels
        {
            get
            {
                return _savedChannels;
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