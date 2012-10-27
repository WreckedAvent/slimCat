using System;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Views;
using System.Linq;
using lib;
using System.Windows.Input;
using slimCat.Properties;
using slimCat;
using System.Text;

namespace ViewModels
{
    /// <summary>
    /// Used for a few channels which are not treated normally and cannot receive/send messages. 
    /// </summary>
    public class UtilityChannelViewModel : ChannelViewModelBase, IDisposable
    {
        #region fields
        private System.Timers.Timer UpdateTimer = new System.Timers.Timer(1000); // every second
        private StringBuilder _flavorText;
        private StringBuilder _connectDotDot;
        private CacheCount _minuteOnlineCount;
        private bool _inStagger = false;
        #endregion

        #region Properties
        // things the UI binds to
        public string RoughServerUpTime { get { return HelperConverter.DateTimeToRough(CM.ServerUpTime, true, false); } }
        public string RoughClientUpTime { get { return HelperConverter.DateTimeToRough(CM.ClientUptime, true, false); } }

        public int OnlineCount { get { return CM.OnlineCharacters.Count(); } }
        public int OnlineFriendsCount
        {
            get { return (CM.OnlineFriends == null ? 0 : CM.OnlineFriends.Count()); } 
        }
        public int OnlineBookmarksCount
        {
            get { return (CM.OnlineBookmarks == null ? 0 : CM.OnlineBookmarks.Count()); } 
        }

        public string OnlineCountChange
        {
            get
            {
                return _minuteOnlineCount.GetDisplayString();
            }
        }

        public bool IsConnecting
        {
            get
            {
                return !CM.IsAuthenticated;
            }
        }

        public int ConnectTime { get; set; }

        public string ConnectFlavorText
        {
            get
            {
                return _flavorText.ToString() + _connectDotDot.ToString() + (!_inStagger ? "\nRequest sent " + ConnectTime + " seconds ago" : "");
            }
        }

        // things used elsewhere
        public int OnlineCountPrime()
        {
            return CM.OnlineCharacters.Count();
        }
        #endregion

        #region Constructors
        public UtilityChannelViewModel(string name, IUnityContainer contain, IRegionManager regman,
                                       IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                Model = _container.Resolve<GeneralChannelModel>(name);
                ConnectTime = 0;
                _flavorText = new StringBuilder("Connecting");
                _connectDotDot = new StringBuilder();

                _container.RegisterType<object, UtilityChannelView>(Model.ID, new InjectionConstructor(this));
                _minuteOnlineCount = new CacheCount(OnlineCountPrime, 15);

                UpdateTimer.Enabled = true;
                UpdateTimer.Elapsed += (s, e) => 
                { 
                    OnPropertyChanged("RoughServerUpTime");
                    OnPropertyChanged("RoughClientUpTime");
                    OnPropertyChanged("IsConnecting");
                };

                UpdateTimer.Elapsed += UpdateConnectText;

                _events.GetEvent<NewUpdateEvent>().Subscribe(param =>
                    {
                        if (param is CharacterUpdateModel)
                        {
                            var temp = param as CharacterUpdateModel;
                            if (temp.Arguments is Models.CharacterUpdateModel.LoginStateChangedEventArgs)
                            {
                                OnPropertyChanged("OnlineCount");
                                OnPropertyChanged("OnlineFriendsCount");
                                OnPropertyChanged("OnlineBookmarksCount");
                                OnPropertyChanged("OnlineCountChange");
                            }
                        }
                    });

                _events.GetEvent<LoginAuthenticatedEvent>().Subscribe(LoggedInEvent);
                _events.GetEvent<LoginFailedEvent>().Subscribe(LoginFailedEvent);
                _events.GetEvent<ReconnectingEvent>().Subscribe(LoginReconnectingEvent);
            }

            catch (Exception ex)
            {
                ex.Source = "Utility Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        protected override void SendMessage()
        {
            UpdateError("Cannot send messages to this channel!");
        }

        public void LoggedInEvent(bool? payload)
        {
            UpdateTimer.Elapsed -= UpdateConnectText;
            OnPropertyChanged("IsConnecting");
        }

        public void LoginFailedEvent(string error)
        {
            if (CM.IsAuthenticated)
            {
                UpdateTimer.Elapsed += UpdateConnectText;
                CM.IsAuthenticated = false;
            }

            _inStagger = true;
            _flavorText = new StringBuilder(error);

            _flavorText.Append("\nStaggering connection");
            ConnectTime = 0;

            OnPropertyChanged("IsConnecting");
        }

        public void UpdateConnectText(object sender, EventArgs e)
        {
            if (!CM.IsAuthenticated)
            {
                ConnectTime++;

                if (_connectDotDot.Length >= 3)
                    _connectDotDot.Clear();
                _connectDotDot.Append('.');

                OnPropertyChanged("ConnectFlavorText");
            }
        }

        public void LoginReconnectingEvent(string payload)
        {
            _inStagger = false;
            if (CM.IsAuthenticated)
            {
                UpdateTimer.Elapsed += UpdateConnectText;
                CM.IsAuthenticated = false;
            }

            _flavorText = new StringBuilder("Attempting reconnect");
            ConnectTime = 0;
            OnPropertyChanged("IsConnecting");
        }
        #endregion

        #region Commands
        private RelayCommand _saveChannels;
        public ICommand SaveChannelsCommand
        {
            get
            {
                if (_saveChannels == null)
                    _saveChannels = new RelayCommand(args =>
                        {
                            Settings.Default.SavedChannels = new System.Collections.Specialized.StringCollection();

                            foreach (var channel in CM.CurrentChannels)
                            {
                                if (!(channel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase)))
                                    Settings.Default.SavedChannels.Add(channel.ID);
                            }

                            Settings.Default.Save();
                            UpdateError("Channels saved.");
                        });
                return _saveChannels;
            }
        }

        #endregion

        protected override void Dispose(bool IsManagedDispose)
        {
            base.Dispose();

            if (IsManagedDispose)
            {
                UpdateTimer.Dispose();
                _minuteOnlineCount.Dispose();
                Model = null;
            }
        }
    }
}
