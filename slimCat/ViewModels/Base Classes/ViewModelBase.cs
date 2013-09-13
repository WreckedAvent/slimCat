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

namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Modularity;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using slimCat;

    /// <summary>
    ///     The view model base.
    /// </summary>
    public abstract class ViewModelBase : SysProp, IModule, IDisposable
    {
        #region Fields

        /// <summary>
        ///     The _ban.
        /// </summary>
        protected RelayCommand _ban;

        /// <summary>
        ///     The _cm.
        /// </summary>
        protected IChatModel _cm;

        /// <summary>
        ///     The _container.
        /// </summary>
        protected IUnityContainer _container;

        /// <summary>
        ///     The _events.
        /// </summary>
        protected IEventAggregator _events;

        /// <summary>
        ///     The _getlogs.
        /// </summary>
        protected RelayCommand _getlogs;

        /// <summary>
        ///     The _handle report.
        /// </summary>
        protected RelayCommand _handleReport;

        /// <summary>
        ///     The _is inter.
        /// </summary>
        protected RelayCommand _isInter;

        /// <summary>
        ///     The _is n inter.
        /// </summary>
        protected RelayCommand _isNInter;

        /// <summary>
        ///     The _kick.
        /// </summary>
        protected RelayCommand _kick;

        /// <summary>
        ///     The _region.
        /// </summary>
        protected IRegionManager _region;

        /// <summary>
        ///     The _report.
        /// </summary>
        protected RelayCommand _report;

        private readonly CreateReportViewModel _crvm;

        private RelayCommand _ign;

        private RelayCommand _invertButton;

        private RelayCommand _join;

        private RelayCommand _link;

        private RelayCommand _openMenu;

        private RelayCommand _priv;

        private RightClickMenuViewModel _rcmvm;

        private RelayCommand _uign;

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
        public ViewModelBase(IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
        {
            try
            {
                this._container = contain.ThrowIfNull("contain");
                this._region = regman.ThrowIfNull("regman");
                this._events = events.ThrowIfNull("events");
                this._cm = cm.ThrowIfNull("cm");

                this._rcmvm = new RightClickMenuViewModel(this._cm.IsGlobalModerator);
                this._crvm = new CreateReportViewModel(this._events, this._cm);
                this._cm.SelectedChannelChanged += this.OnSelectedChannelChanged;

                this._events.GetEvent<NewUpdateEvent>().Subscribe(this.UpdateRightClickMenu);
            }
            catch (Exception ex)
            {
                ex.Source = "Generic ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the ban command.
        /// </summary>
        public ICommand BanCommand
        {
            get
            {
                if (this._ban == null)
                {
                    this._ban = new RelayCommand(this.BanEvent, param => this.HasPermissions);
                }

                return this._ban;
            }
        }

        /// <summary>
        ///     CM is the general reference to the ChatModel, which is central to anything which needs to interact with session data
        /// </summary>
        public IChatModel CM
        {
            get
            {
                return this._cm;
            }
        }

        /// <summary>
        ///     Gets the create report view model.
        /// </summary>
        public CreateReportViewModel CreateReportViewModel
        {
            get
            {
                return this._crvm;
            }
        }

        /// <summary>
        ///     Gets the find log command.
        /// </summary>
        public ICommand FindLogCommand
        {
            get
            {
                if (this._getlogs == null)
                {
                    this._getlogs = new RelayCommand(this.FindLogEvent);
                }

                return this._getlogs;
            }
        }

        /// <summary>
        ///     Gets the handle report command.
        /// </summary>
        public ICommand HandleReportCommand
        {
            get
            {
                if (this._handleReport == null)
                {
                    this._handleReport = new RelayCommand(this.HandleReportEvent);
                }

                return this._handleReport;
            }
        }

        /// <summary>
        ///     Returns true if the current user has moderator permissions
        /// </summary>
        public bool HasPermissions
        {
            get
            {
                if (this._cm.SelectedCharacter == null)
                {
                    return false;
                }

                bool isLocalMod = false;
                if (this._cm.SelectedChannel is GeneralChannelModel)
                {
                    isLocalMod =
                        (this._cm.SelectedChannel as GeneralChannelModel).Moderators.Contains(
                            this._cm.SelectedCharacter.Name);
                }

                return this._cm.IsGlobalModerator || isLocalMod;
            }
        }

        /// <summary>
        ///     Gets the ignore command.
        /// </summary>
        public ICommand IgnoreCommand
        {
            get
            {
                if (this._ign == null)
                {
                    this._ign = new RelayCommand(this.AddIgnoreEvent, this.CanIgnore);
                }

                return this._ign;
            }
        }

        /// <summary>
        ///     Gets the interested command.
        /// </summary>
        public ICommand InterestedCommand
        {
            get
            {
                if (this._isInter == null)
                {
                    this._isInter = new RelayCommand(this.IsInterestedEvent);
                }

                return this._isInter;
            }
        }

        /// <summary>
        ///     Gets the invert button command.
        /// </summary>
        public ICommand InvertButtonCommand
        {
            get
            {
                if (this._invertButton == null)
                {
                    this._invertButton = new RelayCommand(this.InvertButton);
                }

                return this._invertButton;
            }
        }

        /// <summary>
        ///     Gets the join channel command.
        /// </summary>
        public ICommand JoinChannelCommand
        {
            get
            {
                if (this._join == null)
                {
                    this._join = new RelayCommand(this.RequestChannelJoinEvent, this.CanJoinChannel);
                }

                return this._join;
            }
        }

        /// <summary>
        ///     Gets the kick command.
        /// </summary>
        public ICommand KickCommand
        {
            get
            {
                if (this._kick == null)
                {
                    this._kick = new RelayCommand(this.KickEvent, param => this.HasPermissions);
                }

                return this._kick;
            }
        }

        /// <summary>
        ///     Gets the navigate to.
        /// </summary>
        public ICommand NavigateTo
        {
            get
            {
                if (this._link == null)
                {
                    this._link = new RelayCommand(this.StartLinkInDefaultBrowser);
                }

                return this._link;
            }
        }

        /// <summary>
        ///     Gets the not interested command.
        /// </summary>
        public ICommand NotInterestedCommand
        {
            get
            {
                if (this._isNInter == null)
                {
                    this._isNInter = new RelayCommand(this.IsUninterestedEvent);
                }

                return this._isNInter;
            }
        }

        /// <summary>
        ///     Gets the open right click menu command.
        /// </summary>
        public ICommand OpenRightClickMenuCommand
        {
            get
            {
                if (this._openMenu == null)
                {
                    this._openMenu = new RelayCommand(
                        args =>
                            {
                                ICharacter newTarget = this.CM.FindCharacter(args as string);
                                this.updateRightClickMenu(newTarget);
                            });
                }

                return this._openMenu;
            }
        }

        /// <summary>
        ///     Gets the report command.
        /// </summary>
        public ICommand ReportCommand
        {
            get
            {
                if (this._report == null)
                {
                    this._report = new RelayCommand(this.FileReportEvent);
                }

                return this._report;
            }
        }

        /// <summary>
        ///     Gets the request pm command.
        /// </summary>
        public ICommand RequestPMCommand
        {
            get
            {
                if (this._priv == null)
                {
                    this._priv = new RelayCommand(this.RequestPMEvent, this.CanRequestPM);
                }

                return this._priv;
            }
        }

        /// <summary>
        ///     Gets the right click menu view model.
        /// </summary>
        public RightClickMenuViewModel RightClickMenuViewModel
        {
            get
            {
                return this._rcmvm;
            }
        }

        /// <summary>
        ///     Gets the unignore command.
        /// </summary>
        public ICommand UnignoreCommand
        {
            get
            {
                if (this._uign == null)
                {
                    this._uign = new RelayCommand(this.RemoveIgnoreEvent, this.CanUnIgnore);
                }

                return this._uign;
            }
        }

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
            return this._cm.FindCharacter(args as string).HasReport;
        }

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        ///     The initialize.
        /// </summary>
        public abstract void Initialize();

        #endregion

        #region Methods

        internal virtual void InvertButton(object arguments)
        {
        }

        internal void updateRightClickMenu(ICharacter NewTarget)
        {
            string name = NewTarget.Name;
            this._rcmvm.SetNewTarget(
                NewTarget, this.CanIgnore(name), this.CanUnIgnore(name), this.CanHandleReport(name));
            this._rcmvm.IsOpen = true;
            this._crvm.Target = name;
            this.OnPropertyChanged("RightClickMenuViewModel");
            this.OnPropertyChanged("CreateReportViewModel");
        }

        /// <summary>
        /// The add ignore event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void AddIgnoreEvent(object args)
        {
            this.IgnoreEvent(args);
        }

        /// <summary>
        /// The ban event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void BanEvent(object args)
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
        protected bool CanIgnore(object args)
        {
            return !this._cm.Ignored.Contains(args as string, StringComparer.OrdinalIgnoreCase);
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
        protected bool CanJoinChannel(object args)
        {
            return
                !this._cm.CurrentChannels.Any(
                    param => param.ID.Equals((string)args, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The can request pm.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        protected bool CanRequestPM(object args)
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
        protected bool CanUnIgnore(object args)
        {
            return !this.CanIgnore(args);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="IsManaged">
        /// The is managed.
        /// </param>
        protected virtual void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                this._cm.SelectedChannelChanged -= this.OnSelectedChannelChanged;
                this._events.GetEvent<NewUpdateEvent>().Unsubscribe(this.UpdateRightClickMenu);
                this._container = null;
                this._region = null;
                this._cm = null;
                this._events = null;
                this._rcmvm.Dispose();
                this._rcmvm = null;
            }
        }

        /// <summary>
        /// The file report event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void FileReportEvent(object args)
        {
            this._rcmvm.IsOpen = false;
            this.OnPropertyChanged("RightClickMenuViewModel");

            this._crvm.IsOpen = true;
            this.OnPropertyChanged("CreateReportViewModel");
        }

        /// <summary>
        /// The find log event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void FindLogEvent(object args)
        {
            var name = args as string;

            IDictionary<string, object> command =
                CommandDefinitions.CreateCommand("openlogfolder", null, name).toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// The handle report event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void HandleReportEvent(object args)
        {
            var name = args as string;

            IDictionary<string, object> command =
                CommandDefinitions.CreateCommand("handlereport", new List<string> { name }).toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(command);
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
        protected void IgnoreEvent(object args, bool remove = false)
        {
            var name = args as string;

            IDictionary<string, object> command =
                CommandDefinitions.CreateCommand(remove ? "unignore" : "ignore", new List<string> { name })
                                  .toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(command);
            this.updateRightClickMenu(this._rcmvm.Target); // updates the ignore/unignore text
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
        protected void InterestedEvent(object args, bool interestedIn = true)
        {
            if (interestedIn)
            {
                this._events.GetEvent<UserCommandEvent>()
                    .Publish(CommandDefinitions.CreateCommand("interesting", new[] { args as string }).toDictionary());
            }
            else
            {
                this._events.GetEvent<UserCommandEvent>()
                    .Publish(
                        CommandDefinitions.CreateCommand("notinteresting", new[] { args as string }).toDictionary());
            }
        }

        /// <summary>
        /// The is interested event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void IsInterestedEvent(object args)
        {
            this.InterestedEvent(args);
        }

        /// <summary>
        /// The is uninterested event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void IsUninterestedEvent(object args)
        {
            this.InterestedEvent(args, false);
        }

        /// <summary>
        /// The kick event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void KickEvent(object args)
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
        protected void KickOrBanEvent(object args, bool isBan)
        {
            var name = args as string;

            IDictionary<string, object> command =
                CommandDefinitions.CreateCommand(isBan ? "ban" : "kick", new[] { name }, this.CM.SelectedChannel.ID)
                                  .toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(command);
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
        protected virtual void OnSelectedChannelChanged(object sender, EventArgs e)
        {
            this.OnPropertyChanged("HasPermissions");
            this._rcmvm.IsOpen = false;
            this._crvm.IsOpen = false;
        }

        /// <summary>
        /// The remove ignore event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void RemoveIgnoreEvent(object args)
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
            IDictionary<string, object> command =
                CommandDefinitions.CreateCommand("join", new List<string> { args as string }).toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// The request pm event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void RequestPMEvent(object args)
        {
            var TabName = (string)args;
            if (this._cm.CurrentPMs.Any(param => param.ID.Equals(TabName, StringComparison.OrdinalIgnoreCase)))
            {
                this._events.GetEvent<RequestChangeTabEvent>().Publish(TabName);
                return;
            }

            IDictionary<string, object> command =
                CommandDefinitions.CreateCommand("priv", new List<string> { TabName }).toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// The start link in default browser.
        /// </summary>
        /// <param name="link">
        /// The link.
        /// </param>
        protected void StartLinkInDefaultBrowser(object link)
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
            if (!this._rcmvm.IsOpen)
            {
                return;
            }

            var updateKind = argument as CharacterUpdateModel;
            if (updateKind == null)
            {
                return;
            }

            if (this._rcmvm.Target == null)
            {
                return;
            }

            if (updateKind.TargetCharacter.Name == this._rcmvm.Target.Name)
            {
                this.updateRightClickMenu(this._rcmvm.Target);
            }

            this.OnPropertyChanged("RightClickMenuViewModel");
        }

        #endregion
    }
}