using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Views;

namespace ViewModels
{
    /// <summary>
    /// The UserbarViewCM allows the user to navigate the current conversations they have open. 
    /// It responds to the ChatOnDisplayEvent to paritally create the chat wrapper.
    /// </summary>
    public class UserbarViewModel : ViewModelBase
    {
        #region Fields
        private int _selPMIndex = -1;
        private int _selChanIndex = 0;
        private bool _isExpanded = true;
        private bool _hasNewPM = false;
        private bool _hasNewChanMessage = false;
        private bool _pmsExpanded = true;
        private bool _channelsExpanded = true;
        private bool _isChangingStatus = false;

        public const string UserbarView = "UserbarView";

        private string _status_cache = "";
        private StatusType _status_type_cache;

        private IDictionary<string, StatusType> _statuskinds = new Dictionary<string, StatusType>()
                { {"Online", StatusType.online},
                    {"Busy", StatusType.busy},
                    {"Do not Disturb", StatusType.dnd}, 
                    {"Looking For Play", StatusType.looking}, 
                    {"Away", StatusType.away} };
        #endregion

        #region Properties
        public bool PMsAreExpanded
        {
            get { return _pmsExpanded; }
            set { _pmsExpanded = value; OnPropertyChanged("PMsAreExpanded"); }
        }
        public bool ChannelsAreExpanded
        {
            get { return _channelsExpanded; }
            set { _channelsExpanded = value; OnPropertyChanged("ChannelsAreExpanded"); }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; OnPropertyChanged("IsExpanded"); OnPropertyChanged("ExpandString"); }
        }

        public string ExpandString
        {
            get
            {
                if (HasUpdate && !IsExpanded) return "!";
                return (IsExpanded ? "<" : ">");
            }
        }

        public bool HasUpdate
        {
            get { return (_hasNewChanMessage || _hasNewPM); }
        }

        public bool HasNewPM
        {
            get { return _hasNewPM; }
            set 
            { 
                _hasNewPM = value;
                OnPropertyChanged("HasNewPM");
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString");
            }
        }

        public bool HasNewMessage
        {
            get { return _hasNewChanMessage; }
            set
            {
                _hasNewChanMessage = value;
                OnPropertyChanged("HasNewMessage");
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString");
            }
        }

        public bool IsChangingStatus
        {
            get { return _isChangingStatus; }
            set
            {
                if (_isChangingStatus != value)
                {
                    _isChangingStatus = value;
                    OnPropertyChanged("IsChangingStatus");

                    if (value == false
                        && (_status_cache != CM.SelectedCharacter.StatusMessage
                            || _status_type_cache != CM.SelectedCharacter.Status))
                    {
                        sendStatusChangedCommand();
                        _status_cache = CM.SelectedCharacter.StatusMessage;
                        _status_type_cache = CM.SelectedCharacter.Status;
                    }

                }
            }
        }

        public bool HasPMs
        {
            get
            {
                return CM.CurrentPMs.Count > 0;
            }
        }

        public IDictionary<string, StatusType> StatusTypes { get { return _statuskinds; } }

        // this links the PM and Channel boxes so they act as one
        #region Selection Logic
        public int PM_Selected
        {
            get { return _selPMIndex; }
            set
            {
                if (_selPMIndex != value)
                {
                    _selPMIndex = value;
                    OnPropertyChanged("PM_Selected");

                    if (value != -1 && CM.SelectedChannel != CM.CurrentPMs[value])
                        _events.GetEvent<RequestChangeTabEvent>().Publish(CM.CurrentPMs[value].ID);

                    if (PM_Selected != -1)
                    {
                        if (Chan_Selected != -1)
                            Chan_Selected = -1;
                    }
                }
            }
        }

        public int Chan_Selected
        {
            get { return _selChanIndex; }
            set
            {
                if (_selChanIndex != value)
                {
                    _selChanIndex = value;
                    OnPropertyChanged("Chan_Selected");

                    if (value != -1 &&CM.SelectedChannel != CM.CurrentChannels[value])
                        _events.GetEvent<RequestChangeTabEvent>().Publish(CM.CurrentChannels[value].ID);

                    if (Chan_Selected != -1)
                    {
                        if (PM_Selected != -1)
                            PM_Selected = -1;
                    }
                }
            }
        }
        #endregion
        #endregion

