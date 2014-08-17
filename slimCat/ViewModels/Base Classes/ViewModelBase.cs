#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs">
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

    using System.Management.Instrumentation;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Modularity;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using System.Windows.Input;
    using Utilities;

    #endregion

    /// <summary>
    ///     The view model base.
    /// </summary>
    public abstract class ViewModelBase : SysProp, IModule
    {
        #region Fields

        private RelayCommand ban;

        private RelayCommand getLogs;

        private RelayCommand handleReport;

        private RelayCommand ignore;
        private RelayCommand ignoreUpdate;

        private RelayCommand invert;
        private RelayCommand isInterested;

        private RelayCommand isNotInterested;

        private RelayCommand @join;
        private RelayCommand kick;

        private RelayCommand link;

        private RelayCommand openMenu;

        private RelayCommand openPm;
        private RelayCommand report;

        private RelayCommand unignore;
        private RelayCommand searchTag;

        private RelayCommand advanceFriend;
        private RelayCommand bookmark;
        private RelayCommand regressFriend;

        #endregion

        #region Constructors and Destructors

        protected ViewModelBase(IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager manager)
        {
            try
            {
                Container = contain.ThrowIfNull("contain");
                RegionManager = regman.ThrowIfNull("regman");
                Events = events.ThrowIfNull("events");
                ChatModel = cm.ThrowIfNull("cm");
                CharacterManager = manager.ThrowIfNull("manager");

                RightClickMenuViewModel = new RightClickMenuViewModel(ChatModel.IsGlobalModerator, CharacterManager, Container.Resolve<IPermissionService>());
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

        #region Public Properties

        public bool SpellCheckEnabled
        {
            get { return ApplicationSettings.SpellCheckEnabled; }
            set
            {
                ApplicationSettings.SpellCheckEnabled = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                OnPropertyChanged("SpellCheckEnabled");
            }
        }

        public string Language
        {
            get { return ApplicationSettings.Langauge; }
            set
            {
                ApplicationSettings.Langauge = value;
                SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                OnPropertyChanged("Language");
            }
        }

        public static IEnumerable<string> Languages
        {
            get { return ApplicationSettings.LanguageList; }
        }

        public CreateReportViewModel CreateReportViewModel { get; private set; }


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
                    isLocalMod = channel.CharacterManager.IsOnList(ChatModel.CurrentCharacter.Name, ListKind.Moderator, false);

                return ChatModel.IsGlobalModerator || isLocalMod;
            }
        }

        public RightClickMenuViewModel RightClickMenuViewModel { get; private set; }

        public ICommand NavigateTo
        {
            get { return link ?? (link = new RelayCommand(StartLinkInDefaultBrowser)); }
        }

        /// <summary>
        ///     Gets or sets the chat model. The chat model is used to contain chat-related data unrelated to characters.
        /// </summary>
        /// <value>
        ///     The chat model.
        /// </value>
        public IChatModel ChatModel { get; set; }

        /// <summary>
        ///     Gets or sets the container. The container is used to resolve dependencies.
        /// </summary>
        /// <value>
        ///     The container.
        /// </value>
        protected IUnityContainer Container { get; set; }

        /// <summary>
        ///     Gets or sets the region manager. The region manager is used to handle the views being displayed.
        /// </summary>
        /// <value>
        ///     The region manager.
        /// </value>
        protected IRegionManager RegionManager { get; set; }

        /// <summary>
        ///     Gets or sets the events. The event aggregator is for intra program communication.
        /// </summary>
        /// <value>
        ///     The events.
        /// </value>
        protected IEventAggregator Events { get; set; }

        /// <summary>
        ///     Gets or sets the character manager. The character manage is used to handle all character data.
        /// </summary>
        /// <value>
        ///     The character manager.
        /// </value>
        protected ICharacterManager CharacterManager { get; set; }

        #region ICommands

        public ICommand BanCommand
        {
            get { return ban ?? (ban = new RelayCommand(BanEvent, param => HasPermissions)); }
        }

        public ICommand FindLogCommand
        {
            get { return getLogs ?? (getLogs = new RelayCommand(FindLogEvent)); }
        }

        public ICommand HandleReportCommand
        {
            get { return handleReport ?? (handleReport = new RelayCommand(HandleReportEvent)); }
        }

        public ICommand IgnoreCommand
        {
            get { return ignore ?? (ignore = new RelayCommand(AddIgnoreEvent, CanIgnore)); }
        }

        public ICommand InterestedCommand
        {
            get { return isInterested ?? (isInterested = new RelayCommand(IsInterestedEvent)); }
        }

        public ICommand InvertCommand
        {
            get { return invert ?? (invert = new RelayCommand(InvertButton)); }
        }

        public ICommand JoinChannelCommand
        {
            get { return @join ?? (@join = new RelayCommand(RequestChannelJoinEvent, CanJoinChannel)); }
        }

        public ICommand KickCommand
        {
            get { return kick ?? (kick = new RelayCommand(KickEvent, param => HasPermissions)); }
        }

        public ICommand NotInterestedCommand
        {
            get { return isNotInterested ?? (isNotInterested = new RelayCommand(IsUninterestedEvent)); }
        }

        public ICommand SearchTagCommand
        {
            get { return searchTag ?? (searchTag = new RelayCommand(SearchTagEvent)); }
        }


        public ICommand OpenRightClickMenuCommand
        {
            get
            {
                return openMenu ?? (openMenu = new RelayCommand(
                    args =>
                        {
                            var newTarget = CharacterManager.Find(args as string);
                            OnRightClickMenuUpdated(newTarget);
                        }));
            }
        }

        public ICommand ReportCommand
        {
            get { return report ?? (report = new RelayCommand(FileReportEvent)); }
        }

        public ICommand RequestPmCommand
        {
            get { return openPm ?? (openPm = new RelayCommand(RequestPmEvent, CanRequestPm)); }
        }

        public ICommand UnignoreCommand
        {
            get { return unignore ?? (unignore = new RelayCommand(RemoveIgnoreEvent, CanUnIgnore)); }
        }

        public ICommand IgnoreUpdateCommand
        {
            get { return ignoreUpdate ?? (ignoreUpdate = new RelayCommand(IgnoreUpdatesEvent, CanIgnoreUpdate)); }
        }

        public ICommand AdvanceFriendCommand
        {
            get { return advanceFriend ?? (advanceFriend = new RelayCommand(AdvanceFriendEvent)); }
        }

        public ICommand BookmarkCommand
        {
            get { return bookmark ?? (bookmark = new RelayCommand(BookmarkEvent)); }
        }

        public ICommand RegressFriendCommand
        {
            get { return regressFriend ?? (regressFriend = new RelayCommand(RegressFriendEvent)); }
        }

        #endregion

        #endregion

        #region Properties

        internal string LoggingSection { get; set; }

        #endregion

        #region Public Methods and Operators

        public abstract void Initialize();

        public bool CanHandleReport(object args)
        {
            return CharacterManager.Find(args as string).HasReport;
        }

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

            Events.SendUserCommand("priv", new[] {tabName});
        }

        private void StartLinkInDefaultBrowser(object linkToOpen)
        {
            Log("Opening link " + linkToOpen);
            var interpret = linkToOpen as string;
            if (string.IsNullOrEmpty(interpret)) return;

            // todo: show character profile in client
            if (!interpret.Contains(".") || interpret.Contains(" "))
            {
                if (interpret.EndsWith("/notes"))
                {
                    RequestPmEvent(interpret);
                    return;
                }

                interpret = "http://www.f-list.net/c/" + HttpUtility.UrlEncode(interpret);
            }

            Process.Start(interpret);
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
            Events.SendUserCommand("searchtag", new []{obj as string});
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
            Events.GetEvent<UserCommandEvent>()
                .Publish(CommandDefinitions.CreateCommand(interestedIn
                    ? "interesting"
                    : "notinteresting", new[] {args as string}).ToDictionary());

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

            Events.SendUserCommand(action, new[] { character });
        }

        private void BookmarkEvent(object obj)
        {
            var character = obj as string;

            Events.SendUserCommand(
                CharacterManager.IsOnList(character, ListKind.Bookmark, false) ? "removebookmark" : "addbookmark",
                new[] { character });
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

            Events.SendUserCommand(action, new[] { character });
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