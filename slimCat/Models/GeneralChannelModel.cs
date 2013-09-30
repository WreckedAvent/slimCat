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

namespace Slimcat.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;

    using Slimcat.Utilities;

    /// <summary>
    ///     The general channel model.
    /// </summary>
    public sealed class GeneralChannelModel : ChannelModel
    {
        #region Fields

        private readonly List<string> banned;

        private readonly List<string> mods;

        private readonly ObservableCollection<ICharacter> users;

        private string description;

        private int lastAdCount;

        private DateTime lastUpdate;

        private int userCount;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GeneralChannelModel" /> class.
        /// </summary>
        /// <param name="channelName">
        ///     The channel_name.
        /// </param>
        /// <param name="type">
        ///     The type.
        /// </param>
        /// <param name="users">
        ///     The users.
        /// </param>
        /// <param name="mode">
        ///     The mode.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        public GeneralChannelModel(
            string channelName, ChannelType type, int users = 0, ChannelMode mode = ChannelMode.Both)
            : base(channelName, type, mode)
        {
            try
            {
                if (users < 0)
                {
                    throw new ArgumentOutOfRangeException("users", "Users cannot be a negative number");
                }

                this.UserCount = users;

                this.users = new ObservableCollection<ICharacter>();
                this.mods = new List<string>();
                this.banned = new List<string>();
                this.Settings = new ChannelSettingsModel();

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
                        if (e.Action != NotifyCollectionChangedAction.Reset)
                        {
                            return;
                        }

                        this.LastReadCount = this.Messages.Count;
                        this.UpdateBindings();
                    };

                this.Ads.CollectionChanged += (s, e) =>
                    {
                        if (e.Action != NotifyCollectionChangedAction.Reset)
                        {
                            return;
                        }

                        this.LastReadAdCount = this.Ads.Count;
                        this.UpdateBindings();
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
                return this.banned;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether can close.
        /// </summary>
        public override bool CanClose
        {
            get
            {
                return (this.Id != "Home") && this.IsSelected;
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
        ///     Gets or sets the motd.
        /// </summary>
        public string Description
        {
            get
            {
                return this.description;
            }

            set
            {
                this.description = value;
                this.OnPropertyChanged("Description");
            }
        }

        /// <summary>
        ///     Gets the display number.
        /// </summary>
        public int DisplayNumber
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
                return this.lastAdCount;
            }

            set
            {
                if (this.lastAdCount == value)
                {
                    return;
                }

                this.lastAdCount = value;
                this.UpdateBindings();
            }
        }

        /// <summary>
        ///     Gets the moderators.
        /// </summary>
        public IList<string> Moderators
        {
            get
            {
                return this.mods;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether needs attention.
        /// </summary>
        public override bool NeedsAttention
        {
            get
            {
                if (!this.IsSelected && this.NeedsAttentionOverride)
                {
                    return true; // flash for ding words
                }

                if (this.Settings.MessageNotifyLevel == 0)
                {
                    return false; // terminate early upon user request
                }

                if (this.Settings.MessageNotifyOnlyForInteresting)
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
                return this.mods != null ? this.mods[0] : null;
            }
        }

        /// <summary>
        ///     Gets the unread Ads.
        /// </summary>
        public int UnreadAds
        {
            get
            {
                return this.Ads.Count - this.lastAdCount;
            }
        }

        /// <summary>
        ///     Gets or sets the user count.
        /// </summary>
        public int UserCount
        {
            get
            {
                return this.Users.Count == 0 ? this.userCount : this.Users.Count();
            }

            set
            {
                this.userCount = value;
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
                return this.users;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The add character.
        /// </summary>
        /// <param name="toAdd">
        ///     The to add.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool AddCharacter(ICharacter toAdd)
        {
            if (this.users.Contains(toAdd))
            {
                return false;
            }

            this.users.Add(toAdd);
            this.CallListChanged();
            return true;
        }

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
            var messageCollection = message.Type == MessageType.Ad ? this.Ads : this.Messages;

            while (messageCollection.Count >= ApplicationSettings.BackLogMax)
            {
                messageCollection[0].Dispose();
                messageCollection.RemoveAt(0);
            }

            messageCollection.Add(message);

            if (this.IsSelected)
            {
                if (message.Type == MessageType.Normal)
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
                if (message.Type == MessageType.Normal)
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
                this.UnreadContainsInteresting = isOfInterest;
            }

            this.UpdateBindings();
        }

        /// <summary>
        ///     The call list changed.
        /// </summary>
        public void CallListChanged()
        {
            if (this.lastUpdate.AddSeconds(3) >= DateTime.Now)
            {
                return;
            }

            this.OnPropertyChanged("Moderators");
            this.OnPropertyChanged("Owner");
            this.OnPropertyChanged("Banned");
            this.OnPropertyChanged("Users");
            this.OnPropertyChanged("UsersCount");
            this.lastUpdate = DateTime.Now;
        }

        /// <summary>
        ///     The remove character.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool RemoveCharacter(string name)
        {
            var toRemove = this.users.FirstOrDefault(c => c.NameEquals(name));
            if (toRemove == null)
            {
                return false;
            }

            this.users.Remove(toRemove);
            this.CallListChanged();
            return true;
        }

        #endregion

        #region Methods
        protected override void Dispose(bool isManaged)
        {
            this.Settings = new ChannelSettingsModel();
            base.Dispose(isManaged);
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