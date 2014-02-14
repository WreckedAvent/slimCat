#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelSettingsModel.cs">
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
    using System.Windows.Input;
    using Libraries;

    #endregion

    /// <summary>
    /// Channel settings specific to each channel
    /// </summary>
    public class ChannelSettingsModel
    {
        #region Fields

        private bool enableLogging = true;

        private bool alertAboutUpdates = true;

        private RelayCommand expandSettings;

        private bool isChangingSettings;

        private int joinLeaveLevel = (int) NotifyLevel.NotificationOnly;

        private bool joinLeaveNotifyOnlyForInteresting = true;

        private int messageLevel = (int) NotifyLevel.NotificationOnly;

        private bool messageOnlyForInteresting;

        private IEnumerable<string> notifyEnumerable;

        private bool notifyIncludesCharacterNames;

        private string notifyOnTheseTerms = string.Empty;

        private bool notifyTermsChanged;

        private int promoteDemoteLevel = (int) NotifyLevel.NotificationAndToast;

        private bool promoteDemoteNotifyOnlyForInteresting;

        private int shouldFlashInterval = 1;
        private int adNotifyLevel;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelSettingsModel" /> class.
        /// </summary>
        public ChannelSettingsModel()
            // ReSharper disable RedundantArgumentDefaultValue
            : this(false)
            // ReSharper restore RedundantArgumentDefaultValue
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelSettingsModel" /> class.
        /// </summary>
        /// <param name="isPm">
        ///     The is PrivateMessage.
        /// </param>
        public ChannelSettingsModel(bool isPm = false)
        {
            if (isPm)
                MessageNotifyLevel = (int) NotifyLevel.NotificationAndSound;
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
                if (!notifyTermsChanged || notifyEnumerable != null)
                {
                    notifyTermsChanged = false;

                    notifyEnumerable =
                        notifyOnTheseTerms.Split(',')
                            .Select(word => word.Trim())
                            .Where(word => !string.IsNullOrWhiteSpace(word));

                    // tokenizes our terms
                }

                return notifyEnumerable;
            }
        }

        /// <summary>
        ///     How many unread messages must be accumulated before the channel tab flashes
        /// </summary>
        public int FlashInterval
        {
            get { return shouldFlashInterval; }

            set
            {
                if (shouldFlashInterval == value && value >= 1)
                    return;

                shouldFlashInterval = value;
                CallUpdate();
            }
        }

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

                isChangingSettings = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets the join leave notify level.
        /// </summary>
        public int JoinLeaveNotifyLevel
        {
            get { return joinLeaveLevel; }

            set
            {
                joinLeaveLevel = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether join leave notify only for interesting.
        /// </summary>
        public bool JoinLeaveNotifyOnlyForInteresting
        {
            get { return joinLeaveNotifyOnlyForInteresting; }

            set
            {
                joinLeaveNotifyOnlyForInteresting = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     If we log each message
        /// </summary>
        public bool LoggingEnabled
        {
            get { return enableLogging; }

            set
            {
                enableLogging = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     0 for no notification ever
        ///     1 for a simple notification
        ///     2 for a simple notification and a toast
        ///     3 for a simple notification, a toast, and a sound
        /// </summary>
        public int MessageNotifyLevel
        {
            get { return messageLevel; }

            set
            {
                messageLevel = value;
                CallUpdate();
            }
        }   
     
        /// <summary>
        ///     0 for no notification ever
        ///     1 for a simple notification
        ///     2 for a simple notification and a toast
        ///     3 for a simple notification, a toast, and a sound
        /// </summary>
        public int AdNotifyLevel
        {
            get { return adNotifyLevel; }

            set
            {
                adNotifyLevel = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether to notify about updates.
        /// </summary>
        public bool AlertAboutUpdates
        {
            get { return alertAboutUpdates; }

            set
            {
                alertAboutUpdates = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether message notify only for interesting.
        /// </summary>
        public bool MessageNotifyOnlyForInteresting
        {
            get { return messageOnlyForInteresting; }

            set
            {
                messageOnlyForInteresting = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     If a term notification dings when the term appears in a character's name
        /// </summary>
        public bool NotifyIncludesCharacterNames
        {
            get { return notifyIncludesCharacterNames; }

            set
            {
                notifyIncludesCharacterNames = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Raw terms which will ding the user when mentioned
        /// </summary>
        public string NotifyTerms
        {
            get { return notifyOnTheseTerms.Trim().ToLower(); }

            set
            {
                if (notifyOnTheseTerms == value)
                    return;

                notifyOnTheseTerms = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets the open channel settings command.
        /// </summary>
        public ICommand OpenChannelSettingsCommand
        {
            get
            {
                return expandSettings
                       ?? (expandSettings =
                           new RelayCommand(param => IsChangingSettings = !IsChangingSettings));
            }
        }

        /// <summary>
        ///     Gets or sets the promote demote notify level.
        /// </summary>
        public int PromoteDemoteNotifyLevel
        {
            get { return promoteDemoteLevel; }

            set
            {
                promoteDemoteLevel = value;
                CallUpdate();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether promote demote notify only for interesting.
        /// </summary>
        public bool PromoteDemoteNotifyOnlyForInteresting
        {
            get { return promoteDemoteNotifyOnlyForInteresting; }

            set
            {
                promoteDemoteNotifyOnlyForInteresting = value;
                CallUpdate();
            }
        }

        #endregion

        #region Methods

        private void CallUpdate()
        {
            if (Updated != null)
                Updated(this, new EventArgs());
        }

        #endregion
    }
}