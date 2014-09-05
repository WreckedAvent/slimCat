#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HomeChannelViewModel.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//  
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Timers;
    using System.Windows.Media;
    using Microsoft.Practices.Unity;
    using Microsoft.VisualBasic.FileIO;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     Used for a few channels which are not treated normally and cannot receive/send messages.
    /// </summary>
    public class HomeChannelViewModel : ChannelViewModelBase, IHasTabs
    {
        #region Fields

        private readonly IAutomationService automation;

        private readonly IBrowser browser;

        private readonly StringBuilder connectDotDot;

        private readonly CacheCount minuteOnlineCount;

        private readonly Timer updateTimer = new Timer(1000); // every second

        private StringBuilder flavorText;

        private bool inStagger;

        private string selectedTab = "About";

        #endregion

        #region Constructors and Destructors

        public HomeChannelViewModel(string name, IChatState chatState, IAutomationService automation, IBrowser browser,
            HomeSettingsViewModel settingsVm)
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
                SettingsVm = settingsVm;

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

        public HomeSettingsViewModel SettingsVm { get; set; }

        public string SelectedTab
        {
            get { return selectedTab; }
            set
            {
                selectedTab = value;
                OnPropertyChanged("SelectedTab");
            }
        }

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

                var updateDelayTimer = new Timer(10*1000);
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

                    Dispatcher.BeginInvoke((Action) (() => Themes.Add(model)));
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