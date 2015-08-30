#region Copyright

// <copyright file="HomeChannelViewModel.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
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
        private readonly IUpdateMyself updateService;

        #region Constructors and Destructors

        public HomeChannelViewModel(string name, IChatState chatState, IAutomateThings automation, IBrowseThings browser,
            HomeSettingsViewModel settingsVm, HomeHelpViewModel helpVm, IUpdateMyself updateService)
            : base(chatState)
        {
            try
            {
                Model = Container.Resolve<GeneralChannelModel>(name);
                ConnectTime = 0;
                flavorText = new StringBuilder("Connecting");
                connectDotDot = new StringBuilder();

                this.updateService = updateService;
                HelpVm = helpVm;

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

                Events.GetEvent<NewUpdateEvent>().Subscribe(param =>
                {
                    var temp = param as CharacterUpdateModel;
                    if (!(temp?.Arguments is LoginStateChangedEventArgs))
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

        #region Fields

        private readonly IAutomateThings automation;

        private readonly IBrowseThings browser;

        private readonly StringBuilder connectDotDot;

        private readonly CacheCount minuteOnlineCount;

        private readonly Timer updateTimer = new Timer(1000); // every second

        private StringBuilder flavorText;

        private bool inStagger;

        private string selectedTab = "About";

        #endregion

        #region Public Properties

        #region Header

        public static string ClientIdString
            => $"{Constants.ClientId} {Constants.ClientNickname} ({Constants.ClientVersion})";

        public string LastMessageReceived => ChatModel.LastMessageReceived.DateTimeToRough(true, false)
            ;

        public int OnlineBookmarksCount => CharacterManager.GetNames(ListKind.Bookmark).Count;

        public int OnlineCount => CharacterManager.CharacterCount;

        public string OnlineCountChange => minuteOnlineCount.GetDisplayString();

        public int OnlineFriendsCount => CharacterManager.GetNames(ListKind.Friend).Count;

        public string RoughClientUpTime => ChatModel.ClientUptime.DateTimeToRough(true, false);

        public string RoughServerUpTime => ChatModel.ServerUpTime.DateTimeToRough(true, false);

        public int OnlineCountPrime() => OnlineCount;

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

        public override string EntryTextBoxIcon => "pack://application:,,,/icons/send_console.png";

        public override string EntryTextBoxLabel => "Enter commands here ...";

        #endregion

        public HomeSettingsViewModel SettingsVm { get; set; }

        public HomeHelpViewModel HelpVm { get; set; }
        public ICharacter slimCat => CharacterManager.Find("slimCat");

        public ChannelModel slimCatChannel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ApplicationSettings.SlimCatChannelId))
                    return null;

                return new GeneralChannelModel(ApplicationSettings.SlimCatChannelId, "slimCat", ChannelType.Private);
            }
        }

        public string SelectedTab
        {
            get { return selectedTab; }
            set
            {
                selectedTab = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Public Methods and Operators

        public void LoggedInEvent(bool? _)
        {
            updateTimer.Elapsed -= UpdateConnectText;

            automation.ResetStatusTimers();
            OnPropertyChanged("RecentChannels");
            OnPropertyChanged("RecentCharacters");
            SettingsVm.OnSettingsLoaded();

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

        private async void CheckForUpdates()
        {
            var latest = await updateService.GetLatestAsync();
            if (latest == null) return;

            await Dispatcher.BeginInvoke((Action) delegate
            {
                HasNewUpdate = latest.IsNewUpdate;
                UpdateName = latest.ClientName;
                UpdateLink = latest.DownloadLink;
                UpdateBuildTime = latest.PublishDate;
                ChangeLog = latest.ChangelogLink;

                OnPropertyChanged("HasNewUpdate");
                OnPropertyChanged("UpdateName");
                OnPropertyChanged("UpdateLink");
                OnPropertyChanged("UpdateBuildTime");
                OnPropertyChanged("ChangeLog");
            });

            if (!latest.IsNewUpdate) return;

            var updated = await updateService.TryUpdateAsync();
            var message = "Automatic update successful, restart to finish applying updates.";

            if (!updated) message = "Automatic update failed, please install update manually.";

            Events.NewError(message);
        }

        private void CheckForThemes()
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

            try
            {
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

        private static Color GetColor(string hex)
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