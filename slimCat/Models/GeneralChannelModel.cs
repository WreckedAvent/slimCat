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
        private string _motd;
        private ICharacter _owner;
        private int _userCount;
        private int _lastAdCount;
        private IList<string> _mods;
        #endregion

        #region Properties
        // used as an abstraction away from the user collection (so we know how many are in without it being set)
        public ObservableCollection<ICharacter> Users  { get { return _users; } }
        
        public IList<string> Moderators { get { return _mods; } }

        public string MOTD
        {
            get { return _motd; }
            set { _motd = value; OnPropertyChanged("MOTD"); }
        }

        public ICharacter Owner
        {
            get { return _owner; }
            set { _owner = value; OnPropertyChanged("Owner"); }
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

        public override bool NeedsAttention // this includes ads
        {
            get
            {
                return base.NeedsAttention || (UnreadAds >= Settings.FlashInterval && Settings.ShouldFlash);
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

                _settings = new ChannelSettingsModel();
                Users.CollectionChanged += (s, e) => { if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset) UpdateBindings(); };
            }

            catch (Exception ex)
            {
                ex.Source = "General Channel Model, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        public override void AddMessage(IMessage message)
        {
            var messageCollection = (message.Type == MessageType.ad ? Ads : Messages);

            while (messageCollection.Count > ApplicationSettings.BackLogMax)
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

            UpdateBindings();
        }

        protected override void Dispose(bool IsManaged)
        {
            if (IsManaged)
                _settings = new ChannelSettingsModel();
            base.Dispose(IsManaged);
        }
        #endregion
    }
}
