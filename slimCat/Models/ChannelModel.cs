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

namespace Models
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    ///     The channel model is used as a base for channels and conversations
    /// </summary>
    public abstract class ChannelModel : SysProp, IDisposable
    {
        #region Fields

        internal bool _unreadContainsInteresting;

        /// <summary>
        ///     The _ads.
        /// </summary>
        protected ObservableCollection<IMessage> _ads = new ObservableCollection<IMessage>();

        /// <summary>
        ///     The _history.
        /// </summary>
        protected ObservableCollection<string> _history = new ObservableCollection<string>();

        /// <summary>
        ///     The _messages.
        /// </summary>
        protected ObservableCollection<IMessage> _messages = new ObservableCollection<IMessage>();

        /// <summary>
        ///     The _needs attention override.
        /// </summary>
        protected bool _needsAttentionOverride = false;

        /// <summary>
        ///     The _settings.
        /// </summary>
        protected ChannelSettingsModel _settings;

        private readonly string _identity; // an ID never changes

        private bool _isSelected;

        private int _lastRead;

        private ChannelMode _mode;

        private string _title;

        private ChannelType _type;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelModel"/> class.
        ///     Creates a new Channel data model
        /// </summary>
        /// <param name="identity">
        /// Name of the channel (or character, for PMs)
        /// </param>
        /// <param name="kind">
        /// Type of the channel
        /// </param>
        /// <param name="mode">
        /// The rules associated with the channel (only ads, only posts, or both)
        /// </param>
        public ChannelModel(string identity, ChannelType kind, ChannelMode mode = ChannelMode.both)
        {
            try
            {
                this._identity = identity.ThrowIfNull("identity");
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
        ///     Gets the ads.
        /// </summary>
        public ObservableCollection<IMessage> Ads
        {
            get
            {
                return this._ads;
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
        ///     A number displayed on the UI along with the rest of the channel data
        /// </summary>
        public abstract int DisplayNumber { get; }

        /// <summary>
        ///     Gets the history.
        /// </summary>
        public ObservableCollection<string> History
        {
            get
            {
                return this._history;
            }
        }

        /// <summary>
        ///     An ID is used to unambigiously identify the channel or character's name
        /// </summary>
        public string ID
        {
            get
            {
                return this._identity;
            }
        }

        /// <summary>
        ///     If the channel is selected or not
        /// </summary>
        public virtual bool IsSelected
        {
            get
            {
                return this._isSelected;
            }

            set
            {
                if (this._isSelected != value)
                {
                    this._isSelected = value;

                    if (value)
                    {
                        this.LastReadCount = this.Messages.Count;
                    }

                    this._needsAttentionOverride = false;
                    this._unreadContainsInteresting = false;

                    this.UpdateBindings();
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        ///     The number of messages we've read up to
        /// </summary>
        public virtual int LastReadCount
        {
            get
            {
                return this._lastRead;
            }

            set
            {
                if (this._lastRead != value)
                {
                    this._lastRead = value;
                    this.UpdateBindings();
                }
            }
        }

        /// <summary>
        ///     Gets the messages.
        /// </summary>
        public ObservableCollection<IMessage> Messages
        {
            get
            {
                return this._messages;
            }
        }

        /// <summary>
        ///     Gets or sets the mode.
        /// </summary>
        public ChannelMode Mode
        {
            get
            {
                return this._mode;
            }

            set
            {
                this._mode = value;
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
                if (!this.IsSelected && this._needsAttentionOverride)
                {
                    return true; // flash if we have a ding word
                }

                if (this.Settings.MessageNotifyLevel == 0)
                {
                    return false; // if we don't want any flashes then terminate
                }
                else if (this.Settings.MessageNotifyOnlyForInteresting)
                {
                    return this._unreadContainsInteresting;
                }

                return !this.IsSelected && (this.Unread >= this.Settings.FlashInterval);
            }
        }

        /// <summary>
        ///     Gets or sets the settings.
        /// </summary>
        public virtual ChannelSettingsModel Settings
        {
            get
            {
                return this._settings;
            }

            set
            {
                this._settings = value;
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
                return this._title == null ? this.ID : this._title;
            }

            set
            {
                this._title = value;
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
                return this._type;
            }

            set
            {
                this._type = value;
                this.OnPropertyChanged("Type");
            }
        }

        /// <summary>
        ///     Number of messages we haven't read
        /// </summary>
        public int Unread
        {
            get
            {
                return this.Messages.Count - this.LastReadCount;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The add message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="isOfInterest">
        /// The is of interest.
        /// </param>
        public virtual void AddMessage(IMessage message, bool isOfInterest = false)
        {
            while (this._messages.Count >= ApplicationSettings.BackLogMax)
            {
                this._messages[0].Dispose();
                this._messages[0] = null;
                this._messages.RemoveAt(0);
            }

            this._messages.Add(message);

            if (this._isSelected)
            {
                this._lastRead = this._messages.Count;
            }
            else if (this._messages.Count == ApplicationSettings.BackLogMax)
            {
                this._unreadContainsInteresting = this._unreadContainsInteresting || isOfInterest;
                this._lastRead--;
            }

            this.UpdateBindings();
        }

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        ///     The flash tab.
        /// </summary>
        public virtual void FlashTab()
        {
            this._needsAttentionOverride = this._needsAttentionOverride || true;
            this.UpdateBindings();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="IsManaged">
        /// The is managed.
        /// </param>
        protected virtual void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                this._messages.Clear();
                this._ads.Clear();
            }
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

    /// <summary>
    ///     Represents the possible channel types
    /// </summary>
    public enum ChannelType
    {
        /* Official Channels */

        /// <summary>
        ///     The pub.
        /// </summary>
        pub, 

        // pub channels are official channels which are open to the public and abide by F-list's rules and moderation.

        /* Private Channels */

        /// <summary>
        ///     The priv.
        /// </summary>
        priv, 

        // priv channels are private channels which are open to the public

        /// <summary>
        ///     The pm.
        /// </summary>
        pm, 

        // pm channels are private channels which can only be accessed by two characters

        /// <summary>
        ///     The closed.
        /// </summary>
        closed, 

        // closed channels are private channels which can only be joined with an outstanding invite

        /// <summary>
        ///     The utility.
        /// </summary>
        utility, 

        // utility channels are channels which have custom functionality, such as the home page
    }

    /// <summary>
    ///     Represents possible channel modes
    /// </summary>
    public enum ChannelMode
    {
        /// <summary>
        ///     The ads.
        /// </summary>
        ads, 

        // ad-only channels, e.g LfRP

        /// <summary>
        ///     The chat.
        /// </summary>
        chat, 

        // no-ad channels, e.g. most private channels

        /// <summary>
        ///     The both.
        /// </summary>
        both, 

        // both messages and ads, e.g most public channels
    }
}