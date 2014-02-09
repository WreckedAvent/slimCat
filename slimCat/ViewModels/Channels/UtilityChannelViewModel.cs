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

        /// <summary>
        ///     Initializes a new instance of the <see cref="UtilityChannelViewModel" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="events">
        ///     The events.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
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

        /// <summary>
        ///     Gets the client id string.
        /// </summary>
        public static string ClientIdString
        {
            get
            {
                return string.Format("{0} {1} ({2})", Constants.ClientId, Constants.ClientName, Constants.ClientVer);
            }
        }

        /// <summary>
        ///     Gets the language display names for a given culture name
        /// </summary>
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

        /// <summary>
        ///     Gets or sets a value indicating whether allow logging.
        /// </summary>
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

        /// <summary>
        ///     Gets or sets the back log max.
        /// </summary>
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

        /// <summary>
        ///     Gets the connect flavor text.
        /// </summary>
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

        /// <summary>
        ///     Gets or sets the connect time.
        /// </summary>
        public int ConnectTime { get; set; }

        /// <summary>
        ///     Gets or sets the global notify terms.
        /// </summary>
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

        /// <summary>
        ///     Gets or sets a value indicating whether has new update.
        /// </summary>
        public bool HasNewUpdate { get; set; }

        /// <summary>
        ///     Gets a value indicating whether is connecting.
        /// </summary>
        public bool IsConnecting
        {
            get { return !ChatModel.IsAuthenticated; }
        }

        /// <summary>
        ///     Gets the last message received.
        /// </summary>
        public string LastMessageReceived
        {
            get { return HelperConverter.DateTimeToRough(ChatModel.LastMessageReceived, true, false); }
        }

        /// <summary>
        ///     Gets the online bookmarks count.
        /// </summary>
        public int OnlineBookmarksCount
        {
            get { return CharacterManager.GetNames(ListKind.Bookmark).Count; }
        }

        /// <summary>
        ///     Gets the online count.
        /// </summary>
        public int OnlineCount
        {
            get { return CharacterManager.CharacterCount; }
        }

        /// <summary>
        ///     Gets the online count change.
        /// </summary>
        public string OnlineCountChange
        {
            get { return minuteOnlineCount.GetDisplayString(); }
        }

        /// <summary>
        ///     Gets the online friends count.
        /// </summary>
        public int OnlineFriendsCount
        {
            get { return CharacterManager.GetNames(ListKind.Friend).Count; }
        }

        /// <summary>
        ///     Gets the rough client up time.
        /// </summary>
        public string RoughClientUpTime
        {
            get { return HelperConverter.DateTimeToRough(ChatModel.ClientUptime, true, false); }
        }

        /// <summary>
        ///     Gets the rough server up time.
        /// </summary>
        public string RoughServerUpTime
        {
            get { return HelperConverter.DateTimeToRough(ChatModel.ServerUpTime, true, false); }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show notifications.
        /// </summary>
        public bool ShowNotifications
        {
            get { return ApplicationSettings.ShowNotificationsGlobal; }

            set
            {
                ApplicationSettings.ShowNotificationsGlobal = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
            }
        }

        /// <summary>
        ///     Gets or sets the update build time.
        /// </summary>
        public string UpdateBuildTime { get; set; }

        /// <summary>
        ///     Gets or sets the update link.
        /// </summary>
        public string UpdateLink { get; set; }

        /// <summary>
        ///     Gets or sets the update name.
        /// </summary>
        public string UpdateName { get; set; }

        /// <summary>
        ///     Gets or sets the volume.
        /// </summary>
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

        /// <summary>
        ///     The logged in event.
        /// </summary>
        /// <param name="payload">
        ///     The payload.
        /// </param>
        public void LoggedInEvent(bool? payload)
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

        /// <summary>
        ///     The login failed event.
        /// </summary>
        /// <param name="error">
        ///     The error.
        /// </param>
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

        /// <summary>
        ///     The login reconnecting event.
        /// </summary>
        /// <param name="payload">
        ///     The payload.
        /// </param>
        public void LoginReconnectingEvent(int payload)
        {
            if (ChatModel.IsAuthenticated)
            {
                updateTimer.Elapsed += UpdateConnectText;
                ChatModel.IsAuthenticated = false;
            }

            inStagger = true;
            flavorText = new StringBuilder("Attempting reconnect");

            ConnectTime = 0;
            DelayTime = payload;

            OnPropertyChanged("IsConnecting");
        }

        /// <summary>
        ///     The online count prime.
        /// </summary>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        public int OnlineCountPrime()
        {
            return OnlineCount;
        }

        /// <summary>
        ///     The update connect text.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
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

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="isManaged">
        ///     The is managed dispose.
        /// </param>
        protected override void Dispose(bool isManaged)
        {
            Dispose();

            if (!isManaged)
                return;

            updateTimer.Dispose();
            minuteOnlineCount.Dispose();
            Model = null;
        }

        /// <summary>
        ///     The send message.
        /// </summary>
        protected override void SendMessage()
        {
            UpdateError("Cannot send messages to this channel!");
        }

        #endregion
    }
}