        #region Constructors
        public UserbarViewModel(IUnityContainer contain, IRegionManager regman,
                                IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                CM.CurrentPMs.CollectionChanged += (s, e) => OnPropertyChanged("HasPMs"); // this checks if we need to hide/show the PM tab

                _events.GetEvent<ChatOnDisplayEvent>().Subscribe(requestNavigate, ThreadOption.UIThread, true);

                _events.GetEvent<NewPMEvent>().Subscribe(
                    param =>
                    {
                        updateFlashingTabs();
                    }, ThreadOption.UIThread, true);

                _events.GetEvent<NewMessageEvent>().Subscribe(
                    param =>
                    {
                        updateFlashingTabs(); 
                    }, ThreadOption.UIThread, true);

                CM.SelectedChannelChanged += (s, e) => updateFlashingTabs();

            }

            catch (Exception ex)
            {
                ex.Source = "Userbar ViewCM, init";
                Exceptions.HandleException(ex);
            }
        }

        public override void Initialize()
        {
            try
            {
                _container.RegisterType<object, UserbarView>(UserbarView);
            }
            catch (Exception ex)
            {
                ex.Source = "Userbar ViewCM, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        private void requestNavigate(bool? payload)
        {
            _events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(requestNavigate);
            _region.Regions[ChatWrapperView.UserbarRegion].Add(_container.Resolve<UserbarView>());
        }

        private void sendStatusChangedCommand()
        {
            var torSend = CommandDefinitions
                .CreateCommand("status", new List<string>() 
                    { CM.SelectedCharacter.Status.ToString(), CM.SelectedCharacter.StatusMessage })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(torSend);
        }

        private void onExpanded(object args = null)
        {
            PMsAreExpanded = PMsAreExpanded || HasNewPM;
            ChannelsAreExpanded = ChannelsAreExpanded || HasNewMessage;
            IsExpanded = !IsExpanded;
        }

        // this will update which tabs need to be 'flashing' and which do not
        private void updateFlashingTabs()
        {
            if (CM.SelectedChannel is PMChannelModel)
                PM_Selected = CM.CurrentPMs.IndexOf(CM.SelectedChannel as PMChannelModel);
            else
                Chan_Selected = CM.CurrentChannels.IndexOf(CM.SelectedChannel as GeneralChannelModel);

            bool stillHasPMs = false;
            foreach (ChannelModel cm in CM.CurrentPMs)
            {
                if (cm.NeedsAttention == true)
                {
                    stillHasPMs = true;
                    break;
                }
            }

            HasNewPM = stillHasPMs;

            bool stillHasMessages = false;
            foreach (ChannelModel cm in CM.CurrentChannels)
            {
                if (cm.NeedsAttention == true)
                {
                    stillHasMessages = true;
                    break;
                }
            }

            HasNewMessage = stillHasMessages;
        }
        #endregion

        #region Commands
        private RelayCommand _close;
        public ICommand CloseCommand
        {
            get
            {
                if (_close == null)
                    _close = new RelayCommand(TabCloseEvent);
                return _close;
            }
        }

        private RelayCommand _toggle;
        public ICommand ToggleBarCommand
        {
            get
            {
                if (_toggle == null)
                    _toggle = new RelayCommand(onExpanded);
                return _toggle;
            }
        }

        private RelayCommand _toggleStatus;
        public ICommand ToggleStatusWindowCommand
        {
            get
            {
                if (_toggleStatus == null)
                    _toggleStatus = new RelayCommand(args => IsChangingStatus = !IsChangingStatus);
                return _toggleStatus;
            }
        }

        private void TabCloseEvent(object args)
        {
            var toSend = CommandDefinitions
                .CreateCommand("close", null, args as string)
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(toSend);
        }
        private RelayCommand _saveChannels;

        public ICommand SaveChannelsCommand
        {
            get
            {
                if (_saveChannels == null)
                    _saveChannels = new RelayCommand(args =>
                    {
                        ApplicationSettings.SavedChannels.Clear();

                        foreach (var channel in CM.CurrentChannels)
                        {
                            if (!(channel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase)))
                                ApplicationSettings.SavedChannels.Add(channel.ID);
                        }

                        Services.SettingsDaemon.SaveApplicationSettingsToXML(CM.SelectedCharacter.Name);
                        _events.GetEvent<ErrorEvent>().Publish("Channels saved.");
                    });
                return _saveChannels;
            }
        }
        #endregion
    }
}
