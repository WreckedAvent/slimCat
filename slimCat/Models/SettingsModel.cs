using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using lib;
using slimCat.Properties;

namespace Models
{
    /// <summary>
    /// Gender Settings Model provides basic settings for a gender filter
    /// </summary>
    public class GenderSettingsModel
    {
        #region Events
        /// <summary>
        /// Called whenever the UI updates one of the genders
        /// </summary>
        public event EventHandler Updated;
        #endregion

        #region Fields
        private IDictionary<Gender, bool> _genderFilter = new Dictionary<Gender, bool>
        { 
            { Gender.Male, true }, { Gender.Female, true }, { Gender.Herm_F, true },
            { Gender.Herm_M, true }, { Gender.Cuntboy, true }, { Gender.Shemale, true },
            { Gender.None, true }, { Gender.Transgender, true },
        };
        #endregion

        #region Properties
        public IDictionary<Gender, bool> GenderFilter { get { return _genderFilter; } }

        public IEnumerable<Gender> FilteredGenders
        {
            get
            {
               return GenderFilter.Where(x => x.Value == false).Select(x => x.Key);
            }
        }

        #region Gender Accessors for UI
        public bool ShowMales
        {
            get { return _genderFilter[Gender.Male]; }

            set
            {
                if (_genderFilter[Gender.Male] != value)
                {
                    _genderFilter[Gender.Male] = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowFemales
        {
            get { return _genderFilter[Gender.Female]; }

            set
            {
                if (_genderFilter[Gender.Female] != value)
                {
                    _genderFilter[Gender.Female] = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowMaleHerms
        {
            get { return _genderFilter[Gender.Herm_M]; }

            set
            {
                if (_genderFilter[Gender.Herm_M] != value)
                {
                    _genderFilter[Gender.Herm_M] = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowFemaleHerms
        {
            get { return _genderFilter[Gender.Herm_F]; }

            set
            {
                if (_genderFilter[Gender.Herm_F] != value)
                {
                    _genderFilter[Gender.Herm_F] = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowCuntboys
        {
            get { return _genderFilter[Gender.Cuntboy]; }

            set
            {
                if (_genderFilter[Gender.Cuntboy] != value)
                {
                    _genderFilter[Gender.Cuntboy] = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowNoGenders
        {
            get { return _genderFilter[Gender.None]; }

            set
            {
                if (_genderFilter[Gender.None] != value)
                {
                    _genderFilter[Gender.None] = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowShemales
        {
            get { return _genderFilter[Gender.Shemale]; }

            set
            {
                if (_genderFilter[Gender.Shemale] != value)
                {
                    _genderFilter[Gender.Shemale] = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowTransgenders
        {
            get { return _genderFilter[Gender.Transgender]; }

            set
            {
                if (_genderFilter[Gender.Transgender] != value)
                {
                    _genderFilter[Gender.Transgender] = value;
                    Updated(this, new EventArgs());
                }
            }
        }
        #endregion
        #endregion

        private void CallUpdate()
        {
            if (Updated != null)
                Updated(this, new EventArgs());
        }

        public bool MeetsGenderFilter(ICharacter character)
        {
            return _genderFilter[character.Gender];
        }
    }

    /// <summary>
    /// Search settings to be used with a search text box tool cahin
    /// </summary>
    public class GenericSearchSettingsModel
    {
        #region Events
        /// <summary>
        /// Called when our search settings are updated
        /// </summary>
        public event EventHandler Updated;
        #endregion

        // dev note: I haven't really found a better way to do a lot of properties like this. UI can't access dictionaries.

        #region Fields
        private bool _isChangingSettings = false;
        private string _search = "";
        private bool _showFriends = true;
        private bool _showBookmarks = true;
        private bool _showMods = true;
        private bool _showLooking = true;
        private bool _showNormal = true;
        private bool _showBusyAway = true;
        private bool _showDND = true;
        private bool _showIgnore = false;
        #endregion

        #region Properties
        public string SearchString
        {
            get { return (_search == null ? _search: _search.ToLower()); }
            set
            {
                if (_search != value)
                {
                    _search = value;
                    CallUpdate();
                }
            }
        }

        #region Accessors for the UI
        public bool ShowFriends
        {
            get { return _showFriends; }

            set
            {
                if (_showFriends != value)
                {
                    _showFriends = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowBookmarks
        {
            get { return _showBookmarks; }

            set
            {
                if (_showBookmarks != value)
                {
                    _showBookmarks = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowMods
        {
            get { return _showMods; }

            set
            {
                if (_showMods != value)
                {
                    _showMods = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowLooking
        {
            get { return _showLooking; }
            set
            {
                if (_showLooking != value)
                {
                    _showLooking = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowNormal
        {
            get { return _showNormal; }

            set
            {
                if (_showNormal != value)
                {
                    _showNormal = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowBusyAway
        {
            get { return _showBusyAway; }

            set
            {
                if (_showBusyAway != value)
                {
                    _showBusyAway = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowIgnored
        {
            get { return _showIgnore; }
            set
            {
                if (_showIgnore != value)
                {
                    _showIgnore = value;
                    CallUpdate();
                }
            }
        }

        public bool ShowDND
        {
            get { return _showDND; }
            set
            {
                if (_showDND != value)
                {
                    _showDND = value;
                    CallUpdate();
                }
            }
        }
        #endregion

        public bool IsChangingSettings
        {
            get { return _isChangingSettings; }
            set
            {
                if (_isChangingSettings != value)
                {
                    _isChangingSettings = value;
                    CallUpdate();
                }
            }
        }
        #endregion

        #region Commands
        RelayCommand _expandSearch;
        public ICommand OpenSearchSettingsCommand
        {
            get
            {
                if (_expandSearch == null)
                    _expandSearch = new RelayCommand(param => IsChangingSettings = !IsChangingSettings);

                return _expandSearch;
            }
        }
        #endregion

        private void CallUpdate()
        {
            if (Updated != null)
                Updated(this, new EventArgs());
        }

        public bool MeetsStatusFilter(ICharacter character)
        {
            switch (character.Status)
            {
                case StatusType.idle:
                case StatusType.away:
                case StatusType.busy:
                    return _showBusyAway;

                case StatusType.dnd:
                    return _showDND;

                case StatusType.looking:
                    return _showLooking;

                case StatusType.crown:
                case StatusType.online:
                    return _showNormal;

                default:
                    return false;
            }
        }

        public bool MeetsSearchString(ICharacter character)
        {
            return character.NameContains(SearchString);
        }
    }

    /// <summary>
    /// Channel settings specific to each channel
    /// </summary>
    public class ChannelSettingsModel
    {
        public event EventHandler Updated;

        #region Fields
        private bool _shouldFlash = true;
        private bool _shouldDing = false;
        private int _shouldFlashInterval = 1;

        private bool _notifyWhenThisCharacterIsMentioned = true;
        private bool _notifyIncludesMessages = true;
        private bool _notifyIncludesCharacterNames = false;
        private bool _notifyOnWholeOnly = true;
        private string _notifyOnTheseTerms = "";
        private IEnumerable<string> _notifyEnumerate;
        private bool _notifyTermsChanged = false;

        private bool _isChangingSettings = false;
        #endregion

        public ChannelSettingsModel() : this(false) { }
        public ChannelSettingsModel(bool isPM = false)
        {
            if (isPM)
            {
                _shouldDing = true;
                _notifyWhenThisCharacterIsMentioned = false;
            }
        }

        #region Properties
        /// <summary>
        /// If this tab should flash on a new message
        /// </summary>
        public bool ShouldFlash
        {
            get { return _shouldFlash; }
            set
            {
                if (_shouldFlash != value)
                {
                    _shouldFlash = value;
                    CallUpdate();
                }
            }
        }

        /// <summary>
        /// If this tab should ding the user on a new message
        /// </summary>
        public bool ShouldDing
        {
            get { return _shouldDing; }
            set
            {
                if (_shouldDing != value)
                {
                    _shouldDing = value;
                    CallUpdate();
                }
            }
        }

        /// <summary>
        /// If a notification will be arised whenever this character is mentioned
        /// </summary>
        public bool NotifyCharacterMention
        {
            get { return _notifyWhenThisCharacterIsMentioned; }
            set
            {
                if (_notifyWhenThisCharacterIsMentioned != value)
                {
                    _notifyWhenThisCharacterIsMentioned = value;
                    CallUpdate();
                }
            }
        }

        /// <summary>
        /// How many unread messages must be accumulated before the channel tab flashes
        /// </summary>
        public int FlashInterval
        {
            get { return _shouldFlashInterval; }
            set
            {
                if (_shouldFlashInterval != value || value < 1)
                {
                    _shouldFlashInterval = value;
                    CallUpdate();
                }
            }
        }

        /// <summary>
        /// Raw terms which will ding the user when mentioned
        /// </summary>
        public string NotifyTerms
        {
            get { return _notifyOnTheseTerms.Trim().ToLower(); }
            set
            {
                if (_notifyOnTheseTerms != value)
                {
                    _notifyOnTheseTerms = value;
                    CallUpdate();
                }
            }
        }

        /// <summary>
        /// NotifyTerms processed into an array
        /// </summary>
        public IEnumerable<string> EnumerableTerms
        {
            get
            {
                if (!_notifyTermsChanged || _notifyEnumerate != null)
                {
                    _notifyTermsChanged = false;

                    _notifyEnumerate = _notifyOnTheseTerms.Split(',').Select(word => word.Trim()).Where(word => !string.IsNullOrWhiteSpace(word));
                    // tokenizes our terms
                }

                return _notifyEnumerate;
            }
        }

        /// <summary>
        /// If a term notification dings when the term appears in a character's name
        /// </summary>
        public bool NotifyIncludesCharacterNames
        {
            get { return _notifyIncludesCharacterNames; }
            set
            {
                _notifyIncludesCharacterNames = value;
                CallUpdate();
            }
        }

        /// <summary>
        /// If a term notification dings when the term appears as a whole word only
        /// </summary>
        public bool NotifyOnWholeWordsOnly
        {
            get { return _notifyOnWholeOnly; }
            set
            {
                _notifyOnWholeOnly = value;
                CallUpdate();
            }
        }

        /// <summary>
        /// If a term notification dings when the term appears in a character's post
        /// </summary>
        public bool NotifyIncludesMessages
        {
            get { return _notifyIncludesMessages; }
            set
            {
                _notifyIncludesMessages = value;
                CallUpdate();
            }
        }

        public bool IsChangingSettings
        {
            get { return _isChangingSettings; }
            set
            {
                if (_isChangingSettings != value)
                {
                    _isChangingSettings = value;
                    CallUpdate();
                }
            }
        }
        #endregion

        #region Commands
        RelayCommand _expandSettings;
        public ICommand OpenChannelSettingsCommand
        {
            get
            {
                if (_expandSettings == null)
                    _expandSettings = new RelayCommand(param => IsChangingSettings = !IsChangingSettings);

                return _expandSettings;
            }
        }
        #endregion

        private void CallUpdate()
        {
            if (Updated != null)
                Updated(this, new EventArgs());
        }
    }

    /// <summary>
    /// Settings for the entire application
    /// </summary>
    public static class ApplicationSettings
    {
        static ApplicationSettings()
        {
            Volume = 0.5;
            ShowNotificationsGlobal = true;
            BackLogMax = 300;
        }

        public static double Volume { get; set; }
        public static bool ShowNotificationsGlobal { get; set; }
        public static int BackLogMax { get; set; }
    }
}
