namespace Slimcat.Models
{
    using System;
    using System.Windows.Input;

    using Slimcat.Libraries;
    using Slimcat.Utilities;

    /// <summary>
    ///     Search settings to be used with a search text box tool cahin
    /// </summary>
    public class GenericSearchSettingsModel
    {
        // dev note: I haven't really found a better way to do a lot of properties like this. UI can't access dictionaries.
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
            get
            {
                return this.isChangingSettings;
            }

            set
            {
                if (this.isChangingSettings == value)
                {
                    return;
                }

                this.isChangingSettings = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets the open search settings command.
        /// </summary>
        public ICommand OpenSearchSettingsCommand
        {
            get
            {
                return this.expandSearch
                       ?? (this.expandSearch =
                           new RelayCommand(param => this.IsChangingSettings = !this.IsChangingSettings));
            }
        }

        /// <summary>
        ///     Gets or sets the search string.
        /// </summary>
        public string SearchString
        {
            get
            {
                return this.search == null ? this.search : this.search.ToLower();
            }

            set
            {
                if (this.search == value)
                {
                    return;
                }

                this.search = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show bookmarks.
        /// </summary>
        public bool ShowBookmarks
        {
            get
            {
                return this.showBookmarks;
            }

            set
            {
                if (this.showBookmarks != value)
                {
                    this.showBookmarks = value;
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
                return this.showBusyAway;
            }

            set
            {
                if (this.showBusyAway == value)
                {
                    return;
                }

                this.showBusyAway = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show dnd.
        /// </summary>
        public bool ShowDnd
        {
            get
            {
                return this.showDnd;
            }

            set
            {
                if (this.showDnd == value)
                {
                    return;
                }

                this.showDnd = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show friends.
        /// </summary>
        public bool ShowFriends
        {
            get
            {
                return this.showFriends;
            }

            set
            {
                if (this.showFriends == value)
                {
                    return;
                }

                this.showFriends = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show ignored.
        /// </summary>
        public bool ShowIgnored
        {
            get
            {
                return this.showIgnored;
            }

            set
            {
                if (this.showIgnored == value)
                {
                    return;
                }

                this.showIgnored = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show looking.
        /// </summary>
        public bool ShowLooking
        {
            get
            {
                return this.showLooking;
            }

            set
            {
                if (this.showLooking == value)
                {
                    return;
                }

                this.showLooking = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show mods.
        /// </summary>
        public bool ShowMods
        {
            get
            {
                return this.showMods;
            }

            set
            {
                if (this.showMods == value)
                {
                    return;
                }

                this.showMods = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show normal.
        /// </summary>
        public bool ShowNormal
        {
            get
            {
                return this.showNormal;
            }

            set
            {
                if (this.showNormal == value)
                {
                    return;
                }

                this.showNormal = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show not interested.
        /// </summary>
        public bool ShowNotInterested
        {
            get
            {
                return this.showNotInterested;
            }

            set
            {
                if (this.showNotInterested == value)
                {
                    return;
                }

                this.showNotInterested = value;
                this.CallUpdate();
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
                    return this.showBusyAway;

                case StatusType.dnd:
                    return this.showDnd;

                case StatusType.looking:
                    return this.showLooking;

                case StatusType.crown:
                case StatusType.online:
                    return this.showNormal;

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
}