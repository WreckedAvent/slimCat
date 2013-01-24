using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Views;

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
        private ChannelManagementViewModel _channManVM;
        private string _adMessage = "";

        private System.Timers.Timer _messageFlood = new System.Timers.Timer(500);
        private System.Timers.Timer _adFlood = new System.Timers.Timer(602000);
        private System.Timers.Timer _update = new System.Timers.Timer(1000);

        private bool _isInCoolDownAd = false;
        private bool _isInCoolDownMessage = false;
        private DateTimeOffset _timeLeftAd;

        public event EventHandler NewAdArrived;
        public event EventHandler NewMessageArrived;

        // these two are completely different from the channel model's counts, as they work while the tab is selected
        private bool _hasNewAds;
        private bool _hasNewMessages;

        private IList<string> _thisDingTerms = new List<string>(); // this is a combination of all relevant ding terms 
        #endregion

        #region Properties
        public bool ShouldDisplayAds { get { return ((Model.Mode == ChannelMode.both) || (Model.Mode == ChannelMode.ads)); } }
        public bool ShouldDisplayChat { get { return ((Model.Mode == ChannelMode.both) || (Model.Mode == ChannelMode.chat)); } }

        private IEnumerable<string> thisDingTerms
        {
            get
            {
                int characterNameOffset = 2; // how many ding terms we have to offset for the character's name
                var count = _thisDingTerms.Count;
                var shouldBe = ApplicationSettings.GlobalNotifyTermsList.Count() 
                                + Model.Settings.EnumerableTerms.Count() 
                                + characterNameOffset;

                if (count != shouldBe)
                {
                    _thisDingTerms.Clear();

                    foreach (var term in ApplicationSettings.GlobalNotifyTermsList)
                        _thisDingTerms.Add(term);

                    foreach (var term in Model.Settings.EnumerableTerms)
                        _thisDingTerms.Add(term);

                    _thisDingTerms.Add(_cm.SelectedCharacter.Name);
                    _thisDingTerms.Add(_cm.SelectedCharacter.Name + "'s");
                }

                return _thisDingTerms.Distinct().Where(term => !string.IsNullOrWhiteSpace(term));
            }
        }

        public IEnumerable<IMessage> FilteredMessages
        {
            get
            {
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

                    if (value)
                        _hasNewMessages = false;
                    else
                        _hasNewAds = false;

                    OnPropertyChanged("OtherTabHasMessages");
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
                    OnPropertyChanged("IsNotSearching");
                }
            }
        }
        public bool IsNotSearching { get { return !IsSearching; } }

        public bool IsChatting
        {
            get { return !IsSearching; }
        }

        // Near the top, used for the label on the right side of title
        public string ChatContentString { get { if (IsDisplayingChat) return "Chat"; else return "Ads"; } }

        // Near the bottom above the text entry, to the far left
        public string StatusString
        {
            get
            {
                if (IsDisplayingAds && AutoPost && _isInCoolDownAd)
                    return "Auto post ads enabled";

                if (Message == null || Message.Length == 0)
                {
                    if (_hasNewAds && IsDisplayingChat)
                        return "This channel has new ad(s).";
                    else if (_hasNewMessages && IsDisplayingAds)
                        return "This channel has new message(s).";
                    else
                        return "";
                }

                else 
                    return string.Format("{0} / {1} characters", Message.Length, (IsDisplayingChat ? "4,096" : "50,000"));
            }
        }

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

        /// <summary>
        /// if we're displaying the channel's messages, if there's a new ad (or vice-versa)
        /// </summary>
        public bool OtherTabHasMessages
        {
            get
            {
                if (IsDisplayingChat)
                    return _hasNewAds;
                else
                    return _hasNewMessages;
            }
        }

        /// <summary>
        /// This is used for the channel settings, if it should show settings like 'notify when this character is mentioned'
        /// </summary>
        public bool ShowAllSettings { get { return true; } }

        /// <summary>
        /// Used for channel settings to display settings related to notify terms
        /// </summary>
        public bool HasNotifyTerms { get { return ChannelSettings.NotifyTerms != null && ChannelSettings.NotifyTerms.Length > 0; } }

        public ChannelManagementViewModel ChannelManagementViewModel { get { return _channManVM; } }
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

                var safeName = HelperConverter.EscapeSpaces(name);

                _container.RegisterType<object, GeneralChannelView>(safeName, new InjectionConstructor(this));

                _isDisplayingChat = ShouldDisplayChat;

                _channManVM = new ChannelManagementViewModel(_events, Model as GeneralChannelModel); // instance our management vm

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

                _update.Elapsed += (s, e) =>
                {
                    if (Model.IsSelected)
                    {
                        if (CannotPost)
                            OnPropertyChanged("TimeLeft");

                        OnPropertyChanged("StatusString");
                    }
                };

                _channManVM.PropertyChanged += (s, e) =>
                {
                    OnPropertyChanged("ChannelManagementViewModel");
                };
                #endregion

                _update.Enabled = true;

                #region Load Settings
                var newSettings = Services.SettingsDaemon.GetChannelSettings(cm.SelectedCharacter.Name, Model.Title, Model.ID, Model.Type);
                Model.Settings = newSettings;

                ChannelSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("ChannelSettings");
                    OnPropertyChanged("HasNotifyTerms");
                    if (!ChannelSettings.IsChangingSettings)
                        Services.SettingsDaemon.UpdateSettingsFile(ChannelSettings, cm.SelectedCharacter.Name, Model.Title, Model.ID);
                };
                #endregion

                PropertyChanged += OnPropertyChanged;
            }

            catch (Exception ex)
            {
                ex.Source = "General Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        private bool MeetsFilter(IMessage message)
        {
            return message.MeetsFilters(GenderSettings, SearchSettings, CM, CM.SelectedChannel as GeneralChannelModel);
        }

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
            _adFlood.Start();
            OnPropertyChanged("CanPost");
            OnPropertyChanged("CannotPost");
        }

        internal override void InvertButton(object arguments)
        {
            var args = arguments as string;
            if (args == null) return;

            if (args.Equals("Messages"))
                ChannelSettings.MessageNotifyOnlyForInteresting = !ChannelSettings.MessageNotifyOnlyForInteresting;

            if (args.Equals("PromoteDemote"))
                ChannelSettings.PromoteDemoteNotifyOnlyForInteresting = !ChannelSettings.PromoteDemoteNotifyOnlyForInteresting;

            if (args.Equals("JoinLeave"))
                ChannelSettings.JoinLeaveNotifyOnlyForInteresting = !ChannelSettings.JoinLeaveNotifyOnlyForInteresting;

            OnPropertyChanged("ChannelSettings");
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
            else if (IsDisplayingChat)
            {
                _hasNewAds = Model.Ads.Count > 0;
                OnPropertyChanged("OtherTabHasMessages");
            }

            OnPropertyChanged("StatusString");
        }

        private void OnMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            if (IsDisplayingChat && Model.IsSelected)
            {
                OnPropertyChanged("FilteredMessages");
                if (NewMessageArrived != null)
                    NewMessageArrived(this, new EventArgs());
            }

            else if (IsDisplayingAds)
            {
                _hasNewMessages = Model.Messages.Count > 0;
                OnPropertyChanged("OtherTabHasMessages");
            }

            OnPropertyChanged("StatusString");
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MOTD")
                OnPropertyChanged("MOTD");

            else if (e.PropertyName == "Type")
                OnPropertyChanged("ChannelTypeString"); // fixes laggy room type change

            else if (e.PropertyName == "Moderators")
                OnPropertyChanged("HasPermissions"); // fixes laggy permissions

            else if (e.PropertyName == "Mode")
            {
                if (Model.Mode == ChannelMode.ads && IsDisplayingChat)
                    IsDisplayingChat = false;
                if (Model.Mode == ChannelMode.chat && IsDisplayingAds)
                    IsDisplayingChat = true;
                OnPropertyChanged("CanSwitch");
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Message")
                OnPropertyChanged("StatusString"); // keep the counter updated
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
                PropertyChanged -= OnPropertyChanged;

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
