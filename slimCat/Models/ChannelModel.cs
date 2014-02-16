#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelModel.cs">
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
    using System.Collections.ObjectModel;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     The channel model is used as a base for channels and conversations
    /// </summary>
    public abstract class ChannelModel : SysProp
    {
        #region Fields

        private readonly ObservableCollection<IMessage> ads = new ObservableCollection<IMessage>();

        private readonly string identity;

        private readonly ObservableCollection<IMessage> messages = new ObservableCollection<IMessage>();

        private bool isSelected;

        private int lastRead;

        private ChannelMode mode;

        private ChannelSettingsModel settings;

        private string title;

        private ChannelType type;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelModel" /> class.
        ///     Creates a new Channel data model
        /// </summary>
        /// <param name="identity">
        ///     Name of the channel (or character, for Pms)
        /// </param>
        /// <param name="kind">
        ///     Type of the channel
        /// </param>
        /// <param name="mode">
        ///     The rules associated with the channel (only Ads, only posts, or both)
        /// </param>
        protected ChannelModel(string identity, ChannelType kind, ChannelMode mode = ChannelMode.Both)
        {
            try
            {
                this.identity = identity.ThrowIfNull("identity");
                Type = kind;
                Mode = mode;
                LastReadCount = 0;
            }
            catch (Exception ex)
            {
                ex.Source = "Channel Model, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the Ads.
        /// </summary>
        public ObservableCollection<IMessage> Ads
        {
            get { return ads; }
        }

        /// <summary>
        ///     Gets a value indicating whether can close.
        /// </summary>
        public virtual bool CanClose
        {
            get { return IsSelected; }
        }

        /// <summary>
        ///     An ID is used to unambigiously identify the channel or character's name
        /// </summary>
        public string Id
        {
            get { return identity; }
        }

        /// <summary>
        ///     If the channel is selected or not
        /// </summary>
        public virtual bool IsSelected
        {
            get { return isSelected; }

            set
            {
                if (isSelected == value)
                    return;

                isSelected = value;

                if (value)
                    LastReadCount = Messages.Count;

                NeedsAttentionOverride = false;
                UnreadContainsInteresting = false;

                UpdateBindings();
                OnPropertyChanged("IsSelected");
            }
        }

        /// <summary>
        ///     Gets the messages.
        /// </summary>
        public ObservableCollection<IMessage> Messages
        {
            get { return messages; }
        }

        /// <summary>
        ///     Gets or sets the mode.
        /// </summary>
        public ChannelMode Mode
        {
            get { return mode; }

            set
            {
                mode = value;
                OnPropertyChanged("Mode");
            }
        }

        /// <summary>
        ///     Used to determine if the channel should make itself more visible on the UI
        /// </summary>
        public virtual bool NeedsAttention
        {
            get
            {
                if (!IsSelected && NeedsAttentionOverride)
                    return true; // flash if we have a ding word

                if (Settings.MessageNotifyLevel == 0)
                    return false; // if we don't want any flashes then terminate

                if (Settings.MessageNotifyOnlyForInteresting)
                    return UnreadContainsInteresting;

                return !IsSelected && (Unread >= Settings.FlashInterval);
            }
        }

        /// <summary>
        ///     Gets or sets the settings.
        /// </summary>
        public ChannelSettingsModel Settings
        {
            get { return settings; }

            set
            {
                settings = value;
                UpdateBindings();
            }
        }

        /// <summary>
        ///     Gets or sets the title.
        /// </summary>
        public string Title
        {
            get { return title ?? Id; }

            set
            {
                title = value;
                OnPropertyChanged("Title");
            }
        }

        /// <summary>
        ///     Gets or sets the type.
        /// </summary>
        public ChannelType Type
        {
            get { return type; }

            set
            {
                type = value;
                OnPropertyChanged("Type");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The number of messages we've read up to
        /// </summary>
        protected int LastReadCount
        {
            get { return lastRead; }

            set
            {
                if (lastRead == value)
                    return;

                lastRead = value;
                UpdateBindings();
            }
        }

        protected bool NeedsAttentionOverride { get; private set; }

        /// <summary>
        ///     Number of messages we haven't read
        /// </summary>
        protected int Unread
        {
            get { return Math.Max(Messages.Count - LastReadCount, 0); }
        }

        protected bool UnreadContainsInteresting { private get; set; }

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
        public virtual void AddMessage(IMessage message, bool isOfInterest = false)
        {
            messages.Backlog(message, settings.MaxBackLogItems);

            if (isSelected)
                lastRead = messages.Count;
            else if (messages.Count == settings.MaxBackLogItems)
            {
                UnreadContainsInteresting = UnreadContainsInteresting || isOfInterest;
                lastRead--;
            }

            UpdateBindings();
        }

        /// <summary>
        ///     The flash tab.
        /// </summary>
        public void FlashTab()
        {
            NeedsAttentionOverride = true;
            UpdateBindings();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                messages.Clear();
                ads.Clear();
                settings = new ChannelSettingsModel();
            }

            base.Dispose(isManaged);
        }

        /// <summary>
        ///     Updates the bound data so the UI can react accordingly
        /// </summary>
        protected virtual void UpdateBindings()
        {
            OnPropertyChanged("NeedsAttention");
            OnPropertyChanged("DisplayNumber");
            OnPropertyChanged("CanClose");
            OnPropertyChanged("Settings");
        }

        #endregion
    }
}