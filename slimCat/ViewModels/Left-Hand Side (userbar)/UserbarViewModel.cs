// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserbarViewModel.cs" company="Justin Kadrovach">
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
//   The UserbarViewCM allows the user to navigate the current conversations they have open.
//   It responds to the ChatOnDisplayEvent to paritally create the chat wrapper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Timers;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using Services;

    using slimCat;

    using Views;

    /// <summary>
    ///     The UserbarViewCM allows the user to navigate the current conversations they have open.
    ///     It responds to the ChatOnDisplayEvent to paritally create the chat wrapper.
    /// </summary>
    public class UserbarViewModel : ViewModelBase
    {
        #region Constants

        /// <summary>
        ///     The userbar view.
        /// </summary>
        public const string UserbarView = "UserbarView";

        #endregion

        #region Fields

        private readonly IDictionary<string, StatusType> _statuskinds = new Dictionary<string, StatusType>
                                                                            {
                                                                                {
                                                                                    "Online"
                                                                                    , 
                                                                                    StatusType
                                                                                    .online
                                                                                }, 
                                                                                {
                                                                                    "Busy", 
                                                                                    StatusType
                                                                                    .busy
                                                                                }, 
                                                                                {
                                                                                    "Do not Disturb"
                                                                                    , 
                                                                                    StatusType
                                                                                    .dnd
                                                                                }, 
                                                                                {
                                                                                    "Looking For Play"
                                                                                    , 
                                                                                    StatusType
                                                                                    .looking
                                                                                }, 
                                                                                {
                                                                                    "Away", 
                                                                                    StatusType
                                                                                    .away
                                                                                }
                                                                            };

        private readonly Timer _updateTick = new Timer(2500);

        private bool _channelsExpanded = true;

        private RelayCommand _close;

        private bool _hasNewChanMessage;

        private bool _hasNewPM;

        private bool _isChangingStatus;

        private bool _isExpanded = true;

        private bool _pmsExpanded = true;

        private RelayCommand _saveChannels;

        private int _selChanIndex;

        private int _selPMIndex = -1;

        private string _status_cache = string.Empty;

        private StatusType _status_type_cache;

        private RelayCommand _toggle;

        private RelayCommand _toggleStatus;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UserbarViewModel"/> class.
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
        public UserbarViewModel(IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                this.CM.CurrentPMs.CollectionChanged += (s, e) => this.OnPropertyChanged("HasPMs");

                // this checks if we need to hide/show the PM tab
                this._events.GetEvent<ChatOnDisplayEvent>().Subscribe(this.requestNavigate, ThreadOption.UIThread, true);

                this._events.GetEvent<NewPMEvent>()
                    .Subscribe(param => { this.updateFlashingTabs(); }, ThreadOption.UIThread, true);

                this._events.GetEvent<NewMessageEvent>()
                    .Subscribe(param => { this.updateFlashingTabs(); }, ThreadOption.UIThread, true);

                this.CM.SelectedChannelChanged += (s, e) => this.updateFlashingTabs();

                this._updateTick.Enabled = true;
                this._updateTick.Elapsed += this.updateConnectionBars;
            }
            catch (Exception ex)
            {
                ex.Source = "Userbar ViewCM, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the chan_ selected.
        /// </summary>
        public int Chan_Selected
        {
            get
            {
                return this._selChanIndex;
            }

            set
            {
                if (this._selChanIndex != value)
                {
                    this._selChanIndex = value;
                    this.OnPropertyChanged("Chan_Selected");

                    if (value != -1 && this.CM.SelectedChannel != this.CM.CurrentChannels[value])
                    {
                        this._events.GetEvent<RequestChangeTabEvent>().Publish(this.CM.CurrentChannels[value].ID);
                    }

                    if (this.Chan_Selected != -1)
                    {
                        if (this.PM_Selected != -1)
                        {
                            this.PM_Selected = -1;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether channels are expanded.
        /// </summary>
        public bool ChannelsAreExpanded
        {
            get
            {
                return this._channelsExpanded;
            }

            set
            {
                this._channelsExpanded = value;
                this.OnPropertyChanged("ChannelsAreExpanded");
            }
        }

        /// <summary>
        ///     Gets the close command.
        /// </summary>
        public ICommand CloseCommand
        {
            get
            {
                if (this._close == null)
                {
                    this._close = new RelayCommand(this.TabCloseEvent);
                }

                return this._close;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether connection is connected.
        /// </summary>
        public bool ConnectionIsConnected { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether connection is good.
        /// </summary>
        public bool ConnectionIsGood { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether connection is moderate.
        /// </summary>
        public bool ConnectionIsModerate { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether connection is perfect.
        /// </summary>
        public bool ConnectionIsPerfect { get; set; }

        /// <summary>
        ///     Gets the expand string.
        /// </summary>
        public string ExpandString
        {
            get
            {
                if (this.HasUpdate && !this.IsExpanded)
                {
                    return "!";
                }

                return this.IsExpanded ? "<" : ">";
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether has new message.
        /// </summary>
        public bool HasNewMessage
        {
            get
            {
                return this._hasNewChanMessage;
            }

            set
            {
                this._hasNewChanMessage = value;
                this.OnPropertyChanged("HasNewMessage");
                this.OnPropertyChanged("HasUpdate");
                this.OnPropertyChanged("ExpandString");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether has new pm.
        /// </summary>
        public bool HasNewPM
        {
            get
            {
                return this._hasNewPM;
            }

            set
            {
                this._hasNewPM = value;
                this.OnPropertyChanged("HasNewPM");
                this.OnPropertyChanged("HasUpdate");
                this.OnPropertyChanged("ExpandString");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has p ms.
        /// </summary>
        public bool HasPMs
        {
            get
            {
                return this.CM.CurrentPMs.Count > 0;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has update.
        /// </summary>
        public bool HasUpdate
        {
            get
            {
                return this._hasNewChanMessage || this._hasNewPM;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is changing status.
        /// </summary>
        public bool IsChangingStatus
        {
            get
            {
                return this._isChangingStatus;
            }

            set
            {
                if (this._isChangingStatus != value)
                {
                    this._isChangingStatus = value;
                    this.OnPropertyChanged("IsChangingStatus");

                    if (value == false
                        && (this._status_cache != this.CM.SelectedCharacter.StatusMessage
                            || this._status_type_cache != this.CM.SelectedCharacter.Status))
                    {
                        this.sendStatusChangedCommand();
                        this._status_cache = this.CM.SelectedCharacter.StatusMessage;
                        this._status_type_cache = this.CM.SelectedCharacter.Status;
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return this._isExpanded;
            }

            set
            {
                this._isExpanded = value;
                this.OnPropertyChanged("IsExpanded");
                this.OnPropertyChanged("ExpandString");
            }
        }

        // this links the PM and Channel boxes so they act as one

        /// <summary>
        ///     Gets or sets the p m_ selected.
        /// </summary>
        public int PM_Selected
        {
            get
            {
                return this._selPMIndex;
            }

            set
            {
                if (this._selPMIndex != value)
                {
                    this._selPMIndex = value;
                    this.OnPropertyChanged("PM_Selected");

                    if (value != -1 && this.CM.SelectedChannel != this.CM.CurrentPMs[value])
                    {
                        this._events.GetEvent<RequestChangeTabEvent>().Publish(this.CM.CurrentPMs[value].ID);
                    }

                    if (this.PM_Selected != -1)
                    {
                        if (this.Chan_Selected != -1)
                        {
                            this.Chan_Selected = -1;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether p ms are expanded.
        /// </summary>
        public bool PMsAreExpanded
        {
            get
            {
                return this._pmsExpanded;
            }

            set
            {
                this._pmsExpanded = value;
                this.OnPropertyChanged("PMsAreExpanded");
            }
        }

        /// <summary>
        ///     Gets the save channels command.
        /// </summary>
        public ICommand SaveChannelsCommand
        {
            get
            {
                if (this._saveChannels == null)
                {
                    this._saveChannels = new RelayCommand(
                        args =>
                            {
                                ApplicationSettings.SavedChannels.Clear();

                                foreach (GeneralChannelModel channel in this.CM.CurrentChannels)
                                {
                                    if (!channel.ID.Equals("Home", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ApplicationSettings.SavedChannels.Add(channel.ID);
                                    }
                                }

                                SettingsDaemon.SaveApplicationSettingsToXML(this.CM.SelectedCharacter.Name);
                                this._events.GetEvent<ErrorEvent>().Publish("Channels saved.");
                            });
                }

                return this._saveChannels;
            }
        }

        /// <summary>
        ///     Gets the status types.
        /// </summary>
        public IDictionary<string, StatusType> StatusTypes
        {
            get
            {
                return this._statuskinds;
            }
        }

        /// <summary>
        ///     Gets the toggle bar command.
        /// </summary>
        public ICommand ToggleBarCommand
        {
            get
            {
                if (this._toggle == null)
                {
                    this._toggle = new RelayCommand(this.onExpanded);
                }

                return this._toggle;
            }
        }

        /// <summary>
        ///     Gets the toggle status window command.
        /// </summary>
        public ICommand ToggleStatusWindowCommand
        {
            get
            {
                if (this._toggleStatus == null)
                {
                    this._toggleStatus = new RelayCommand(args => this.IsChangingStatus = !this.IsChangingStatus);
                }

                return this._toggleStatus;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
            try
            {
                this._container.RegisterType<object, UserbarView>(UserbarView);
            }
            catch (Exception ex)
            {
                ex.Source = "Userbar ViewCM, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        private void TabCloseEvent(object args)
        {
            IDictionary<string, object> toSend =
                CommandDefinitions.CreateCommand("close", null, args as string).toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        private void onExpanded(object args = null)
        {
            this.PMsAreExpanded = this.PMsAreExpanded || this.HasNewPM;
            this.ChannelsAreExpanded = this.ChannelsAreExpanded || this.HasNewMessage;
            this.IsExpanded = !this.IsExpanded;
        }

        private void requestNavigate(bool? payload)
        {
            this._events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(this.requestNavigate);
            this._region.Regions[ChatWrapperView.UserbarRegion].Add(this._container.Resolve<UserbarView>());
        }

        private void sendStatusChangedCommand()
        {
            IDictionary<string, object> torSend =
                CommandDefinitions.CreateCommand(
                    "status", 
                    new List<string>
                        {
                            this.CM.SelectedCharacter.Status.ToString(), 
                            this.CM.SelectedCharacter.StatusMessage
                        }).toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(torSend);
        }

        // this will update the connection bars to show the user about how good our connection is to the server
        private void updateConnectionBars(object sender, EventArgs e)
        {
            TimeSpan difference = DateTime.Now - this.CM.LastMessageReceived;
            this.ConnectionIsPerfect = true;
            this.ConnectionIsGood = true;
            this.ConnectionIsModerate = true;
            this.ConnectionIsConnected = true;

            if (difference.TotalSeconds > 5)
            {
                this.ConnectionIsPerfect = false;
            }

            if (difference.TotalSeconds > 10)
            {
                this.ConnectionIsGood = false;
            }

            if (difference.TotalSeconds > 15)
            {
                this.ConnectionIsModerate = false;
            }

            if (difference.TotalSeconds > 30)
            {
                this.ConnectionIsConnected = false;
            }

            this.OnPropertyChanged("ConnectionIsPerfect");
            this.OnPropertyChanged("ConnectionIsGood");
            this.OnPropertyChanged("ConnectionIsModerate");
            this.OnPropertyChanged("ConnectionIsConnected");
        }

        private void updateFlashingTabs()
        {
            if (this.CM.SelectedChannel is PMChannelModel)
            {
                this.PM_Selected = this.CM.CurrentPMs.IndexOf(this.CM.SelectedChannel as PMChannelModel);
            }
            else
            {
                this.Chan_Selected = this.CM.CurrentChannels.IndexOf(this.CM.SelectedChannel as GeneralChannelModel);
            }

            bool stillHasPMs = false;
            foreach (ChannelModel cm in this.CM.CurrentPMs)
            {
                if (cm.NeedsAttention)
                {
                    stillHasPMs = true;
                    break;
                }
            }

            this.HasNewPM = stillHasPMs;

            bool stillHasMessages = false;
            foreach (ChannelModel cm in this.CM.CurrentChannels)
            {
                if (cm.NeedsAttention)
                {
                    stillHasMessages = true;
                    break;
                }
            }

            this.HasNewMessage = stillHasMessages;
        }

        #endregion
    }
}