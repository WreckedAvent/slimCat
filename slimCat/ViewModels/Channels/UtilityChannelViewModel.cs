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

        private readonly StringBuilder connectDotDot;

        private readonly CacheCount minuteOnlineCount;
        private readonly Timer updateTimer = new Timer(1000); // every second

        private StringBuilder flavorText;

        private bool inStagger;

        #endregion

        #region Constructors and Destructors

        public UtilityChannelViewModel(
            string name, IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager manager)
            : base(contain, regman, events, cm, manager)
        {
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

        public static string ClientIdString
        {
            get
            {
                return string.Format("{0} {1} ({2})", Constants.ClientId, Constants.ClientName, Constants.ClientVer);
            }
        }

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
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public bool AllowLogging
        {
            get { return ApplicationSettings.AllowLogging; }

            set
            {
                ApplicationSettings.AllowLogging = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        /// <summary>
        ///     Gets or sets a value indiciating whether friends are account wide, or character-specific
        /// </summary>
        public bool FriendsAreAccountWide
        {
            get { return ApplicationSettings.FriendsAreAccountWide; }

            set
            {
                ApplicationSettings.FriendsAreAccountWide = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public int FontSize
        {
            get { return ApplicationSettings.FontSize; }
            set
            {
                if (value >= 8 && value <= 20)
                    ApplicationSettings.FontSize = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public int BackLogMax
        {
            get { return ApplicationSettings.BackLogMax; }

            set
            {
                if (value < 25000 || value > 10)
                    ApplicationSettings.BackLogMax = value;

                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public ICharacter slimCat
        {
            get { return CharacterManager.Find("slimCat"); }
        }


        public string ConnectFlavorText
        {
            get
            {
                if (ChatModel.IsAuthenticated) return string.Empty;

                return flavorText + connectDotDot.ToString()
                       + (!inStagger ? "\nRequest sent " + ConnectTime + " seconds ago" : string.Empty)
                       + (DelayTime > 0 ? "\nWaiting " + --DelayTime + " seconds until reconnecting" : string.Empty);
            }
        }

        public int ConnectTime { get; set; }


        public string GlobalNotifyTerms
        {
            get { return ApplicationSettings.GlobalNotifyTerms; }

            set
            {
                ApplicationSettings.GlobalNotifyTerms = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public GenderColorSettings GenderColorSettings
        {
            get { return ApplicationSettings.GenderColorSettings; }
            set
            {
                ApplicationSettings.GenderColorSettings = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public bool HasNewUpdate { get; set; }

        public bool IsConnecting
        {
            get { return !ChatModel.IsAuthenticated; }
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

        public bool ShowNotifications
        {
            get { return ApplicationSettings.ShowNotificationsGlobal; }

            set
            {
                ApplicationSettings.ShowNotificationsGlobal = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public string UpdateBuildTime { get; set; }

        public string UpdateLink { get; set; }

        public string UpdateName { get; set; }

        public double Volume
        {
            get { return ApplicationSettings.Volume; }

            set
            {
                ApplicationSettings.Volume = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        public bool AllowSoundWhenTabIsFocused
        {
            get { return ApplicationSettings.PlaySoundEvenWhenTabIsFocused; }

            set
            {
                ApplicationSettings.PlaySoundEvenWhenTabIsFocused = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        #endregion

        #region Public Methods and Operators

        public int DelayTime { get; set; }

        public void LoggedInEvent(bool? _)
        {
            updateTimer.Elapsed -= UpdateConnectText;
            OnPropertyChanged("IsConnecting");


            SettingsDaemon.ReadApplicationSettingsFromXml(ChatModel.CurrentCharacter.Name, CharacterManager);

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

                if (args[0].Equals(Constants.FriendlyName, StringComparison.OrdinalIgnoreCase))
                    return;

                var updateDelayTimer = new Timer(10*1000);
                updateDelayTimer.Elapsed += (s, e) =>
                    {
                        Events.GetEvent<ErrorEvent>()
                            .Publish("{0} is now available! \nPlease Update with the link in the home tab.".FormatWith(args[0]));
                        updateDelayTimer.Stop();
                        updateDelayTimer = null;
                    };
                updateDelayTimer.Start();

                HasNewUpdate = true;

                UpdateName = args[0];
                UpdateLink = args[1];
                UpdateBuildTime = args[2];

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

        public int OnlineCountPrime()
        {
            return OnlineCount;
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

        #endregion
    }
}