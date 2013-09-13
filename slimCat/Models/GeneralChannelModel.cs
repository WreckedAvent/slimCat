// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralChannelModel.cs" company="Justin Kadrovach">
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
//   The general channel model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    ///     The general channel model.
    /// </summary>
    public sealed class GeneralChannelModel : ChannelModel
    {
        #region Fields

        private readonly IList<string> _banned;

        private readonly IList<string> _mods;

        private readonly ObservableCollection<ICharacter> _users;

        private int _lastAdCount;

        private DateTime _lastUpdate;

        private string _motd;

        private int _userCount;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralChannelModel"/> class.
        /// </summary>
        /// <param name="channel_name">
        /// The channel_name.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="users">
        /// The users.
        /// </param>
        /// <param name="mode">
        /// The mode.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        public GeneralChannelModel(
            string channel_name, ChannelType type, int users = 0, ChannelMode mode = ChannelMode.both)
            : base(channel_name, type, mode)
        {
            try
            {
                if (users < 0)
                {
                    throw new ArgumentOutOfRangeException("users", "Users cannot be a negative number");
                }

                this.UserCount = users;

                this._users = new ObservableCollection<ICharacter>();
                this._mods = new List<string>();
                this._banned = new List<string>();

                this._settings = new ChannelSettingsModel();

                this.Users.CollectionChanged += (s, e) =>
                    {
                        if (e.Action != NotifyCollectionChangedAction.Reset)
                        {
                            this.UpdateBindings();
                        }
                    };

                // the message count now faces the user, so when we reset it it now requires a UI update
                this.Messages.CollectionChanged += (s, e) =>
                    {
                        if (e.Action == NotifyCollectionChangedAction.Reset)
                        {
                            this.LastReadCount = this.Messages.Count;
                            this.UpdateBindings();
                        }
                    };

                this.Ads.CollectionChanged += (s, e) =>
                    {
                        if (e.Action == NotifyCollectionChangedAction.Reset)
                        {
                            this.LastReadAdCount = this.Ads.Count;
                            this.UpdateBindings();
                        }
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

        /// <summary>
        ///     Gets the banned.
        /// </summary>
        public IList<string> Banned
        {
            get
            {
                return this._banned;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether can close.
        /// </summary>
        public override bool CanClose
        {
            get
            {
                return (this.ID != "Home") && this.IsSelected;
            }
        }

        /// <summary>
        ///     Gets the composite unread count.
        /// </summary>
        public int CompositeUnreadCount
        {
            get
            {
                return this.Unread + this.UnreadAds;
            }
        }

        /// <summary>
        ///     Gets the display number.
        /// </summary>
        public override int DisplayNumber
        {
            get
            {
                return this.UserCount;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is selected.
        /// </summary>
        public override bool IsSelected
        {
            get
            {
                return base.IsSelected;
            }

            set
            {
                base.IsSelected = value;
                if (value)
                {
                    this.LastReadAdCount = this.Ads.Count;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the last read ad count.
        /// </summary>
        public int LastReadAdCount
        {
            get
            {
                return this._lastAdCount;
            }

            set
            {
                if (this._lastAdCount != value)
                {
                    this._lastAdCount = value;
                    this.UpdateBindings();
                }
            }
        }

        /// <summary>
        ///     Gets or sets the motd.
        /// </summary>
        public string MOTD
        {
            get
            {
                return this._motd;
            }

            set
            {
                this._motd = value;
                this.OnPropertyChanged("MOTD");
            }
        }

        /// <summary>
        ///     Gets the moderators.
        /// </summary>
        public IList<string> Moderators
        {
            get
            {
                return this._mods;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether needs attention.
        /// </summary>
        public override bool NeedsAttention
        {
            get
            {
                if (!this.IsSelected && this._needsAttentionOverride)
                {
                    return true; // flash for ding words
                }

                if (this.Settings.MessageNotifyLevel == 0)
                {
                    return false; // terminate early upon user request
                }
                else if (this.Settings.MessageNotifyOnlyForInteresting)
                {
                    return base.NeedsAttention;
                }

                return base.NeedsAttention || (this.UnreadAds >= this.Settings.FlashInterval);
            }
        }

        /// <summary>
        ///     Gets the owner.
        /// </summary>
        public string Owner
        {
            get
            {
                if (this._mods != null)
                {
                    return this._mods[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///     Gets the unread ads.
        /// </summary>
        public int UnreadAds
        {
            get
            {
                return this.Ads.Count - this._lastAdCount;
            }
        }

        /// <summary>
        ///     Gets or sets the user count.
        /// </summary>
        public int UserCount
        {
            get
            {
                if (this.Users.Count == 0)
                {
                    return this._userCount;
                }
                else
                {
                    return this.Users.Count();
                }
            }

            set
            {
                this._userCount = value;
                this.UpdateBindings();
            }
        }

        /// <summary>
        ///     Gets the users.
        /// </summary>
        public ObservableCollection<ICharacter> Users
        {
            get
            {
                return this._users;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The add character.
        /// </summary>
        /// <param name="toAdd">
        /// The to add.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool AddCharacter(ICharacter toAdd)
        {
            if (this._users.Contains(toAdd))
            {
                return false;
            }

            this._users.Add(toAdd);
            this.CallListChanged();
            return true;
        }

        /// <summary>
        /// The add message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="isOfInterest">
        /// The is of interest.
        /// </param>
        public override void AddMessage(IMessage message, bool isOfInterest = false)
        {
            ObservableCollection<IMessage> messageCollection = message.Type == MessageType.ad ? this.Ads : this.Messages;

            while (messageCollection.Count >= ApplicationSettings.BackLogMax)
            {
                messageCollection[0].Dispose();
                messageCollection.RemoveAt(0);
            }

            messageCollection.Add(message);

            if (this.IsSelected)
            {
                if (message.Type == MessageType.normal)
                {
                    this.LastReadCount = messageCollection.Count;
                }
                else
                {
                    this.LastReadAdCount = messageCollection.Count;
                }
            }
            else if (messageCollection.Count >= ApplicationSettings.BackLogMax)
            {
                if (message.Type == MessageType.normal)
                {
                    this.LastReadCount--;
                }
                else
                {
                    this.LastReadAdCount--;
                }
            }
            else if (!this.IsSelected)
            {
                this._unreadContainsInteresting = this._unreadContainsInteresting || isOfInterest;
            }

            this.UpdateBindings();
        }

        /// <summary>
        ///     The call list changed.
        /// </summary>
        public void CallListChanged()
        {
            if (this._lastUpdate.AddSeconds(3) < DateTime.Now)
            {
                this.OnPropertyChanged("Moderators");
                this.OnPropertyChanged("Owner");
                this.OnPropertyChanged("Banned");
                this.OnPropertyChanged("Users");
                this.OnPropertyChanged("UsersCount");
                this._lastUpdate = DateTime.Now;
            }
        }

        /// <summary>
        /// The remove character.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool RemoveCharacter(string name)
        {
            ICharacter toRemove = this._users.FirstOrDefault(c => c.NameEquals(name));
            if (toRemove == null)
            {
                return false;
            }

            this._users.Remove(toRemove);
            this.CallListChanged();
            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="IsManaged">
        /// The is managed.
        /// </param>
        protected override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                this._settings = new ChannelSettingsModel();
            }

            base.Dispose(IsManaged);
        }

        /// <summary>
        ///     The update bindings.
        /// </summary>
        protected override void UpdateBindings()
        {
            base.UpdateBindings();
            this.OnPropertyChanged("CompositeUnreadCount");
        }

        #endregion
    }
}