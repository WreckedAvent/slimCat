#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UtilityChannelViewModel.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Timers;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     Used for a few channels which are not treated normally and cannot receive/send messages.
    /// </summary>
    public class UtilityChannelViewModel : ChannelViewModelBase
    {
        #region Constants

        private const string NewVersionLink = "https://dl.dropbox.com/u/29984849/slimCat/latest.csv";

        #endregion

        #region Fields

        private readonly IAutomationService automation;
        private readonly StringBuilder connectDotDot;

        private readonly CacheCount minuteOnlineCount;

        private readonly Timer updateTimer = new Timer(1000); // every second

        private StringBuilder flavorText;

        private bool inStagger;

        #endregion

        #region Constructors and Destructors

        public UtilityChannelViewModel(
            string name, IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager manager, IAutomationService automation)
            : base(contain, regman, events, cm, manager)
        {
            this.automation = automation;
            try
            {
                Model = Container.Resolve<GeneralChannelModel>(name);
                ConnectTime = 0;
                flavorText = new StringBuilder("Connecting");
                connectDotDot = new StringBuilder();

                Container.RegisterType<object, UtilityChannelView>(Model.Id, new InjectionConstructor(this));
                minuteOnlineCount = new CacheCount(OnlineCountPrime, 15, 1000*15);

                updateTimer.Enabled = true;
                updateTimer.Elapsed += (s, e) =>
                    {
                        OnPropertyChanged("RoughServerUpTime");
                        OnPropertyChanged("RoughClientUpTime");
                        OnPropertyChanged("LastMessageReceived");
                        OnPropertyChanged("IsConnecting");
                    };

                updateTimer.Elapsed += UpdateConnectText;

                Events.GetEvent<NewUpdateEvent>().Subscribe(
                    param =>
                        {
                            if (!(param is CharacterUpdateModel))
                                return;

                            var temp = param as CharacterUpdateModel;
                            if (!(temp.Arguments is CharacterUpdateModel.LoginStateChangedEventArgs))
                                return;

                            OnPropertyChanged("OnlineCount");
                            OnPropertyChanged("OnlineFriendsCount");
                            OnPropertyChanged("OnlineBookmarksCount");
                            OnPropertyChanged("OnlineCountChange");
                        });

                Events.GetEvent<LoginAuthenticatedEvent>().Subscribe(LoggedInEvent);
                Events.GetEvent<LoginFailedEvent>().Subscribe(LoginFailedEvent);
                Events.GetEvent<ReconnectingEvent>().Subscribe(LoginReconnectingEvent);
            }
            catch (Exception ex)
            {
                ex.Source = "Utility Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        #region Header

        public static string ClientIdString
        {
            get
            {
                return string.Format("{0} {1} ({2})", Constants.ClientId, Constants.ClientName, Constants.ClientVer);
            }
        }

        public string LastMessageReceived
        {
            get { return HelperConverter.DateTimeToRough(ChatModel.LastMessageReceived, true, false); }
        }

        public int OnlineBookmarksCount
        {
            get { return CharacterManager.GetNames(ListKind.Bookmark).Count; }
        }

        public int OnlineCount
        {
            get { return CharacterManager.CharacterCount; }
        }

        public string OnlineCountChange
        {
            get { return minuteOnlineCount.GetDisplayString(); }
        }

        public int OnlineFriendsCount
        {
            get { return CharacterManager.GetNames(ListKind.Friend).Count; }
        }

        public string RoughClientUpTime
        {
            get { return HelperConverter.DateTimeToRough(ChatModel.ClientUptime, true, false); }
        }

        public string RoughServerUpTime
        {
            get { return HelperConverter.DateTimeToRough(ChatModel.ServerUpTime, true, false); }
        }

        public int OnlineCountPrime()
        {
            return OnlineCount;
        }

        #endregion

        #region Settings
        #region General

        public static IEnumerable<KeyValuePair<string, string>> LanguageNames
        {
            get
            {
                return new Dictionary<string, string>
                    {
                        {"American English", "en-US"},
                        {"British English", "en-GB"},
                        {"French", "fr"},
                        {"German", "de"},
                        {"Spanish", "es"}
                    };
            }
        }

        public bool AllowLogging
        {
            get { return ApplicationSettings.AllowLogging; }

            set
            {
                ApplicationSettings.AllowLogging = value;
                Save();
            }
        }

        public bool FriendsAreAccountWide
        {
            get { return ApplicationSettings.FriendsAreAccountWide; }

            set
            {
                ApplicationSettings.FriendsAreAccountWide = value;
                Save();
            }
        }

        public bool AllowMinimizeToSystemTray
        {
            get { return ApplicationSettings.AllowMinimizeToTray; }
            set
            {
                ApplicationSettings.AllowMinimizeToTray = value;
                Save();
            }
        }

        #endregion

        #region Appearance

        public static IEnumerable<KeyValuePair<string, GenderColorSettings>> GenderSettings
        {
            get
            {
                return new Dictionary<string, GenderColorSettings>
                    {
                        {"No Coloring", GenderColorSettings.None},
                        {"Minimal Coloring", GenderColorSettings.GenderOnly},
                        {"Moderate Coloring", GenderColorSettings.GenderAndHerm},
                        {"Full Coloring", GenderColorSettings.Full}
                    };
            }
        }

        public bool AllowColors
        {
            get { return ApplicationSettings.AllowColors; }

            set
            {
                ApplicationSettings.AllowColors = value;
                Save();
            }
        }
        public bool AllowStatusDiscolor
        {
            get { return ApplicationSettings.AllowStatusDiscolor; }

            set
            {
                ApplicationSettings.AllowStatusDiscolor = value;
                Save();
            }
        }

        public int FontSize
        {
            get { return ApplicationSettings.FontSize; }
            set
            {
                if (value >= 8 && value <= 20)
                    ApplicationSettings.FontSize = value;

                Save();
            }
        }

        public GenderColorSettings GenderColorSettings
        {
            get { return ApplicationSettings.GenderColorSettings; }
            set
            {
                ApplicationSettings.GenderColorSettings = value;
                Save();
            }
        }

        #endregion

        #region Automation

        public bool AllowAutoIdle
        {
            get { return ApplicationSettings.AllowAutoIdle; }
            set
            {
                ApplicationSettings.AllowAutoIdle = value;
                OnPropertyChanged("AllowAutoIdle");
                automation.ResetStatusTimers();
                Save();
            }
        }

        public int AutoIdleTime
        {
            get { return ApplicationSettings.AutoIdleTime; }
            set
            {
                ApplicationSettings.AutoIdleTime = value;
                OnPropertyChanged("AutoIdleTime");
                automation.ResetStatusTimers();
                Save();
            }
        }

        public bool AllowAutoAway
        {
            get { return ApplicationSettings.AllowAutoAway; }
            set
            {
                ApplicationSettings.AllowAutoAway = value;
                OnPropertyChanged("AllowAutoAway");
                automation.ResetStatusTimers();
                Save();
            }
        }

        public int AutoAwayTime
        {
            get { return ApplicationSettings.AutoAwayTime; }
            set
            {
                ApplicationSettings.AutoAwayTime = value;
                OnPropertyChanged("AutoAwayTime");
                automation.ResetStatusTimers();
                Save();
            }
        }

        public bool AllowAutoStatusReset
        {
            get { return ApplicationSettings.AllowStatusAutoReset; }
            set
            {
                ApplicationSettings.AllowStatusAutoReset = value;
                automation.ResetStatusTimers();
                Save();
            }
        }

        public bool AllowAdDedpulication
        {
            get { return ApplicationSettings.AllowAdDedup; }
            set
            {
                ApplicationSettings.AllowAdDedup = value;

                // remove all stored ads
                if (!value)
                {
                    var characters = CharacterManager.Characters.Where(x => x.LastAd != null).ToList();
                    characters.Each(x => x.LastAd = null);
                }

                Save();
            }
        }

        public bool AllowAutoBusy
        {
            get { return ApplicationSettings.AllowAutoBusy; }
            set
            {
                ApplicationSettings.AllowAutoBusy = value;
                Save();
            }
        }

        #endregion

        #region Notifications

        public string GlobalNotifyTerms
        {
            get { return ApplicationSettings.GlobalNotifyTerms; }

            set
            {
                ApplicationSettings.GlobalNotifyTerms = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public bool CheckOwnName
        {
            get { return ApplicationSettings.CheckForOwnName; }
            set
            {
                ApplicationSettings.CheckForOwnName = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public bool ShowNotifications
        {
            get { return ApplicationSettings.ShowNotificationsGlobal; }

            set
            {
                ApplicationSettings.ShowNotificationsGlobal = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public bool AllowSoundWhenTabIsFocused
        {
            get { return ApplicationSettings.PlaySoundEvenWhenTabIsFocused; }

            set
            {
                ApplicationSettings.PlaySoundEvenWhenTabIsFocused = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        #endregion
        #endregion

        #region Help

        public ICharacter slimCat
        {
            get { return CharacterManager.Find("slimCat"); }
        }

        #endregion

        #region Reconnect

        public int DelayTime { get; set; }

        public bool IsConnecting
        {
            get { return !ChatModel.IsAuthenticated; }
        }

        public string ConnectFlavorText
        {
            get
            {
                if (ChatModel.IsAuthenticated) return string.Empty;

                if (inStagger && DelayTime == 0)
                {
                    inStagger = false;
                    ConnectTime = 0;
                }

                return flavorText + connectDotDot.ToString()
                       + (!inStagger ? "\nRequest sent " + ConnectTime + " second(s) ago" : string.Empty)
                       + (DelayTime > 0 ? "\nWaiting " + --DelayTime + " second(s) until reconnecting" : string.Empty);
            }
        }

        public int ConnectTime { get; set; }

        #endregion

        #region Update

        public bool HasNewUpdate { get; set; }

        public string UpdateBuildTime { get; set; }

        public string UpdateLink { get; set; }

        public string UpdateName { get; set; }

        public string ChangeLog { get; set; }

        #endregion

        #region Recent Tabs

        public IList<ICharacter> RecentCharacters
        {
            get
            {
                var characters = ApplicationSettings.RecentCharacters;

                var toReturn = characters
                    .Select(x => CharacterManager.Find(x))
                    .Reverse()
                    .ToList();

                toReturn.Each(x => x.GetAvatar());

                return toReturn;
            }
        }

        public IList<ChannelModel> RecentChannels
        {
            get
            {
                var channels = ApplicationSettings.RecentChannels;

                return channels
                    .Select(x => ChatModel.FindChannel(x))
                    .Reverse()
                    .ToList();
            }
        } 

        #endregion

        #endregion

        #region Public Methods and Operators

        public void LoggedInEvent(bool? _)
        {
            updateTimer.Elapsed -= UpdateConnectText;
            OnPropertyChanged("IsConnecting");

            SettingsService.ReadApplicationSettingsFromXml(ChatModel.CurrentCharacter.Name, CharacterManager);
            automation.ResetStatusTimers();

            try
            {
                string[] args;
                using (var client = new WebClient())
                using (var stream = client.OpenRead(NewVersionLink))
                {
                    if (stream == null)
                        return;

                    using (var reader = new StreamReader(stream))
                        args = reader.ReadToEnd().Split(',');
                }

                HasNewUpdate = !args[0].Equals(Constants.FriendlyName, StringComparison.OrdinalIgnoreCase);

                var updateDelayTimer = new Timer(10*1000);
                updateDelayTimer.Elapsed += (s, e) =>
                    {
                        Events.GetEvent<ErrorEvent>()
                            .Publish(
                                "{0} is now available! \nPlease Update with the link in the home tab.".FormatWith(
                                    args[0]));
                        updateDelayTimer.Stop();
                        updateDelayTimer = null;
                    };

                if (HasNewUpdate)
                    updateDelayTimer.Start();

                UpdateName = args[0];
                UpdateLink = args[1];
                UpdateBuildTime = args[2];
                ChangeLog = args[3];

                OnPropertyChanged("HasNewUpdate");
                OnPropertyChanged("UpdateName");
                OnPropertyChanged("UpdateLink");
                OnPropertyChanged("UpdateBuildTime");
            }
            catch (WebException)
            {
            }
        }

        public void LoginFailedEvent(string error)
        {
            if (ChatModel.IsAuthenticated)
            {
                updateTimer.Elapsed += UpdateConnectText;
                ChatModel.IsAuthenticated = false;
            }

            inStagger = true;
            flavorText = new StringBuilder(error);

            flavorText.Append("\nStaggering connection");
            ConnectTime = 0;

            OnPropertyChanged("IsConnecting");
        }

        public void LoginReconnectingEvent(int reconnectTime)
        {
            if (ChatModel.IsAuthenticated)
            {
                updateTimer.Elapsed += UpdateConnectText;
                ChatModel.IsAuthenticated = false;
            }

            inStagger = true;
            flavorText = new StringBuilder("Attempting reconnect");

            ConnectTime = 0;
            DelayTime = reconnectTime;

            OnPropertyChanged("IsConnecting");
        }

        public void UpdateConnectText(object sender, EventArgs e)
        {
            if (ChatModel.IsAuthenticated)
                return;

            ConnectTime++;

            if (connectDotDot.Length >= 3)
                connectDotDot.Clear();

            connectDotDot.Append('.');

            OnPropertyChanged("ConnectFlavorText");
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            Dispose();

            if (!isManaged)
                return;

            updateTimer.Dispose();
            minuteOnlineCount.Dispose();
            Model = null;
        }

        protected override void SendMessage()
        {
            UpdateError("Cannot send messages to this channel!");
        }

        private void Save()
        {
            SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
        }

        #endregion
    }
}