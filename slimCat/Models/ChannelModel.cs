// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelModel.cs" company="Justin Kadrovach">
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
//   The channel model is used as a base for channels and conversations
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Models
{
    using System;
    using System.Collections.ObjectModel;

    using Utilities;
    using ViewModels;

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
                this.Type = kind;
                this.Mode = mode;
                this.LastReadCount = 0;
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
            get
            {
                return this.ads;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether can close.
        /// </summary>
        public virtual bool CanClose
        {
            get
            {
                return this.IsSelected;
            }
        }

        /// <summary>
        ///     An ID is used to unambigiously identify the channel or character's name
        /// </summary>
        public string Id
        {
            get
            {
                return this.identity;
            }
        }

        /// <summary>
        ///     If the channel is selected or not
        /// </summary>
        public virtual bool IsSelected
        {
            get
            {
                return this.isSelected;
            }

            set
            {
                if (this.isSelected == value)
                {
                    return;
                }

                this.isSelected = value;

                if (value)
                {
                    this.LastReadCount = this.Messages.Count;
                }

                this.NeedsAttentionOverride = false;
                this.UnreadContainsInteresting = false;

                this.UpdateBindings();
                this.OnPropertyChanged("IsSelected");
            }
        }

        /// <summary>
        ///     Gets the messages.
        /// </summary>
        public ObservableCollection<IMessage> Messages
        {
            get
            {
                return this.messages;
            }
        }

        /// <summary>
        ///     Gets or sets the mode.
        /// </summary>
        public ChannelMode Mode
        {
            get
            {
                return this.mode;
            }

            set
            {
                this.mode = value;
                this.OnPropertyChanged("Mode");
            }
        }

        /// <summary>
        ///     Used to determine if the channel should make itself more visible on the UI
        /// </summary>
        public virtual bool NeedsAttention
        {
            get
            {
                if (!this.IsSelected && this.NeedsAttentionOverride)
                {
                    return true; // flash if we have a ding word
                }

                if (this.Settings.MessageNotifyLevel == 0)
                {
                    return false; // if we don't want any flashes then terminate
                }

                if (this.Settings.MessageNotifyOnlyForInteresting)
                {
                    return this.UnreadContainsInteresting;
                }

                return !this.IsSelected && (this.Unread >= this.Settings.FlashInterval);
            }
        }

        /// <summary>
        ///     Gets or sets the settings.
        /// </summary>
        public ChannelSettingsModel Settings
        {
            get
            {
                return this.settings;
            }

            set
            {
                this.settings = value;
                this.UpdateBindings();
            }
        }

        /// <summary>
        ///     Gets or sets the title.
        /// </summary>
        public string Title
        {
            get
            {
                return this.title ?? this.Id;
            }

            set
            {
                this.title = value;
                this.OnPropertyChanged("Title");
            }
        }

        /// <summary>
        ///     Gets or sets the type.
        /// </summary>
        public ChannelType Type
        {
            get
            {
                return this.type;
            }

            set
            {
                this.type = value;
                this.OnPropertyChanged("Type");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The number of messages we've read up to
        /// </summary>
        protected int LastReadCount
        {
            get
            {
                return this.lastRead;
            }

            set
            {
                if (this.lastRead == value)
                {
                    return;
                }

                this.lastRead = value;
                this.UpdateBindings();
            }
        }

        protected bool NeedsAttentionOverride { get; private set; }

        /// <summary>
        ///     Number of messages we haven't read
        /// </summary>
        protected int Unread
        {
            get
            {
                return this.Messages.Count - this.LastReadCount;
            }
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
            while (this.messages.Count >= ApplicationSettings.BackLogMax)
            {
                this.messages[0].Dispose();
                this.messages[0] = null;
                this.messages.RemoveAt(0);
            }

            this.messages.Add(message);

            if (this.isSelected)
            {
                this.lastRead = this.messages.Count;
            }
            else if (this.messages.Count == ApplicationSettings.BackLogMax)
            {
                this.UnreadContainsInteresting = this.UnreadContainsInteresting || isOfInterest;
                this.lastRead--;
            }

            this.UpdateBindings();
        }

        /// <summary>
        ///     The flash tab.
        /// </summary>
        public void FlashTab()
        {
            this.NeedsAttentionOverride = true;
            this.UpdateBindings();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this.messages.Clear();
                this.ads.Clear();
                this.settings = new ChannelSettingsModel();
            }

            base.Dispose(isManaged);
        }

        /// <summary>
        ///     Updates the bound data so the UI can react accordingly
        /// </summary>
        protected virtual void UpdateBindings()
        {
            this.OnPropertyChanged("NeedsAttention");
            this.OnPropertyChanged("DisplayNumber");
            this.OnPropertyChanged("CanClose");
            this.OnPropertyChanged("Settings");
        }

        #endregion
    }
}