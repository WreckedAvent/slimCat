#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralChannelModel.cs">
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
    using System.Collections.Specialized;
    using Utilities;

    #endregion

    /// <summary>
    ///     The general channel model.
    /// </summary>
    public sealed class GeneralChannelModel : ChannelModel
    {
        #region Fields

        private string description;

        private int lastAdCount;

        private int userCount;

        #endregion

        #region Constructors and Destructors

        public GeneralChannelModel(
            string channelName, ChannelType type, int users = 0, ChannelMode mode = ChannelMode.Both)
            : base(channelName, type, mode)
        {
            try
            {
                if (users < 0)
                    throw new ArgumentOutOfRangeException("users", "Users cannot be a negative number");

                UserCount = users;

                CharacterManager = new ChannelCharacterManager();
                Settings = new ChannelSettingsModel();

                // the message count now faces the user, so when we reset it it now requires a UI update
                Messages.CollectionChanged += (s, e) =>
                {
                    if (e.Action != NotifyCollectionChangedAction.Reset)
                        return;

                    LastReadCount = Messages.Count;
                    UpdateBindings();
                };

                Ads.CollectionChanged += (s, e) =>
                {
                    if (e.Action != NotifyCollectionChangedAction.Reset)
                        return;

                    LastReadAdCount = Ads.Count;
                    UpdateBindings();
                };
            }
            catch (Exception ex)
            {
                ex.Source = "General Channel Model, init";
                Exceptions.HandleException(ex);
            }
        }

        public GeneralChannelModel(string name, string title, ChannelType type)
            : base(name, type, ChannelMode.Both)
        {
            try
            {
                UserCount = 0;

                CharacterManager = new ChannelCharacterManager();
                Settings = new ChannelSettingsModel();
                Title = title;

                // the message count now faces the user, so when we reset it it now requires a UI update
                Messages.CollectionChanged += (s, e) =>
                {
                    if (e.Action != NotifyCollectionChangedAction.Reset)
                        return;

                    LastReadCount = Messages.Count;
                    UpdateBindings();
                };

                Ads.CollectionChanged += (s, e) =>
                {
                    if (e.Action != NotifyCollectionChangedAction.Reset)
                        return;

                    LastReadAdCount = Ads.Count;
                    UpdateBindings();
                };
            }
            catch (Exception ex)
            {
                ex.Source = "General Channel Model, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        public ICharacterManager CharacterManager { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether can close.
        /// </summary>
        public override bool CanClose
        {
            get { return (Id != "Home") && IsSelected; }
        }

        /// <summary>
        ///     Gets the composite unread count.
        /// </summary>
        public int CompositeUnreadCount
        {
            get { return Math.Max(Unread + UnreadAds, 0); }
        }

        /// <summary>
        ///     Gets or sets the motd.
        /// </summary>
        public string Description
        {
            get { return description; }

            set
            {
                description = value;
                OnPropertyChanged("Description");
            }
        }

        public bool ShowChannelDescription
        {
            get { return description != null && description.Replace("\r\n", "\n") != Settings.LastChannelDescription; }
        }

        /// <summary>
        ///     Gets the display number.
        /// </summary>
        public string DisplayNumber
        {
            get { return UserCount > 0 ? userCount.ToString() : string.Empty; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is selected.
        /// </summary>
        public override bool IsSelected
        {
            get { return base.IsSelected; }

            set
            {
                base.IsSelected = value;
                if (!value)
                    LastReadAdCount = Ads.Count;
            }
        }

        /// <summary>
        ///     Gets or sets the last read ad count.
        /// </summary>
        public int LastReadAdCount
        {
            get { return lastAdCount; }

            set
            {
                if (lastAdCount == value)
                    return;

                lastAdCount = value;
                UpdateBindings();
            }
        }

        /// <summary>
        ///     Gets a value indicating whether needs attention.
        /// </summary>
        public override bool NeedsAttention
        {
            get
            {
                if (!IsSelected && NeedsAttentionOverride)
                    return true; // flash for ding words

                if (Messages.Count == 0 && Ads.Count == 0) return false;

                var messageNotifyMatters = Mode == ChannelMode.Chat || Mode == ChannelMode.Both;
                var adNotifyMatters = Mode == ChannelMode.Ads || Mode == ChannelMode.Both;

                var doNotFlash = true;

                if (messageNotifyMatters)
                    doNotFlash = Settings.MessageNotifyLevel == 0;

                if (adNotifyMatters)
                    doNotFlash = doNotFlash && Settings.AdNotifyLevel == 0;

                if (doNotFlash)
                    return false; // terminate early upon user request

                // base.NeedsAttention will check if our messages are of interest, but not ads
                if (Settings.MessageNotifyOnlyForInteresting)
                    return base.NeedsAttention;

                if (adNotifyMatters)
                    return base.NeedsAttention || UnreadAds >= 1;

                return base.NeedsAttention;
            }
        }

        /// <summary>
        ///     Gets the unread Ads.
        /// </summary>
        public int UnreadAds
        {
            get { return Ads.Count - lastAdCount; }
        }

        /// <summary>
        ///     Gets or sets the user count.
        /// </summary>
        public int UserCount
        {
            get { return CharacterManager.CharacterCount == 0 ? userCount : CharacterManager.CharacterCount; }

            set
            {
                userCount = value;
                UpdateBindings();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The add message.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <param name="isOfInterest">
        ///     The is of interest.
        /// </param>
        public override void AddMessage(IMessage message, bool isOfInterest = false)
        {
            var messageCollection = message.Type == MessageType.Ad ? Ads : Messages;

            messageCollection.Backlog(message, Settings.MaxBackLogItems);

            if (IsSelected)
            {
                if (message.Type == MessageType.Normal)
                    LastReadCount = messageCollection.Count;
                else
                    LastReadAdCount = messageCollection.Count;
            }
            else if (messageCollection.Count >= Settings.MaxBackLogItems)
            {
                if (message.Type == MessageType.Normal)
                    LastReadCount--;
                else
                    LastReadAdCount--;
            }
            else if (!IsSelected)
                UnreadContainsInteresting = isOfInterest;

            UpdateBindings();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            Settings = new ChannelSettingsModel();
            base.Dispose(isManaged);
        }

        /// <summary>
        ///     The update bindings.
        /// </summary>
        protected override void UpdateBindings()
        {
            base.UpdateBindings();
            OnPropertyChanged("CompositeUnreadCount");
        }

        #endregion
    }
}