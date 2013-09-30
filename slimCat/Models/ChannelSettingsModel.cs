namespace Slimcat.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using Slimcat.Libraries;

    /// <summary>
    ///     Channel settings specific to each channel
    /// </summary>
    public class ChannelSettingsModel
    {
        #region Fields

        private bool enableLogging = true;

        private RelayCommand expandSettings;

        private bool isChangingSettings;

        private int joinLeaveLevel = (int)NotifyLevel.NotificationOnly;

        private bool joinLeaveNotifyOnlyForInteresting = true;

        private int messageLevel = (int)NotifyLevel.NotificationOnly;

        private bool messageOnlyForInteresting;

        private IEnumerable<string> notifyEnumerable;

        private bool notifyIncludesCharacterNames;

        private string notifyOnTheseTerms = string.Empty;

        private bool notifyTermsChanged;

        private int promoteDemoteLevel = (int)NotifyLevel.NotificationAndToast;

        private bool promoteDemoteNotifyOnlyForInteresting;

        private int shouldFlashInterval = 1;

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
        /// <param name="isPm">
        /// The is PrivateMessage.
        /// </param>
        public ChannelSettingsModel(bool isPm = false)
        {
            if (isPm)
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
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     NotifyTerms processed into an array
        /// </summary>
        public IEnumerable<string> EnumerableTerms
        {
            get
            {
                if (!this.notifyTermsChanged || this.notifyEnumerable != null)
                {
                    this.notifyTermsChanged = false;

                    this.notifyEnumerable =
                        this.notifyOnTheseTerms.Split(',')
                            .Select(word => word.Trim())
                            .Where(word => !string.IsNullOrWhiteSpace(word));

                    // tokenizes our terms
                }

                return this.notifyEnumerable;
            }
        }

        /// <summary>
        ///     How many unread messages must be accumulated before the channel tab flashes
        /// </summary>
        public int FlashInterval
        {
            get
            {
                return this.shouldFlashInterval;
            }

            set
            {
                if (this.shouldFlashInterval == value && value >= 1)
                {
                    return;
                }

                this.shouldFlashInterval = value;
                this.CallUpdate();
            }
        }

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
        ///     Gets or sets the join leave notify level.
        /// </summary>
        public int JoinLeaveNotifyLevel
        {
            get
            {
                return this.joinLeaveLevel;
            }

            set
            {
                this.joinLeaveLevel = value;
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
                return this.joinLeaveNotifyOnlyForInteresting;
            }

            set
            {
                this.joinLeaveNotifyOnlyForInteresting = value;
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
                return this.enableLogging;
            }

            set
            {
                this.enableLogging = value;
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
                return this.messageLevel;
            }

            set
            {
                this.messageLevel = value;
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
                return this.messageOnlyForInteresting;
            }

            set
            {
                this.messageOnlyForInteresting = value;
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
                return this.notifyIncludesCharacterNames;
            }

            set
            {
                this.notifyIncludesCharacterNames = value;
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
                return this.notifyOnTheseTerms.Trim().ToLower();
            }

            set
            {
                if (this.notifyOnTheseTerms == value)
                {
                    return;
                }

                this.notifyOnTheseTerms = value;
                this.CallUpdate();
            }
        }

        /// <summary>
        ///     Gets the open channel settings command.
        /// </summary>
        public ICommand OpenChannelSettingsCommand
        {
            get
            {
                return this.expandSettings
                       ?? (this.expandSettings =
                           new RelayCommand(param => this.IsChangingSettings = !this.IsChangingSettings));
            }
        }

        /// <summary>
        ///     Gets or sets the promote demote notify level.
        /// </summary>
        public int PromoteDemoteNotifyLevel
        {
            get
            {
                return this.promoteDemoteLevel;
            }

            set
            {
                this.promoteDemoteLevel = value;
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
                return this.promoteDemoteNotifyOnlyForInteresting;
            }

            set
            {
                this.promoteDemoteNotifyOnlyForInteresting = value;
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
}