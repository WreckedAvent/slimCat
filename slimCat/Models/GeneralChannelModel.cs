/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Models
{
    public sealed class GeneralChannelModel : ChannelModel
    {
        #region Fields
        private ObservableCollection<ICharacter> _users;
        private IList<string> _banned;
        private string _motd;
        private int _userCount;
        private int _lastAdCount;
        private IList<string> _mods;
        #endregion

        #region Properties
        public ObservableCollection<ICharacter> Users  { get { return _users; } }
        
        public IList<string> Moderators { get { return _mods; } }

        public IList<string> Banned { get { return _banned; } }

        public string Owner { get { if (_mods != null) return _mods[0]; else return null; } }

        public string MOTD
        {
            get { return _motd; }
            set { _motd = value; OnPropertyChanged("MOTD"); }
        }

        public int UserCount
        {
            get
            {
                if (Users.Count == 0)
                    return _userCount;
                else
                    return Users.Count();
            }

            set 
            { 
                _userCount = value;
                UpdateBindings();
            }
        }

        public override int DisplayNumber
        {
            get { return UserCount; }
        }

        public override bool CanClose { get { return ((ID != "Home") && IsSelected); } }

        public override bool NeedsAttention
        {
            get
            {
                if (Settings.MessageNotifyOnlyForInteresting)
                    return base.NeedsAttention;
                else
                    return  (base.NeedsAttention || (UnreadAds >= Settings.FlashInterval));

                // this only returns true if we have more messages than the flash interval,
                // if our notify message level is greater than one,
                // and we've deteced we have someone of interest in our unread backlog
            }
        }

        public int UnreadAds { get { return Ads.Count - _lastAdCount; } }

        public int LastReadAdCount
        {
            get { return _lastAdCount; }
            set
            {
                if (_lastAdCount != value)
                {
                    _lastAdCount = value;
                    UpdateBindings();
                }
            }
        }

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
                    LastReadAdCount = Ads.Count;
            }
        }

        public int CompositeUnreadCount { get { return Unread + UnreadAds; } }
        #endregion

        #region Constructors
        public GeneralChannelModel(string channel_name, ChannelType type, int users = 0, ChannelMode mode = ChannelMode.both)
            : base(channel_name, type, mode)
        {
            try
            {
                if (users < 0) throw new ArgumentOutOfRangeException("users", "Users cannot be a negative number");
                UserCount = users;

                _users = new ObservableCollection<ICharacter>();
                _mods = new List<string>();
                _banned = new List<string>();

                _settings = new ChannelSettingsModel();

                Users.CollectionChanged += (s, e) => 
                {
                    if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                        UpdateBindings();
                };

                // the message count now faces the user, so when we reset it it now requires a UI update
                Messages.CollectionChanged += (s, e) =>
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                    {
                        LastReadCount = Messages.Count;
                        UpdateBindings();
                    }
                };

                Ads.CollectionChanged += (s, e) =>
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                    {
                        LastReadAdCount = Ads.Count;
                        UpdateBindings();
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

        #region Methods
        public override void AddMessage(IMessage message, bool isOfInterest = false)
        {
            var messageCollection = (message.Type == MessageType.ad ? Ads : Messages);

            while (messageCollection.Count >= ApplicationSettings.BackLogMax)
            {
                messageCollection[0].Dispose();
                messageCollection.RemoveAt(0);
            }

            messageCollection.Add(message);

            if (IsSelected)
            {
                if (message.Type == MessageType.normal)
                    LastReadCount = messageCollection.Count;
                else
                    LastReadAdCount = messageCollection.Count;
            }

            else if (messageCollection.Count >= ApplicationSettings.BackLogMax)
            {
                if (message.Type == MessageType.normal)
                    LastReadCount--;
                else
                    LastReadAdCount--;
            }

            else if (!IsSelected)
                _unreadContainsInteresting = _unreadContainsInteresting || isOfInterest;

            UpdateBindings();
        }

        public void CallListChanged()
        {
            OnPropertyChanged("Moderators");
            OnPropertyChanged("Owner");
            OnPropertyChanged("Banned");
            OnPropertyChanged("Users");
            OnPropertyChanged("UsersCount");
        }

        protected override void Dispose(bool IsManaged)
        {
            if (IsManaged)
                _settings = new ChannelSettingsModel();
            base.Dispose(IsManaged);
        }

        protected override void UpdateBindings()
        {
            base.UpdateBindings();
            OnPropertyChanged("CompositeUnreadCount");
        }
        #endregion
    }
}
