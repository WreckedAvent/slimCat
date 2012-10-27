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
        private bool _newMessage;
        private IList<string> _mods;
        // used as an abstraction away from the user collection (so we know how many are in without it being set)
        #endregion

        #region Properties
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

        public override bool NeedsAttention
        {
            get { return _newMessage; }
        }

        public override int DisplayNumber
        {
            get { return UserCount; }
        }

        public override bool IsSelected
        {
            get
            {
                return base.IsSelected;
            }
            set
            {
                if (base.IsSelected != value)
                {
                    base.IsSelected = value;
                    if (value == true)
                    {
                        _newMessage = false;
                    }
                    UpdateBindings();
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public override bool CanClose { get { return ((ID != "Home") && IsSelected); } }
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
                _newMessage = false;
                _mods = new List<string>();

                Messages.CollectionChanged += (s, e) =>
                    {
                        if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                        {
                            if (!IsSelected)
                            {
                                _newMessage = true;
                                UpdateBindings();
                            }
                        }
                    };

                Ads.CollectionChanged += (s, e) =>
                    {
                        if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                        {
                            if (!IsSelected)
                            {
                                _newMessage = true;
                                UpdateBindings();
                            }
                        }
                    };

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
            var messageCollection = (message.Type == MessageType.normal ? Messages : Ads);

            if (messageCollection.Count > 300)
                messageCollection.RemoveAt(0);

            messageCollection.Add(message);
        }
        #endregion
    }
}
