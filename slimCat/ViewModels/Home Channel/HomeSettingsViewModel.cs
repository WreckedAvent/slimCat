#region Copyright

// <copyright file="HomeSettingsViewModel.cs">
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

    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Models;
    using Services;
    using Utilities;

    #endregion

    public class HomeSettingsViewModel : ViewModelBase, IHasTabs
    {
        #region Constructors and Destructors

        public HomeSettingsViewModel(IChatState chatState, IAutomateThings automationService, IconService iconService,
            ICharacterManager characterManager)
            : base(chatState)
        {
            automation = automationService;
            this.iconService = iconService;
            this.characterManager = characterManager;
        }

        #endregion

        #region Fields

        private readonly IAutomateThings automation;

        private readonly ICharacterManager characterManager;
        private readonly IconService iconService;

        private string selectedTab = "General";

        #endregion

        #region Public Properties

        #region General

        public static IEnumerable<KeyValuePair<string, string>> LanguageNames { get; } = new Dictionary<string, string>
        {
            {"American English", "en-US"},
            {"British English", "en-GB"},
            {"French", "fr"},
            {"German", "de"},
            {"Spanish", "es"}
        };

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

        public static IEnumerable<KeyValuePair<string, GenderColorSettings>> GenderSettings { get; } = new Dictionary
            <string, GenderColorSettings>
        {
            {"No Coloring", GenderColorSettings.None},
            {"Minimal Coloring", GenderColorSettings.GenderOnly},
            {"Moderate Coloring", GenderColorSettings.GenderAndHerm},
            {"Full Coloring", GenderColorSettings.Full}
        };

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

        public bool AllowAlignment
        {
            get { return ApplicationSettings.AllowAlignment; }
            set
            {
                ApplicationSettings.AllowAlignment = value;
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

                OnPropertyChanged();
                Save();
            }
        }

        public int EntryFontSize
        {
            get { return ApplicationSettings.EntryFontSize; }
            set
            {
                if (value >= 8 && value <= 20)
                    ApplicationSettings.EntryFontSize = value;

                OnPropertyChanged();
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

        public bool AllowOfInterestColoring
        {
            get { return ApplicationSettings.AllowOfInterestColoring; }
            set
            {
                ApplicationSettings.AllowOfInterestColoring = value;
                Save();
            }
        }

        public bool StickMessageToBottom
        {
            get { return ApplicationSettings.StickNewMessagesToBottom; }
            set
            {
                ApplicationSettings.StickNewMessagesToBottom = value;
                Save();
            }
        }

        public int PreloadMessageAmount
        {
            get { return ApplicationSettings.PreloadMessageAmount; }
            set
            {
                ApplicationSettings.PreloadMessageAmount = value;
                Save();
                OnPropertyChanged();
            }
        }

        public bool UseCustomTimeStamp
        {
            get { return ApplicationSettings.UseCustomTimeStamp; }
            set
            {
                ApplicationSettings.UseCustomTimeStamp = value;
                Save();
                OnPropertyChanged();
            }
        }

        public string CustomTimeStamp
        {
            get { return ApplicationSettings.CustomTimeStamp; }
            set
            {
                ApplicationSettings.CustomTimeStamp = value;
                Save();
                OnPropertyChanged();
            }
        }

        public bool AllowAvatars
        {
            get { return ApplicationSettings.ShowAvatars; }
            set
            {
                ApplicationSettings.ShowAvatars = value;
                Save();
            }
        }

        public bool SortUsersAlphabetically
        {
            get { return ApplicationSettings.SortUsersAlphabetically; }
            set
            {
                ApplicationSettings.SortUsersAlphabetically = value;
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
                OnPropertyChanged();
                automation.ResetStatusTimers();
                Save();
            }
        }

        public bool WipeNotificationsOnTabChange
        {
            get { return ApplicationSettings.WipeNotificationsOnTabChange; }
            set
            {
                ApplicationSettings.WipeNotificationsOnTabChange = value;
                Save();
            }
        }

        public int AutoIdleTime
        {
            get { return ApplicationSettings.AutoIdleTime; }
            set
            {
                ApplicationSettings.AutoIdleTime = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                    var characters = characterManager.Characters.Where(x => x.LastAd != null).ToList();
                    characters.Each(x => x.LastAd = null);
                }

                Save();
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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

        public bool AllowMarkupPastedLink
        {
            get { return ApplicationSettings.AllowMarkupPastedLinks; }
            set
            {
                ApplicationSettings.AllowMarkupPastedLinks = value;
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
                ApplicationSettings.AllowSound = value;
                iconService.AllowSoundUpdate();
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

        public bool ToastsAreLocatedAtTop
        {
            get { return ApplicationSettings.ToastsAreLocatedAtTop; }
            set
            {
                ApplicationSettings.ToastsAreLocatedAtTop = value;
                Save();
            }
        }

        public bool ShowNotifications
        {
            get { return ApplicationSettings.ShowNotificationsGlobal; }

            set
            {
                ApplicationSettings.ShowNotificationsGlobal = value;
                iconService.AllowToastUpdate();
                Save();
                OnPropertyChanged();
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

        public bool DoNotAlertWhenDnd
        {
            get { return ApplicationSettings.DisallowNotificationsWhenDnd; }
            set
            {
                ApplicationSettings.DisallowNotificationsWhenDnd = value;
                Save();
            }
        }

        public bool ShowLoginToasts
        {
            get { return ApplicationSettings.ShowLoginToasts; }
            set
            {
                ApplicationSettings.ShowLoginToasts = value;
                Save();
            }
        }

        public bool ShowStatusToasts
        {
            get { return ApplicationSettings.ShowStatusToasts; }
            set
            {
                ApplicationSettings.ShowStatusToasts = value;
                Save();
            }
        }

        public bool ShowAvatarsInToasts
        {
            get { return ApplicationSettings.ShowAvatarsInToasts; }
            set
            {
                ApplicationSettings.ShowAvatarsInToasts = value;
                Save();
            }
        }

        public bool ShowNamesInToasts
        {
            get { return ApplicationSettings.ShowNamesInToasts; }
            set
            {
                ApplicationSettings.ShowNamesInToasts = value;
                Save();
            }
        }

        public bool ShowMessagesInToasts
        {
            get { return ApplicationSettings.ShowMessagesInToasts; }
            set
            {
                ApplicationSettings.ShowMessagesInToasts = value;
                Save();
            }
        }

        #endregion

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

        #region Methods

        public void OnSettingsLoaded()
        {
            GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Each(x => OnPropertyChanged(x.Name));
        }

        private void Save()
        {
            ApplicationSettings.SettingsVersion = Constants.ClientVersion;
            SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
        }

        #endregion
    }
}