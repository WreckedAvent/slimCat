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

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Modularity;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
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

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ViewModelBase" /> class.
        /// </summary>
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

                RightClickMenuViewModel = new RightClickMenuViewModel(ChatModel.IsGlobalModerator, CharacterManager);
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
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                OnPropertyChanged("SpellCheckEnabled");
            }
        }

        public string Language
        {
            get { return ApplicationSettings.Langauge; }
            set
            {
                ApplicationSettings.Langauge = value;
                SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                OnPropertyChanged("Language");
            }
        }

        public static IEnumerable<string> Languages
        {
            get { return ApplicationSettings.LanguageList; }
        }

        /// <summary>
        ///     Gets the ban command.
        /// </summary>
        public ICommand BanCommand
        {
            get { return ban ?? (ban = new RelayCommand(BanEvent, param => HasPermissions)); }
        }

        /// <summary>
        ///     Gets the create report view model.
        /// </summary>
        public CreateReportViewModel CreateReportViewModel { get; private set; }

        /// <summary>
        ///     Gets the find log command.
        /// </summary>
        public ICommand FindLogCommand
        {
            get { return getLogs ?? (getLogs = new RelayCommand(FindLogEvent)); }
        }

        /// <summary>
        ///     Gets the handle report command.
        /// </summary>
        public ICommand HandleReportCommand
        {
            get { return handleReport ?? (handleReport = new RelayCommand(HandleReportEvent)); }
        }

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
                    isLocalMod = channel.CharacterManager.IsOnList(ChatModel.CurrentCharacter.Name, ListKind.Moderator);

                return ChatModel.IsGlobalModerator || isLocalMod;
            }
        }

        /// <summary>
        ///     Gets the ignore command.
        /// </summary>
        public ICommand IgnoreCommand
        {
            get { return ignore ?? (ignore = new RelayCommand(AddIgnoreEvent, CanIgnore)); }
        }

        /// <summary>
        ///     Gets the interested command.
        /// </summary>
        public ICommand InterestedCommand
        {
            get { return isInterested ?? (isInterested = new RelayCommand(IsInterestedEvent)); }
        }

        /// <summary>
        ///     Gets the invert button command.
        /// </summary>
        public ICommand InvertCommand
        {
            get { return invert ?? (invert = new RelayCommand(InvertButton)); }
        }

        /// <summary>
        ///     Gets the join channel command.
        /// </summary>
        public ICommand JoinChannelCommand
        {
            get { return @join ?? (@join = new RelayCommand(RequestChannelJoinEvent, CanJoinChannel)); }
        }

        /// <summary>
        ///     Gets the kick command.
        /// </summary>
        public ICommand KickCommand
        {
            get { return kick ?? (kick = new RelayCommand(KickEvent, param => HasPermissions)); }
        }

        /// <summary>
        ///     Gets the navigate to.
        /// </summary>
        public ICommand NavigateTo
        {
            get { return link ?? (link = new RelayCommand(StartLinkInDefaultBrowser)); }
        }

        /// <summary>
        ///     Gets the not interested command.
        /// </summary>
        public ICommand NotInterestedCommand
        {
            get { return isNotInterested ?? (isNotInterested = new RelayCommand(IsUninterestedEvent)); }
        }

        /// <summary>
        ///     Gets the open right click menu command.
        /// </summary>
        public ICommand OpenRightClickMenuCommand
        {
            get
            {
                return openMenu ?? (openMenu = new RelayCommand(
                    args =>
                        {
                            var newTarget = CharacterManager.Find(args as string);
                            updateRightClickMenu(newTarget);
                        }));
            }
        }

        /// <summary>
        ///     Gets the report command.
        /// </summary>
        public ICommand ReportCommand
        {
            get { return report ?? (report = new RelayCommand(FileReportEvent)); }
        }

        /// <summary>
        ///     Gets the request PrivateMessage command.
        /// </summary>
        public ICommand RequestPmCommand
        {
            get { return openPm ?? (openPm = new RelayCommand(RequestPmEvent, CanRequestPm)); }
        }

        /// <summary>
        ///     Gets the right click menu view model.
        /// </summary>
        public RightClickMenuViewModel RightClickMenuViewModel { get; private set; }

        /// <summary>
        ///     Gets the unignore command.
        /// </summary>
        public ICommand UnignoreCommand
        {
            get { return unignore ?? (unignore = new RelayCommand(RemoveIgnoreEvent, CanUnIgnore)); }
        }

        /// <summary>
        ///     CM is the general reference to the ChatModel, which is central to anything which needs to interact with session
        ///     data
        /// </summary>
        public IChatModel ChatModel { get; set; }

        protected IUnityContainer Container { get; set; }

        protected IRegionManager RegionManager { get; set; }

        protected IEventAggregator Events { get; set; }

        protected ICharacterManager CharacterManager { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        ///     The can handle report.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
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

        private void updateRightClickMenu(ICharacter newTarget)
        {
            var name = newTarget.Name;
            RightClickMenuViewModel.SetNewTarget(newTarget, CanHandleReport(name));
            RightClickMenuViewModel.IsOpen = true;
            CreateReportViewModel.Target = name;
            OnPropertyChanged("RightClickMenuViewModel");
            OnPropertyChanged("CreateReportViewModel");
        }

        /// <summary>
        ///     The add ignore event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void AddIgnoreEvent(object args)
        {
            IgnoreEvent(args);
        }

        /// <summary>
        ///     The ban event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void BanEvent(object args)
        {
            KickOrBanEvent(args, true);
        }

        /// <summary>
        ///     The can ignore.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool CanIgnore(object args)
        {
            return args is string && !CharacterManager.IsOnList(args as string, ListKind.Ignored, false);
        }

        /// <summary>
        ///     The can join channel.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool CanJoinChannel(object args)
        {
            return
                !ChatModel.CurrentChannels.Any(
                    param => param.Id.Equals((string) args, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     The can request PrivateMessage.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool CanRequestPm(object args)
        {
            return true;
        }

        /// <summary>
        ///     The can un ignore.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool CanUnIgnore(object args)
        {
            return !CanIgnore(args);
        }

        /// <summary>
        ///     The file report event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void FileReportEvent(object args)
        {
            RightClickMenuViewModel.IsOpen = false;
            OnPropertyChanged("RightClickMenuViewModel");

            CreateReportViewModel.IsOpen = true;
            OnPropertyChanged("CreateReportViewModel");
        }

        /// <summary>
        ///     The find log event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void FindLogEvent(object args)
        {
            var name = args as string;

            var command =
                CommandDefinitions.CreateCommand("openlogfolder", null, name).ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        ///     The handle report event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void HandleReportEvent(object args)
        {
            var name = args as string;

            var command =
                CommandDefinitions.CreateCommand("handlereport", new List<string> {name}).ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        ///     The ignore event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <param name="remove">
        ///     The remove.
        /// </param>
        private void IgnoreEvent(object args, bool remove = false)
        {
            var name = args as string;

            var command =
                CommandDefinitions.CreateCommand(remove ? "unignore" : "ignore", new List<string> {name})
                    .ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(command);
            updateRightClickMenu(RightClickMenuViewModel.Target); // updates the ignore/unignore text
        }

        /// <summary>
        ///     The interested event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <param name="interestedIn">
        ///     The interested in.
        /// </param>
        private void InterestedEvent(object args, bool interestedIn = true)
        {
            Events.GetEvent<UserCommandEvent>()
                .Publish(
                    interestedIn
                        ? CommandDefinitions.CreateCommand("interesting", new[] {args as string}).ToDictionary()
                        : CommandDefinitions.CreateCommand("notinteresting", new[] {args as string}).ToDictionary());
        }

        /// <summary>
        ///     The is interested event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void IsInterestedEvent(object args)
        {
            InterestedEvent(args);
        }

        /// <summary>
        ///     The is uninterested event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void IsUninterestedEvent(object args)
        {
            InterestedEvent(args, false);
        }

        /// <summary>
        ///     The kick event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void KickEvent(object args)
        {
            KickOrBanEvent(args, false);
        }

        /// <summary>
        ///     The kick or ban event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <param name="isBan">
        ///     The is ban.
        /// </param>
        private void KickOrBanEvent(object args, bool isBan)
        {
            var name = args as string;

            var command =
                CommandDefinitions.CreateCommand(isBan ? "ban" : "kick", new[] {name}, ChatModel.CurrentChannel.Id)
                    .ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        ///     The on selected channel changed.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void OnSelectedChannelChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("HasPermissions");
            RightClickMenuViewModel.IsOpen = false;
            CreateReportViewModel.IsOpen = false;
        }

        /// <summary>
        ///     The remove ignore event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        private void RemoveIgnoreEvent(object args)
        {
            IgnoreEvent(args, true);
        }

        /// <summary>
        ///     The request channel join event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        protected void RequestChannelJoinEvent(object args)
        {
            var command =
                CommandDefinitions.CreateCommand("join", new List<string> {args as string}).ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        ///     The request PrivateMessage event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        protected void RequestPmEvent(object args)
        {
            var tabName = (string) args;
            if (ChatModel.CurrentPms.Any(param => param.Id.Equals(tabName, StringComparison.OrdinalIgnoreCase)))
            {
                Events.GetEvent<RequestChangeTabEvent>().Publish(tabName);
                return;
            }

            var command =
                CommandDefinitions.CreateCommand("priv", new List<string> {tabName}).ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        ///     The start link in default browser.
        /// </summary>
        /// <param name="linkToOpen">
        ///     The link.
        /// </param>
        private void StartLinkInDefaultBrowser(object linkToOpen)
        {
            var interpret = linkToOpen as string;
            if (interpret != null && (!interpret.Contains(".") || interpret.Contains(" ")))
                interpret = "http://www.f-list.net/c/" + HttpUtility.UrlEncode(interpret);

            if (!string.IsNullOrEmpty(interpret))
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
                updateRightClickMenu(RightClickMenuViewModel.Target);

            OnPropertyChanged("RightClickMenuViewModel");
        }

        #endregion
    }
}