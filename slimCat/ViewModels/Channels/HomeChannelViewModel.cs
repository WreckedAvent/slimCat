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

    using Microsoft.Practices.Unity;
    using Microsoft.VisualBasic.FileIO;
    using Models;
    using Services;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Timers;
    using System.Windows.Media;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     Used for a few channels which are not treated normally and cannot receive/send messages.
    /// </summary>
    public class HomeChannelViewModel : ChannelViewModelBase
    {

        #region Fields

        private readonly IAutomationService automation;

        private readonly IIconService iconService;

        private readonly IBrowser browser;

        private readonly StringBuilder connectDotDot;

        private readonly CacheCount minuteOnlineCount;

        private readonly Timer updateTimer = new Timer(1000); // every second

        private StringBuilder flavorText;

        private bool inStagger;

        #endregion

        #region Constructors and Destructors

        public HomeChannelViewModel(string name, IChatState chatState, IAutomationService automation, IBrowser browser, IIconService iconService)
            : base(chatState)
        {
            try
            {
                Model = Container.Resolve<GeneralChannelModel>(name);
                ConnectTime = 0;
                flavorText = new StringBuilder("Connecting");
                connectDotDot = new StringBuilder();

                Container.RegisterType<object, HomeChannelView>(Model.Id, new InjectionConstructor(this));
                minuteOnlineCount = new CacheCount(OnlineCountPrime, 15, 1000*15);

                updateTimer.Enabled = true;
                updateTimer.Elapsed += (s, e) =>
                    {
                        OnPropertyChanged("RoughServerUpTime");
                        OnPropertyChanged("RoughClientUpTime");
                        OnPropertyChanged("LastMessageReceived");
                    };

                updateTimer.Elapsed += UpdateConnectText;

                Events.GetEvent<NewUpdateEvent>().Subscribe(
                    param =>
                        {
                            if (!(param is CharacterUpdateModel))
                                return;

                            var temp = param as CharacterUpdateModel;
                            if (!(temp.Arguments is LoginStateChangedEventArgs))
                                return;

                            OnPropertyChanged("OnlineCount");
                            OnPropertyChanged("OnlineFriendsCount");
                            OnPropertyChanged("OnlineBookmarksCount");
                            OnPropertyChanged("OnlineCountChange");
                        });

                Events.GetEvent<LoginAuthenticatedEvent>().Subscribe(LoggedInEvent);
                Events.GetEvent<LoginFailedEvent>().Subscribe(LoginFailedEvent);
                Events.GetEvent<ReconnectingEvent>().Subscribe(LoginReconnectingEvent);

                this.automation = automation;
                this.browser = browser;
                this.iconService = iconService;
                this.iconService.SettingsChanged += (s, e) =>
                    {
                        OnPropertyChanged("AllowSound");
                        OnPropertyChanged("ShowNotifications");
                    };

                LoggingSection = "utility channel vm";

                Themes = new ObservableCollection<ThemeModel>();
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

        public bool IsTemplateCharacter
        {
            get { return ApplicationSettings.TemplateCharacter.Equals(ChatModel.CurrentCharacter.Name); }
            set
            {
                var newVale = value ? ChatModel.CurrentCharacter.Name : string.Empty;
                ApplicationSettings.TemplateCharacter = newVale;
                Save();
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

        public bool HideFriendsFromSearchResults
        {
            get { return ApplicationSettings.HideFriendsFromSearchResults; }
            set
            {
                ApplicationSettings.HideFriendsFromSearchResults = value;
                Save();
            }
        }

        public bool AllowGreedyTextboxFocus
        {
            get { return ApplicationSettings.AllowGreedyTextboxFocus; }
            set
            {
                ApplicationSettings.AllowGreedyTextboxFocus = value;
                Save();
            }
        }
        public bool AllowTexboxDisable
        {
            get { return ApplicationSettings.AllowTextboxDisable; }
            set
            {
                ApplicationSettings.AllowTextboxDisable = value;
                Save();
            }
        }

        public bool UseMilitaryTime
        {
            get { return ApplicationSettings.UseMilitaryTime; }
            set
            {
                ApplicationSettings.UseMilitaryTime = value;
                Save();
            }
        }

        public bool OpenOfflineChatsInNoteView
        {
            get { return ApplicationSettings.OpenOfflineChatsInNoteView; }
            set
            {
                ApplicationSettings.OpenOfflineChatsInNoteView = value;
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

        public bool AllowIcons
        {
            get { return ApplicationSettings.AllowIcons; }
            set
            {
                ApplicationSettings.AllowIcons = value;
                Save();
            }
        }

        public bool AllowIndent
        {
            get { return ApplicationSettings.AllowIndent; }
            set
            {
                ApplicationSettings.AllowIndent = value;
                Save();
            }
        }

        public bool ViewProfilesInChat
        {
            get { return ApplicationSettings.OpenProfilesInClient; }
            set
            {
                ApplicationSettings.OpenProfilesInClient = value;
                Save();
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

                OnPropertyChanged("FontSize");
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
                OnPropertyChanged("AllowAdDedpulication");
            }
        }

        public bool AllowAggressiveAdDedpulication
        {
            get { return ApplicationSettings.AllowAggressiveAdDedup; }
            set
            {
                ApplicationSettings.AllowAggressiveAdDedup = value;
                Save();
            }
        }

        public bool AllowAdTruncating
        {
            get { return ApplicationSettings.ShowMoreInAdsLength != 50000; }
            set
            {
                ApplicationSettings.ShowMoreInAdsLength = value ? 400 : 50000;
                OnPropertyChanged("AllowAdTruncating");
                OnPropertyChanged("AdTruncateLength");
                Save();
            }
        }

        public int AdTruncateLength
        {
            get { return ApplicationSettings.ShowMoreInAdsLength; }
            set
            {
                ApplicationSettings.ShowMoreInAdsLength = value;
                OnPropertyChanged("AdTruncateLength");
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
                Save();
            }
        }

        public bool AllowSound
        {
            get { return ApplicationSettings.AllowSound; }
            set
            {
                if (ApplicationSettings.AllowSound == value) return;

                iconService.ToggleSound();
                Save();
            }
        }

        public bool CheckOwnName
        {
            get { return ApplicationSettings.CheckForOwnName; }
            set
            {
                ApplicationSettings.CheckForOwnName = value;
                Save();
            }
        }

        public bool ShowNotifications
        {
            get { return ApplicationSettings.ShowNotificationsGlobal; }

            set
            {
                if (ApplicationSettings.ShowNotificationsGlobal == value) return;

                iconService.ToggleToasts();
                Save();
            }
        }

        public bool AllowSoundWhenTabIsFocused
        {
            get { return ApplicationSettings.PlaySoundEvenWhenTabIsFocused; }

            set
            {
                ApplicationSettings.PlaySoundEvenWhenTabIsFocused = value;
                Save();
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

        #region Theme

        public ObservableCollection<ThemeModel> Themes { get; set; }

        public ThemeModel CurrentTheme { get; set; }

        public bool HasCurrentTheme { get; set; }
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

        #region Textbox

        public override string EntryTextBoxIcon
        {
            get { return "pack://application:,,,/icons/send_console.png"; }
        }

        public override string EntryTextBoxLabel
        {
            get { return "Enter commands here ..."; }
        }

        #endregion

        #endregion

        #region Public Methods and Operators

        public void LoggedInEvent(bool? _)
        {
            updateTimer.Elapsed -= UpdateConnectText;

            SettingsService.ReadApplicationSettingsFromXml(ChatModel.CurrentCharacter.Name, CharacterManager);
            automation.ResetStatusTimers();
            OnPropertyChanged("RecentChannels");
            OnPropertyChanged("RecentCharacters");
            OnPropertyChanged("IsTemplateCharacter");

            CheckForUpdates();
            CheckForThemes();
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
            UpdateError("Cannot send messages to the home tab!");
        }

        private void Save()
        {
            ApplicationSettings.SettingsVersion = Constants.ClientVer;
            SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
        }

        private void CheckForUpdates()
        {
            try
            {
                var resp = browser.GetResponse(Constants.NewVersionUrl);
                if (resp == null) return;
                var args = resp.Split(',');

                var versionString = args[0].Substring(args[0].LastIndexOf(' '));
                var version = Convert.ToDouble(versionString);

                var ourVersion = Convert.ToDouble(Constants.ClientVer.Contains(" ")
                    ? Constants.ClientVer.Substring(0, Constants.ClientVer.LastIndexOf(' '))
                    : Constants.ClientVer);

                HasNewUpdate = version > ourVersion;

                if (!HasNewUpdate && Math.Abs(version - ourVersion) < 0.001)
                {
                    HasNewUpdate = Constants.ClientVer.Contains("dev");
                }

                var updateDelayTimer = new Timer(10 * 1000);
                updateDelayTimer.Elapsed += (s, e) =>
                    {
                        Events.GetEvent<ErrorEvent>()
                            .Publish(
                                "{0} is now available! \nPlease Update with the link in the home tab.".FormatWith(
                                    args[0]));
                        updateDelayTimer.Stop();
                        updateDelayTimer = null;
                        Model.FlashTab();
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
                OnPropertyChanged("ChangeLog");
            }
            catch (WebException)
            {
            }
        }

        private void CheckForThemes()
        {
            try
            {
                try
                {
                    var currentThemeParser =
                        new TextFieldParser(new FileStream("Theme\\theme.csv", FileMode.Open, FileAccess.Read,
                            FileShare.Read));

                    currentThemeParser.SetDelimiters(",");

                    // go through header
                    currentThemeParser.ReadLine();

                    CurrentTheme = GetThemeModel(currentThemeParser.ReadFields());
                    OnPropertyChanged("CurrentTheme");

                    currentThemeParser.Close();

                    HasCurrentTheme = true;
                    OnPropertyChanged("HasCurrentTheme");
                }
                catch
                {
                    HasCurrentTheme = false;
                    OnPropertyChanged("HasCurrentTheme");
                }

                var resp = browser.GetResponse(Constants.ThemeIndexUrl);
                if (resp == null) return;

                var parser = new TextFieldParser(new StringReader(resp))
                {
                    TextFieldType = FieldType.Delimited
                };

                parser.SetDelimiters(",");

                // go through header
                parser.ReadLine();

                Dispatcher.BeginInvoke((Action) (() => Themes.Clear()));
                while (!parser.EndOfData)
                {
                    var row = parser.ReadFields();
                    var model = GetThemeModel(row);

                    if (HasCurrentTheme && model.Name == CurrentTheme.Name && model.Version == CurrentTheme.Version)
                        continue;

                    Dispatcher.BeginInvoke((Action)(() => Themes.Add(model)));
                }

                parser.Close();
            }
            catch (WebException)
            {
            }
        }

        private Color GetColor(string hex)
        {
            return (Color) ColorConverter.ConvertFromString(hex);
        }

        private ThemeModel GetThemeModel(IList<string> themeCsv)
        {
            return new ThemeModel
            {
                Name = themeCsv[0],
                Author = CharacterManager.Find(themeCsv[1]),
                Version = themeCsv[2],
                ForegroundColor = GetColor(themeCsv[3]),
                HighlightColor = GetColor(themeCsv[4]),
                ContrastColor = GetColor(themeCsv[5]),
                BackgroundColor = GetColor(themeCsv[6]),
                DepressedColor = GetColor(themeCsv[7]),
                BrightBackgroundColor = GetColor(themeCsv[8]),
                Url = themeCsv.Count == 10 ? themeCsv[9] : string.Empty
            };
        }

        protected override void LogoutEvent(object o)
        {
            base.LogoutEvent(o);
            ConnectTime = 0;
        }

        #endregion
    }
}