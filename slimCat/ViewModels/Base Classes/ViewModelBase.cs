﻿#region Copyright

// <copyright file="ViewModelBase.cs">
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
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using System.Windows;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;

    #endregion

    /// <summary>
    ///     The view model base.
    /// </summary>
    public abstract class ViewModelBase : SysProp
    {
        #region Constructors and Destructors

        protected ViewModelBase(IChatState chatState)
        {
            try
            {
                Container = chatState.Container;
                RegionManager = chatState.RegionManager;
                Events = chatState.EventAggregator;
                ChatModel = chatState.ChatModel;
                CharacterManager = chatState.CharacterManager;
                ChatConnection = chatState.Connection;

                RightClickMenuViewModel = new RightClickMenuViewModel(ChatModel.IsGlobalModerator, CharacterManager,
                    Container.Resolve<IGetPermissions>());
                CreateReportViewModel = new CreateReportViewModel(Events, ChatModel);
                ChatModel.SelectedChannelChanged += OnSelectedChannelChanged;

                Events.GetEvent<NewUpdateEvent>().Subscribe(UpdateRightClickMenu);
            }
            catch (Exception ex)
            {
                ex.Source = "Generic ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Properties

        internal string LoggingSection { get; set; }

        #endregion

        #region Public Methods and Operators

        public bool CanHandleReport(object args) => CharacterManager.Find(args as string).HasReport;

        #endregion

        #region Fields

        private RelayCommand advanceFriend;
        private RelayCommand ban;
        private RelayCommand bookmark;

        private RelayCommand getLogs;

        private RelayCommand handleReport;

        private RelayCommand ignore;
        private RelayCommand ignoreUpdate;

        private RelayCommand invert;
        private RelayCommand isInterested;

        private RelayCommand isNotInterested;
        private RelayCommand isClientIgnored;

        private RelayCommand @join;
        private RelayCommand kick;

        private RelayCommand link;
        private RelayCommand linkCopy;

        private RelayCommand logout;
        private RelayCommand openMenu;

        private RelayCommand openPm;
        private RelayCommand regressFriend;
        private RelayCommand report;

        private RelayCommand searchTag;
        private RelayCommand unignore;

        #endregion

        #region Public Properties

        public bool SpellCheckEnabled
        {
            get { return ApplicationSettings.SpellCheckEnabled; }
            set
            {
                ApplicationSettings.SpellCheckEnabled = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                OnPropertyChanged();
            }
        }

        public string Language
        {
            get { return ApplicationSettings.Langauge; }
            set
            {
                ApplicationSettings.Langauge = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                OnPropertyChanged();
            }
        }

        public static IEnumerable<string> Languages => ApplicationSettings.LanguageList;

        public CreateReportViewModel CreateReportViewModel { get; }


        /// <summary>
        ///     Returns true if the current user has moderator permissions
        /// </summary>
        public bool HasPermissions
        {
            get
            {
                if (ChatModel.CurrentCharacter == null)
                    return false;

                var isLocalMod = false;
                var channel = ChatModel.CurrentChannel as GeneralChannelModel;

                if (channel != null)
                    isLocalMod = channel.CharacterManager.IsOnList(ChatModel.CurrentCharacter.Name, ListKind.Moderator,
                        false);

                return ChatModel.IsGlobalModerator || isLocalMod;
            }
        }

        public RightClickMenuViewModel RightClickMenuViewModel { get; private set; }

        public ICommand NavigateTo => link ?? (link = new RelayCommand(StartLinkInDefaultBrowser));

        public ICommand CopyLink => linkCopy ?? (linkCopy = new RelayCommand(CopyLinkToClipboard));

        /// <summary>
        ///     Gets or sets the chat model. The chat model is used to contain chat-related data unrelated to characters.
        /// </summary>
        public IChatModel ChatModel { get; set; }

        /// <summary>
        ///     Gets or sets the container. The container is used to resolve dependencies.
        /// </summary>
        protected IUnityContainer Container { get; set; }

        /// <summary>
        ///     Gets or sets the region manager. The region manager is used to handle the views being displayed.
        /// </summary>
        protected IRegionManager RegionManager { get; set; }

        /// <summary>
        ///     Gets or sets the events. The event aggregator is for intra program communication.
        /// </summary>
        protected IEventAggregator Events { get; set; }

        /// <summary>
        ///     Gets or sets the character manager. The character manage is used to handle all character data.
        /// </summary>
        protected ICharacterManager CharacterManager { get; set; }

        protected IHandleChatConnection ChatConnection { get; set; }

        #region ICommands

        public ICommand BanCommand => ban ?? (ban = new RelayCommand(BanEvent, param => HasPermissions));

        public ICommand FindLogCommand => getLogs ?? (getLogs = new RelayCommand(FindLogEvent));

        public ICommand HandleReportCommand => handleReport ?? (handleReport = new RelayCommand(HandleReportEvent));

        public ICommand IgnoreCommand => ignore ?? (ignore = new RelayCommand(AddIgnoreEvent, CanIgnore));

        public ICommand InterestedCommand => isInterested ?? (isInterested = new RelayCommand(IsInterestedEvent));

        public ICommand InvertCommand => invert ?? (invert = new RelayCommand(InvertButton));

        public ICommand JoinChannelCommand
            => @join ?? (@join = new RelayCommand(RequestChannelJoinEvent, CanJoinChannel));

        public ICommand KickCommand => kick ?? (kick = new RelayCommand(KickEvent, param => HasPermissions));

        public ICommand NotInterestedCommand
            => isNotInterested ?? (isNotInterested = new RelayCommand(IsUninterestedEvent));

        public ICommand ClientIgnoredCommand
            => isClientIgnored ?? (isClientIgnored = new RelayCommand(IsClientIgnoredEvent));

        public ICommand SearchTagCommand => searchTag ?? (searchTag = new RelayCommand(SearchTagEvent));

        public ICommand LogoutCommand => logout ?? (logout = new RelayCommand(LogoutEvent));

        public ICommand OpenRightClickMenuCommand => openMenu
                                                     ?? (openMenu = new RelayCommand(args =>
                                                     {
                                                         var newTarget = CharacterManager.Find(args as string);
                                                         OnRightClickMenuUpdated(newTarget);
                                                     }));

        public ICommand ReportCommand => report ?? (report = new RelayCommand(FileReportEvent));

        public ICommand RequestPmCommand => openPm ?? (openPm = new RelayCommand(RequestPmEvent, CanRequestPm));

        public ICommand UnignoreCommand => unignore ?? (unignore = new RelayCommand(RemoveIgnoreEvent, CanUnIgnore));

        public ICommand IgnoreUpdateCommand
            => ignoreUpdate ?? (ignoreUpdate = new RelayCommand(IgnoreUpdatesEvent, CanIgnoreUpdate));

        public ICommand AdvanceFriendCommand => advanceFriend ?? (advanceFriend = new RelayCommand(AdvanceFriendEvent));

        public ICommand BookmarkCommand => bookmark ?? (bookmark = new RelayCommand(BookmarkEvent));

        public ICommand RegressFriendCommand => regressFriend ?? (regressFriend = new RelayCommand(RegressFriendEvent));

        #endregion

        #endregion

        #region Methods

        protected virtual void InvertButton(object arguments)
        {
        }

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                ChatModel.SelectedChannelChanged -= OnSelectedChannelChanged;
                Events.GetEvent<NewUpdateEvent>().Unsubscribe(UpdateRightClickMenu);
                Container = null;
                RegionManager = null;
                ChatModel = null;
                Events = null;
                RightClickMenuViewModel.Dispose();
                RightClickMenuViewModel = null;
            }

            base.Dispose(isManaged);
        }

        private void OnSelectedChannelChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("HasPermissions");
            RightClickMenuViewModel.IsOpen = false;
            CreateReportViewModel.IsOpen = false;
        }

        private void OnRightClickMenuUpdated(ICharacter newTarget)
        {
            var name = newTarget.Name;
            RightClickMenuViewModel.SetNewTarget(newTarget, CanHandleReport(name));
            RightClickMenuViewModel.IsOpen = true;
            CreateReportViewModel.Target = name;
            OnPropertyChanged("RightClickMenuViewModel");
            OnPropertyChanged("CreateReportViewModel");
        }

        protected void RequestChannelJoinEvent(object args)
        {
            Events.SendUserCommand("join", new[] {args as string});
        }

        protected void RequestPmEvent(object args)
        {
            var tabName = (string) args;
            if (ChatModel.CurrentPms.Any(param => param.Id.Equals(tabName, StringComparison.OrdinalIgnoreCase)))
            {
                Events.GetEvent<RequestChangeTabEvent>().Publish(tabName);
                return;
            }

            Events.SendUserCommand("priv", new[] {$"\"{tabName}\""});
        }

        protected virtual void StartLinkInDefaultBrowser(object linkToOpen)
        {
            try
            {
                Log("Opening link " + linkToOpen);
                var interpret = linkToOpen as string;
                if (string.IsNullOrEmpty(interpret)) return;

                if (!interpret.Contains(".") || interpret.Contains(" "))
                {
                    if (interpret.EndsWith("/notes"))
                    {
                        Events.SendUserCommand("priv", new[] {interpret});
                        return;
                    }

                    if (!ApplicationSettings.OpenProfilesInClient)
                    {
                        Process.Start(Constants.UrlConstants.CharacterPage + HttpUtility.HtmlEncode(interpret));
                        return;
                    }

                    Events.SendUserCommand("priv", new[] {interpret + "/profile"});
                    return;
                }

                Process.Start(interpret);
            }
            catch
            {
                Log("Link encountered an error! " + linkToOpen);
                MessageBox.Show(
                    "Encountered an error opening the URL. Try right-click copy & pasting it into your browser instead.");
            }
        }

        protected void CopyLinkToClipboard(object linkToCopy)
        {
            if (!string.IsNullOrWhiteSpace(linkToCopy.ToString()))
                Clipboard.SetText(linkToCopy.ToString());
        }

        private void UpdateRightClickMenu(NotificationModel argument)
        {
            if (!RightClickMenuViewModel.IsOpen)
                return;

            var updateKind = argument as CharacterUpdateModel;
            if (updateKind == null)
                return;

            if (RightClickMenuViewModel.Target == null)
                return;

            if (updateKind.TargetCharacter.Name == RightClickMenuViewModel.Target.Name)
                OnRightClickMenuUpdated(RightClickMenuViewModel.Target);

            OnPropertyChanged("RightClickMenuViewModel");
        }

        #region Predicates for events

        private bool CanIgnore(object args)
        {
            return args is string && !CharacterManager.IsOnList(args as string, ListKind.Ignored, false);
        }

        private bool CanJoinChannel(object args)
        {
            return
                !ChatModel.CurrentChannels.Any(
                    param => param.Id.Equals((string) args, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanRequestPm(object args)
        {
            return true;
        }

        private bool CanUnIgnore(object args)
        {
            return !CanIgnore(args);
        }

        private bool CanIgnoreUpdate(object obj)
        {
            return CharacterManager.IsOfInterest(obj as string, false) ||
                   CharacterManager.IsOnList(obj as string, ListKind.IgnoreUpdates, false);
        }

        #endregion

        #region ICommand events

        private void AddIgnoreEvent(object args)
        {
            IgnoreEvent(args);
        }

        private void BanEvent(object args)
        {
            KickOrBanEvent(args, true);
        }

        private void FileReportEvent(object args)
        {
            RightClickMenuViewModel.IsOpen = false;
            OnPropertyChanged("RightClickMenuViewModel");

            CreateReportViewModel.IsOpen = true;
            OnPropertyChanged("CreateReportViewModel");
        }

        private void FindLogEvent(object args)
        {
            Events.SendUserCommand("openlogfolder", null, args as string);
        }

        private void SearchTagEvent(object obj)
        {
            Events.SendUserCommand("searchtag", new[] {obj as string});
            OnRightClickMenuUpdated(RightClickMenuViewModel.Target);
        }

        private void HandleReportEvent(object args)
        {
            Events.SendUserCommand("handlereport", new[] {args as string});
        }

        private void IgnoreEvent(object args, bool remove = false)
        {
            Events.SendUserCommand(remove ? "unignore" : "ignore", new[] {args as string});
            OnRightClickMenuUpdated(RightClickMenuViewModel.Target);
        }

        private void InterestedEvent(object args, bool interestedIn = true)
        {
            Events.SendUserCommand(
                interestedIn ? "interesting" : "notinteresting",
                new[] {args as string});

            OnRightClickMenuUpdated(RightClickMenuViewModel.Target);
        }

        private void ClientIgnoredEvent(object args, bool clientIgnored = false)
        {
            Events.SendUserCommand(
                clientIgnored ? "clientignored" : "clientunignored",
                new[] {args as string});
            OnRightClickMenuUpdated(RightClickMenuViewModel.Target);
        }

        private void IgnoreUpdatesEvent(object args)
        {
            Events.SendUserCommand("ignoreUpdates", new[] {args as string});
            OnRightClickMenuUpdated(RightClickMenuViewModel.Target);
        }

        private void IsInterestedEvent(object args)
        {
            InterestedEvent(args);
        }

        private void IsUninterestedEvent(object args)
        {
            InterestedEvent(args, false);
        }

        private void IsClientIgnoredEvent(object args)
        {
            ClientIgnoredEvent(args, false);
        }

        protected virtual void LogoutEvent(object o)
        {
            ChatConnection.Disconnect();
            RegionManager.RequestNavigate(Shell.MainRegion,
                new Uri(CharacterSelectViewModel.CharacterSelectViewName, UriKind.Relative));
        }

        private void KickEvent(object args)
        {
            KickOrBanEvent(args, false);
        }

        private void KickOrBanEvent(object args, bool isBan)
        {
            Events.SendUserCommand(isBan ? "ban" : "kick", new[] {args as string}, ChatModel.CurrentChannel.Id);
        }

        private void RemoveIgnoreEvent(object args)
        {
            IgnoreEvent(args, true);
        }

        private void RegressFriendEvent(object obj)
        {
            var character = obj as string;

            var actionSelected = false;
            var action = "";

            if (CharacterManager.IsOnList(character, ListKind.Friend, false))
            {
                action = "removefriend";
                actionSelected = true;
            }

            if (CharacterManager.IsOnList(character, ListKind.FriendRequestReceived, false) && !actionSelected)
            {
                action = "denyrequest";
                actionSelected = true;
            }

            if (CharacterManager.IsOnList(character, ListKind.FriendRequestSent, false) && !actionSelected)
                action = "cancelrequest";

            Events.SendUserCommand(action, new[] {character});
        }

        private void BookmarkEvent(object obj)
        {
            var character = obj as string;

            Events.SendUserCommand(
                CharacterManager.IsOnList(character, ListKind.Bookmark, false) ? "removebookmark" : "addbookmark",
                new[] {character});
        }

        private void AdvanceFriendEvent(object obj)
        {
            var character = obj as string;

            var actionSelected = false;
            var action = "";

            if (CharacterManager.IsOnList(character, ListKind.FriendRequestReceived, false))
            {
                action = "acceptrequest";
                actionSelected = true;
            }

            if (!CharacterManager.IsOnList(character, ListKind.Friend, false) && !actionSelected)
                action = "addfriend";

            Events.SendUserCommand(action, new[] {character});
        }

        [Conditional("DEBUG")]
        internal void Log(string text = null, bool isVerbose = false)
        {
            Logging.LogLine(text, LoggingSection, isVerbose);
        }

        [Conditional("DEBUG")]
        internal void Log(string text, object obj, bool isVerbose = false)
        {
            Logging.Log(text, LoggingSection, isVerbose);
            Logging.LogObject(obj);
            Logging.Log(null, null, isVerbose);
        }

        #endregion

        #endregion
    }
}