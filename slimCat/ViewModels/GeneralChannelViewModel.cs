using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using Views;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ViewModels
{
    public class GeneralChannelViewModel : ChannelViewModelBase, IDisposable
    {
        #region Fields
        private bool _isDisplayingChat;
        private bool _isSearching = false;
        private bool _autoPostAds = false;
        private GenderSettingsModel _genderSettings = new GenderSettingsModel();
        private GenericSearchSettingsModel _searchSettings = new GenericSearchSettingsModel();
        private string _adMessage = "";

        private System.Timers.Timer _messageFlood = new System.Timers.Timer(500);
        private System.Timers.Timer _adFlood = new System.Timers.Timer(602000);
        private System.Timers.Timer _update = new System.Timers.Timer(1000);

        private bool _isInCoolDownAd = false;
        private bool _isInCoolDownMessage = false;
        private DateTimeOffset _timeLeftAd;

        public event EventHandler NewAdArrived;
        public event EventHandler NewMessageArrived;
        #endregion

        #region Properties
        public bool ShouldDisplayAds { get { return ((Model.Mode == ChannelMode.both) || (Model.Mode == ChannelMode.ads)); } }
        public bool ShouldDisplayChat { get { return ((Model.Mode == ChannelMode.both) || (Model.Mode == ChannelMode.chat)); } }

        public IEnumerable<IMessage> FilteredMessages
        {
            get
            {
                #region Filter Functions
                Func<IMessage, bool> ContainsSearchString = (message => message.Message.ToLower().Contains(_searchSettings.SearchString)
                    || message.Poster.Name.ToLower().Contains(_searchSettings.SearchString));

                Func<IMessage, bool> MeetsGenderFilter = 
                    (message => !GenderSettings.FilteredGenders.Any(genders => message.Poster.Gender == genders));

                Func<IMessage, bool> MeetsFriendFilter =
                    (message => SearchSettings.ShowFriends && CM.OnlineFriends.Contains(message.Poster));

                Func<IMessage, bool> MeetsBookmarkFilter =
                    (message => SearchSettings.ShowBookmarks && CM.OnlineBookmarks.Contains(message.Poster));

                Func<IMessage, bool> MeetsModFilter =
                    (message => SearchSettings.ShowMods && CM.OnlineGlobalMods.Contains(message.Poster));

                Func<IMessage, bool> MeetsFilter =
                    (message =>
                        (ContainsSearchString(message)
                        && MeetsGenderFilter(message))
                    &&
                        (SearchSettings.MeetsStatusFilter(message.Poster)
                        || MeetsFriendFilter(message)
                        || MeetsBookmarkFilter(message)
                        || MeetsModFilter(message))
                    );
                #endregion

                if (IsDisplayingChat)
                    return Model.Messages.Where(MeetsFilter);

                else
                    return Model.Ads.Where(MeetsFilter);
            }
        }

        public ObservableCollection<IMessage> CurrentChat
        {
            get
            {
                if (IsDisplayingChat) return Model.Messages;
                else return Model.Ads;
            }
        }

        public GenderSettingsModel GenderSettings { get { return _genderSettings; } }
        public GenericSearchSettingsModel SearchSettings { get { return _searchSettings; } }

        #region Interface-binding Properties
        // Used for ad-displaying tool chain
        public bool IsDisplayingChat
        { 
            get { return _isDisplayingChat; }
            set
            {
                if (_isDisplayingChat != value)
                {
                    _isDisplayingChat = value;

                    string temp = Message;
                    Message = _adMessage;
                    _adMessage = temp;

                    OnPropertyChanged("IsDisplayingChat");
                    OnPropertyChanged("IsDisplayingAds");
                    OnPropertyChanged("ChatContentString");
                    OnPropertyChanged("CurrentChat");
                    OnPropertyChanged("MessageMax");
                    OnPropertyChanged("CanPost");
                    OnPropertyChanged("CannotPost");
                    OnPropertyChanged("ShouldShowAutoPost");
                    OnPropertyChanged("SwitchChannelTypeString");
                }
            }
        }

        public bool IsDisplayingAds { get { return !IsDisplayingChat; } }

        public bool CanSwitch
        {
            get
            {
                if (IsDisplayingChat && ShouldDisplayAds)
                    return true;
                else if (!IsDisplayingChat && ShouldDisplayChat)
                    return true;
                else
                    return false;
            }
        }

        // Used for searching tool chain
        public bool IsSearching
        {
            get { return _isSearching; }
            set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;
                    OnPropertyChanged("IsSearching");
                    OnPropertyChanged("SearchSwitchMessageString");
                    OnPropertyChanged("IsChatting");
                }
            }
        }

        public bool IsChatting
        {
            get { return !IsSearching; }
        }

        public string ChannelTypeString 
        {
            get
            {
                switch (Model.Type)
                {
                    case ChannelType.pub: return "(Official) ";
                    case ChannelType.priv: return "(Public) ";
                    case ChannelType.closed: return "(Private) ";
                }

                return " ";
            }
        }

        public string SwitchChannelTypeString { get { if (IsDisplayingChat) return "View Ads..."; else return "View Chat..."; } }
        // Used for the button that switches between ads and chat

        public string ChatContentString { get { if (IsDisplayingChat) return "Chat"; else return "Ads"; } }
        // Used for 
        public string SearchSwitchMessageString { get { if (IsSearching) return "Chat in this ..."; else return "Search in this ..."; } }

        public string MessageMax { get { if (IsDisplayingChat) return "4,096 characters"; else return "50,000 characters"; } }

        public string TimeLeft
        {
            get
            {
                return HelperConverter.DateTimeInFutureToRough(_timeLeftAd) + "left";
            }
        }

        public bool CanPost
        {
            get
            {
                return ((IsDisplayingChat && !_isInCoolDownMessage) || (IsDisplayingAds && !_isInCoolDownAd));
            }
        }

        public bool CannotPost
        {
            get { return !CanPost; }
        }

        public bool ShouldShowPostLength
        {
            get { return (Message != null && Message.Length > 0) && (IsDisplayingChat || (IsDisplayingAds && !AutoPost)); } 
        }

        public bool ShouldShowAutoPost
        {
            get
            {
                if (!_isInCoolDownAd)
                    return IsDisplayingAds;
                else
                    return (IsDisplayingAds && Message != null && Message.Length > 0);
            }
        }

        public bool AutoPost
        {
            get { return _autoPostAds; }
            set 
            {
                _autoPostAds = value; 
                OnPropertyChanged("AutoPost"); 
            }
        }

        public string MOTD
        {
            get
            {
                return ((GeneralChannelModel)Model).MOTD;
            }
        }
        #endregion
        #endregion

        #region Constructors
        public GeneralChannelViewModel(string name, IUnityContainer contain, IRegionManager regman,
                                       IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                if (CM.CurrentChannels.Any(chan => chan.ID == name))
                    Model = CM.CurrentChannels.First(chan => chan.ID == name);
                else
                    Model = CM.AllChannels.First(chan => chan.ID == name);

                _container.RegisterType<object, GeneralChannelView>(name, new InjectionConstructor(this));

                _isDisplayingChat = ShouldDisplayChat;

                Model.Messages.CollectionChanged += OnMessagesChanged;
                Model.Ads.CollectionChanged += OnAdsChanged;
                Model.PropertyChanged += OnModelPropertyChanged;

                #region disposable
                _genderSettings.Updated += (s, e) =>
                    {
                        OnPropertyChanged("FilteredMessages");
                        OnPropertyChanged("GenderSettings");
                    };

                _searchSettings.Updated += (s, e) =>
                    {
                        OnPropertyChanged("SearchSettings");
                        OnPropertyChanged("FilteredMessages");
                    };

                _messageFlood.Elapsed += (s, e) =>
                {
                    _isInCoolDownMessage = false;
                    _messageFlood.Enabled = false;
                    OnPropertyChanged("CanPost");
                };

                _adFlood.Elapsed += (s, e) =>
                {
                    _isInCoolDownAd = false;
                    _adFlood.Enabled = false;
                    OnPropertyChanged("CanPost");
                    OnPropertyChanged("CannotPost");
                    OnPropertyChanged("ShouldShowAutoPost");
                    if (_autoPostAds) SendAutoAd();
                };

                bool temp = ShouldShowPostLength;

                _update.Elapsed += (s, e) =>
                {
                    if (Model.IsSelected)
                    {
                        if (CannotPost)
                            OnPropertyChanged("TimeLeft");

                        if (ShouldShowPostLength != temp)
                        {
                            OnPropertyChanged("ShouldShowPostLength");
                            temp = ShouldShowPostLength;
                        }
                    }
                };
                #endregion

                _update.Enabled = true;
            }

            catch (Exception ex)
            {
                ex.Source = "General Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        protected override void SendMessage()
        {
            if (!IsSearching) // if we're not searching, treat this input normal
            {
                if ((IsDisplayingChat && Message.Length > 4096) || (IsDisplayingAds && Message.Length > 50000))
                {
                    UpdateError("You expect me to post all of that?! How about you post less, huh?");
                    return;
                }

                if ((_isInCoolDownAd && IsDisplayingAds) || (_isInCoolDownMessage && IsDisplayingChat))
                {
                    UpdateError("Cool your engines. Wait a little before you post again.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Message))
                {
                    UpdateError("I'm sure you didn't mean to do that.");
                    return;
                }

                string command = (IsDisplayingChat ? CommandDefinitions.ClientSendChannelMessage : CommandDefinitions.ClientSendChannelAd);

                IDictionary<string, object> toSend = CommandDefinitions
                    .CreateCommand(command, new List<string>() { this.Message }, Model.ID)
                    .toDictionary();

                _events.GetEvent<UserCommandEvent>().Publish(toSend);

                if (!_autoPostAds || IsDisplayingChat) this.Message = null;

                if (IsDisplayingChat)
                {
                    _isInCoolDownMessage = true;
                    OnPropertyChanged("CanPost");

                    _messageFlood.Enabled = _messageFlood.Enabled || true;
                }

                else
                {
                    _timeLeftAd = DateTime.Now.AddMinutes(10).AddSeconds(2);

                    _isInCoolDownAd = true;
                    OnPropertyChanged("CanPost");
                    OnPropertyChanged("CannotPost");
                    OnPropertyChanged("ShouldShowAutoPost");

                    _adFlood.Enabled = _adFlood.Enabled || true;
                }
            }
        }

        public void SendAutoAd()
        {
            string messageToSend = (IsDisplayingChat ? _adMessage : Message);
            if (messageToSend == null) UpdateError("There is no ad to auto-post!");

            IDictionary<string, object> toSend = CommandDefinitions
                .CreateCommand(CommandDefinitions.ClientSendChannelAd, new List<string>() { messageToSend }, Model.ID)
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(toSend);
            _timeLeftAd = DateTimeOffset.Now.AddMinutes(10).AddSeconds(2);

            _isInCoolDownAd = true;
            OnPropertyChanged("CanPost");
            OnPropertyChanged("CannotPost");
        }

        #region Commands
        RelayCommand _switch;
        public ICommand SwitchCommand
        {
            get
            {
                if (_switch == null)
                    _switch = new RelayCommand( param => IsDisplayingChat = !IsDisplayingChat);

                return _switch;
            }
        }

        RelayCommand _switchSearch;
        public ICommand SwitchSearchCommand
        {
            get
            {
                if (_switchSearch == null)
                    _switchSearch = new RelayCommand(param => 
                        {
                            OnPropertyChanged("FilteredMessages");
                            IsSearching = !IsSearching;
                        });

                return _switchSearch;
            }
        }
        #endregion

        #region Event methods
        private void OnAdsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsDisplayingAds && Model.IsSelected)
            {
                OnPropertyChanged("FilteredMessages");
                if (NewAdArrived != null)
                    NewAdArrived(this, new EventArgs());
            }
        }

        private void OnMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            if (IsDisplayingChat && Model.IsSelected)
            {
                OnPropertyChanged("FilteredMessages");
                if (NewMessageArrived != null)
                    NewMessageArrived(this, new EventArgs());
            }
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MOTD")
                OnPropertyChanged("MOTD");
        }
        #endregion
        #endregion

        #region IDisposable
        protected override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _update.Dispose();
                _update = null;

                _adFlood.Dispose();
                _adFlood = null;

                _messageFlood.Dispose();
                _messageFlood = null;

                _searchSettings = null;
                _genderSettings = null;

                Model.Messages.CollectionChanged -= OnMessagesChanged;
                Model.Ads.CollectionChanged -= OnAdsChanged;

                (Model as GeneralChannelModel).MOTD = null;
                (Model as GeneralChannelModel).Moderators.Clear();

                NewMessageArrived = null;
                NewAdArrived = null;
            }

            base.Dispose(IsManaged);
        }
        #endregion
    }
}
