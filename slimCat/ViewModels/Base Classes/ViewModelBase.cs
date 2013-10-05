// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The view model base.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Modularity;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat;
    using Slimcat.Libraries;
    using Slimcat.Models;
    using Slimcat.Services;
    using Slimcat.Utilities;

    /// <summary>
    ///     The view model base.
    /// </summary>
    public abstract class ViewModelBase : SysProp, IModule
    {
        #region Fields
        private RelayCommand ban;

        private RelayCommand getLogs;

        private RelayCommand handleReport;

        private RelayCommand isInterested;

        private RelayCommand isNotInterested;

        private RelayCommand kick;

        private RelayCommand report;

        private RelayCommand ignore;

        private RelayCommand invert;

        private RelayCommand @join;

        private RelayCommand link;

        private RelayCommand openMenu;

        private RelayCommand openPm;

        private RelayCommand unignore;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        protected ViewModelBase(IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
        {
            try
            {
                this.Container = contain.ThrowIfNull("contain");
                this.RegionManager = regman.ThrowIfNull("regman");
                this.Events = events.ThrowIfNull("events");
                this.ChatModel = cm.ThrowIfNull("cm");

                this.RightClickMenuViewModel = new RightClickMenuViewModel(this.ChatModel.IsGlobalModerator);
                this.CreateReportViewModel = new CreateReportViewModel(this.Events, this.ChatModel);
                this.ChatModel.SelectedChannelChanged += this.OnSelectedChannelChanged;

                this.Events.GetEvent<NewUpdateEvent>().Subscribe(this.UpdateRightClickMenu);
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
            get
            {
                return ApplicationSettings.SpellCheckEnabled;
            }
            set
            {
                ApplicationSettings.SpellCheckEnabled = value;
                SettingsDaemon.SaveApplicationSettingsToXml(this.ChatModel.CurrentCharacter.Name);
                this.OnPropertyChanged("SpellCheckEnabled");
            }
        }

        public string Language
        {
            get
            {
                return ApplicationSettings.Langauge;
            }
            set
            {
                ApplicationSettings.Langauge = value;
                SettingsDaemon.SaveApplicationSettingsToXml(this.ChatModel.CurrentCharacter.Name);
                this.OnPropertyChanged("Language");
            }
        }

        public static IEnumerable<string> Languages
        {
            get
            {
                return ApplicationSettings.LanguageList;
            }
        }

        /// <summary>
        ///     Gets the ban command.
        /// </summary>
        public ICommand BanCommand
        {
            get
            {
                return this.ban ?? (this.ban = new RelayCommand(this.BanEvent, param => this.HasPermissions));
            }
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
            get
            {
                return this.getLogs ?? (this.getLogs = new RelayCommand(this.FindLogEvent));
            }
        }

        /// <summary>
        ///     Gets the handle report command.
        /// </summary>
        public ICommand HandleReportCommand
        {
            get
            {
                return this.handleReport ?? (this.handleReport = new RelayCommand(this.HandleReportEvent));
            }
        }

        /// <summary>
        ///     Returns true if the current user has moderator permissions
        /// </summary>
        public bool HasPermissions
        {
            get
            {
                if (this.ChatModel.CurrentCharacter == null)
                {
                    return false;
                }

                var isLocalMod = false;
                if (this.ChatModel.CurrentChannel is GeneralChannelModel)
                {
                    isLocalMod =
                        (this.ChatModel.CurrentChannel as GeneralChannelModel).Moderators.Contains(
                            this.ChatModel.CurrentCharacter.Name);
                }

                return this.ChatModel.IsGlobalModerator || isLocalMod;
            }
        }

        /// <summary>
        ///     Gets the ignore command.
        /// </summary>
        public ICommand IgnoreCommand
        {
            get
            {
                return this.ignore ?? (this.ignore = new RelayCommand(this.AddIgnoreEvent, this.CanIgnore));
            }
        }

        /// <summary>
        ///     Gets the interested command.
        /// </summary>
        public ICommand InterestedCommand
        {
            get
            {
                return this.isInterested ?? (this.isInterested = new RelayCommand(this.IsInterestedEvent));
            }
        }

        /// <summary>
        ///     Gets the invert button command.
        /// </summary>
        public ICommand InvertCommand
        {
            get
            {
                return this.invert ?? (this.invert = new RelayCommand(this.InvertButton));
            }
        }

        /// <summary>
        ///     Gets the join channel command.
        /// </summary>
        public ICommand JoinChannelCommand
        {
            get
            {
                return this.@join ?? (this.@join = new RelayCommand(this.RequestChannelJoinEvent, this.CanJoinChannel));
            }
        }

        /// <summary>
        ///     Gets the kick command.
        /// </summary>
        public ICommand KickCommand
        {
            get
            {
                return this.kick ?? (this.kick = new RelayCommand(this.KickEvent, param => this.HasPermissions));
            }
        }

        /// <summary>
        ///     Gets the navigate to.
        /// </summary>
        public ICommand NavigateTo
        {
            get
            {
                return this.link ?? (this.link = new RelayCommand(this.StartLinkInDefaultBrowser));
            }
        }

        /// <summary>
        ///     Gets the not interested command.
        /// </summary>
        public ICommand NotInterestedCommand
        {
            get
            {
                return this.isNotInterested ?? (this.isNotInterested = new RelayCommand(this.IsUninterestedEvent));
            }
        }

        /// <summary>
        ///     Gets the open right click menu command.
        /// </summary>
        public ICommand OpenRightClickMenuCommand
        {
            get
            {
                return this.openMenu ?? (this.openMenu = new RelayCommand(
                        args =>
                            {
                                var newTarget =
                                    this.ChatModel.FindCharacter(args as string);
                                this.updateRightClickMenu(newTarget);
                            }));
            }
        }

        /// <summary>
        ///     Gets the report command.
        /// </summary>
        public ICommand ReportCommand
        {
            get
            {
                return this.report ?? (this.report = new RelayCommand(this.FileReportEvent));
            }
        }

        /// <summary>
        ///     Gets the request PrivateMessage command.
        /// </summary>
        public ICommand RequestPmCommand
        {
            get
            {
                return this.openPm ?? (this.openPm = new RelayCommand(this.RequestPmEvent, this.CanRequestPm));
            }
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
            get
            {
                return this.unignore ?? (this.unignore = new RelayCommand(this.RemoveIgnoreEvent, this.CanUnIgnore));
            }
        }

        /// <summary>
        ///     CM is the general reference to the ChatModel, which is central to anything which needs to interact with session data
        /// </summary>
        public IChatModel ChatModel { get; set; }

        protected IUnityContainer Container { get; set; }

        protected IRegionManager RegionManager { get; set; }

        protected IEventAggregator Events { get; set; }
        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The can handle report.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool CanHandleReport(object args)
        {
            return this.ChatModel.FindCharacter(args as string).HasReport;
        }

        /// <summary>
        ///     The initialize.
        /// </summary>
        public abstract void Initialize();

        #endregion

        #region Methods

        protected virtual void InvertButton(object arguments)
        {
        }

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {

                this.ChatModel.SelectedChannelChanged -= this.OnSelectedChannelChanged;
                this.Events.GetEvent<NewUpdateEvent>().Unsubscribe(this.UpdateRightClickMenu);
                this.Container = null;
                this.RegionManager = null;
                this.ChatModel = null;
                this.Events = null;
                this.RightClickMenuViewModel.Dispose();
                this.RightClickMenuViewModel = null;
            }

            base.Dispose(isManaged);
        }

        private void updateRightClickMenu(ICharacter newTarget)
        {
            var name = newTarget.Name;
            this.RightClickMenuViewModel.SetNewTarget(
                newTarget, this.CanIgnore(name), this.CanUnIgnore(name), this.CanHandleReport(name));
            this.RightClickMenuViewModel.IsOpen = true;
            this.CreateReportViewModel.Target = name;
            this.OnPropertyChanged("RightClickMenuViewModel");
            this.OnPropertyChanged("CreateReportViewModel");
        }

        /// <summary>
        /// The add ignore event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void AddIgnoreEvent(object args)
        {
            this.IgnoreEvent(args);
        }

        /// <summary>
        /// The ban event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void BanEvent(object args)
        {
            this.KickOrBanEvent(args, true);
        }

        /// <summary>
        /// The can ignore.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool CanIgnore(object args)
        {
            return !this.ChatModel.Ignored.Contains(args as string, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The can join channel.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool CanJoinChannel(object args)
        {
            return
                !this.ChatModel.CurrentChannels.Any(
                    param => param.Id.Equals((string)args, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The can request PrivateMessage.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool CanRequestPm(object args)
        {
            return true;
        }

        /// <summary>
        /// The can un ignore.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool CanUnIgnore(object args)
        {
            return !this.CanIgnore(args);
        }

        /// <summary>
        /// The file report event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void FileReportEvent(object args)
        {
            this.RightClickMenuViewModel.IsOpen = false;
            this.OnPropertyChanged("RightClickMenuViewModel");

            this.CreateReportViewModel.IsOpen = true;
            this.OnPropertyChanged("CreateReportViewModel");
        }

        /// <summary>
        /// The find log event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void FindLogEvent(object args)
        {
            var name = args as string;

            var command =
                CommandDefinitions.CreateCommand("openlogfolder", null, name).ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// The handle report event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void HandleReportEvent(object args)
        {
            var name = args as string;

            var command =
                CommandDefinitions.CreateCommand("handlereport", new List<string> { name }).ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// The ignore event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <param name="remove">
        /// The remove.
        /// </param>
        private void IgnoreEvent(object args, bool remove = false)
        {
            var name = args as string;

            var command =
                CommandDefinitions.CreateCommand(remove ? "unignore" : "ignore", new List<string> { name })
                                  .ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(command);
            this.updateRightClickMenu(this.RightClickMenuViewModel.Target); // updates the ignore/unignore text
        }

        /// <summary>
        /// The interested event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <param name="interestedIn">
        /// The interested in.
        /// </param>
        private void InterestedEvent(object args, bool interestedIn = true)
        {
            this.Events.GetEvent<UserCommandEvent>()
                .Publish(
                    interestedIn
                        ? CommandDefinitions.CreateCommand("interesting", new[] { args as string }).ToDictionary()
                        : CommandDefinitions.CreateCommand("notinteresting", new[] { args as string }).ToDictionary());
        }

        /// <summary>
        /// The is interested event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void IsInterestedEvent(object args)
        {
            this.InterestedEvent(args);
        }

        /// <summary>
        /// The is uninterested event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void IsUninterestedEvent(object args)
        {
            this.InterestedEvent(args, false);
        }

        /// <summary>
        /// The kick event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void KickEvent(object args)
        {
            this.KickOrBanEvent(args, false);
        }

        /// <summary>
        /// The kick or ban event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <param name="isBan">
        /// The is ban.
        /// </param>
        private void KickOrBanEvent(object args, bool isBan)
        {
            var name = args as string;

            var command =
                CommandDefinitions.CreateCommand(isBan ? "ban" : "kick", new[] { name }, this.ChatModel.CurrentChannel.Id)
                                  .ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// The on selected channel changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void OnSelectedChannelChanged(object sender, EventArgs e)
        {
            this.OnPropertyChanged("HasPermissions");
            this.RightClickMenuViewModel.IsOpen = false;
            this.CreateReportViewModel.IsOpen = false;
        }

        /// <summary>
        /// The remove ignore event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void RemoveIgnoreEvent(object args)
        {
            this.IgnoreEvent(args, true);
        }

        /// <summary>
        /// The request channel join event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void RequestChannelJoinEvent(object args)
        {
            var command =
                CommandDefinitions.CreateCommand("join", new List<string> { args as string }).ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// The request PrivateMessage event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void RequestPmEvent(object args)
        {
            var tabName = (string)args;
            if (this.ChatModel.CurrentPms.Any(param => param.Id.Equals(tabName, StringComparison.OrdinalIgnoreCase)))
            {
                this.Events.GetEvent<RequestChangeTabEvent>().Publish(tabName);
                return;
            }

            var command = CommandDefinitions.CreateCommand("priv", new List<string> { tabName }).ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// The start link in default browser.
        /// </summary>
        /// <param name="link">
        /// The link.
        /// </param>
        private void StartLinkInDefaultBrowser(object link)
        {
            var interpret = link as string;
            if (!interpret.Contains(".") || interpret.Contains(" "))
            {
                interpret = "http://www.f-list.net/c/" + HttpUtility.UrlEncode(interpret);
            }

            if (!string.IsNullOrEmpty(interpret))
            {
                Process.Start(interpret);
            }
        }

        private void UpdateRightClickMenu(NotificationModel argument)
        {
            if (!this.RightClickMenuViewModel.IsOpen)
            {
                return;
            }

            var updateKind = argument as CharacterUpdateModel;
            if (updateKind == null)
            {
                return;
            }

            if (this.RightClickMenuViewModel.Target == null)
            {
                return;
            }

            if (updateKind.TargetCharacter.Name == this.RightClickMenuViewModel.Target.Name)
            {
                this.updateRightClickMenu(this.RightClickMenuViewModel.Target);
            }

            this.OnPropertyChanged("RightClickMenuViewModel");
        }

        #endregion
    }
}