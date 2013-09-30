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

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat;
    using Slimcat.Libraries;
    using Slimcat.Models;
    using Slimcat.Services;
    using Slimcat.Utilities;
    using Slimcat.Views;

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
        internal const string UserbarView = "UserbarView";

        #endregion

        #region Fields

        private readonly IDictionary<string, StatusType> statusKinds = new Dictionary<string, StatusType>
        {
            {
                "Online",  
                StatusType
                .Online
            }, 
            {
                "Busy", 
                StatusType
                .Busy
            }, 
            {
                "Do not Disturb",  
                StatusType
                .Dnd
            }, 
            {
                "Looking For Play",  
                StatusType
                .Looking
            }, 
            {
                "Away", 
                StatusType
                .Away
            }
        };

        private readonly Timer updateTick = new Timer(2500);

        private bool channelsExpanded = true;

        private RelayCommand close;

        private bool hasNewChanMessage;

        private bool hasNewPm;

        private bool isChangingStatus;

        private bool isExpanded = true;

        private bool pmsExpanded = true;

        private RelayCommand saveChannels;

        private int selChannelIndex;

        private int selPmIndex = -1;

        private string statusCache = string.Empty;

        private StatusType statusTypeCache;

        private RelayCommand toggle;

        private RelayCommand toggleStatus;

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
                this.ChatModel.CurrentPMs.CollectionChanged += (s, e) => this.OnPropertyChanged("HasPMs");

                // this checks if we need to hide/show the PM tab
                this.Events.GetEvent<ChatOnDisplayEvent>().Subscribe(this.RequestNavigate, ThreadOption.UIThread, true);

                this.Events.GetEvent<NewPMEvent>()
                    .Subscribe(param => this.UpdateFlashingTabs(), ThreadOption.UIThread, true);

                this.Events.GetEvent<NewMessageEvent>()
                    .Subscribe(param => this.UpdateFlashingTabs(), ThreadOption.UIThread, true);

                this.ChatModel.SelectedChannelChanged += (s, e) => this.UpdateFlashingTabs();

                this.updateTick.Enabled = true;
                this.updateTick.Elapsed += this.UpdateConnectionBars;
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
        public int ChannelSelected
        {
            get
            {
                return this.selChannelIndex;
            }

            set
            {
                if (this.selChannelIndex == value)
                {
                    return;
                }

                this.selChannelIndex = value;
                this.OnPropertyChanged("ChannelSelected");

                if (value != -1 && this.ChatModel.CurrentChannel != this.ChatModel.CurrentChannels[value])
                {
                    this.Events.GetEvent<RequestChangeTabEvent>().Publish(this.ChatModel.CurrentChannels[value].Id);
                }

                if (this.ChannelSelected != -1)
                {
                    this.PmSelected = -1;
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
                return this.channelsExpanded;
            }

            set
            {
                this.channelsExpanded = value;
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
                return this.close ?? (this.close = new RelayCommand(this.TabCloseEvent));
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
                return this.hasNewChanMessage;
            }

            set
            {
                this.hasNewChanMessage = value;
                this.OnPropertyChanged("HasNewMessage");
                this.OnPropertyChanged("HasUpdate");
                this.OnPropertyChanged("ExpandString");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether has new PrivateMessage.
        /// </summary>
        public bool HasNewPM
        {
            get
            {
                return this.hasNewPm;
            }

            set
            {
                this.hasNewPm = value;
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
                return this.ChatModel.CurrentPMs.Count > 0;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has update.
        /// </summary>
        public bool HasUpdate
        {
            get
            {
                return this.hasNewChanMessage || this.hasNewPm;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is changing status.
        /// </summary>
        public bool IsChangingStatus
        {
            get
            {
                return this.isChangingStatus;
            }

            set
            {
                if (this.isChangingStatus == value)
                {
                    return;
                }

                this.isChangingStatus = value;
                this.OnPropertyChanged("IsChangingStatus");

                if (value
                    || (this.statusCache == this.ChatModel.CurrentCharacter.StatusMessage
                        && this.statusTypeCache == this.ChatModel.CurrentCharacter.Status))
                {
                    return;
                }

                this.SendStatusChangedCommand();
                this.statusCache = this.ChatModel.CurrentCharacter.StatusMessage;
                this.statusTypeCache = this.ChatModel.CurrentCharacter.Status;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return this.isExpanded;
            }

            set
            {
                this.isExpanded = value;
                this.OnPropertyChanged("IsExpanded");
                this.OnPropertyChanged("ExpandString");
            }
        }

        // this links the PM and Channel boxes so they act as one

        /// <summary>
        ///     Gets or sets the p m_ selected.
        /// </summary>
        public int PmSelected
        {
            get
            {
                return this.selPmIndex;
            }

            set
            {
                if (this.selPmIndex == value)
                {
                    return;
                }

                this.selPmIndex = value;
                this.OnPropertyChanged("PmSelected");

                if (value != -1 && this.ChatModel.CurrentChannel != this.ChatModel.CurrentPMs[value])
                {
                    this.Events.GetEvent<RequestChangeTabEvent>().Publish(this.ChatModel.CurrentPMs[value].Id);
                }

                if (this.PmSelected != -1)
                {
                    this.ChannelSelected = -1;
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
                return this.pmsExpanded;
            }

            set
            {
                this.pmsExpanded = value;
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
                return this.saveChannels ?? (this.saveChannels = new RelayCommand(
                args =>
                    {
                        ApplicationSettings.SavedChannels.Clear();

                        foreach (
                            var channel in
                                this.ChatModel.CurrentChannels.Where(
                                    channel => !channel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase)))
                        {
                            ApplicationSettings.SavedChannels.Add(channel.Id);
                        }

                        SettingsDaemon.SaveApplicationSettingsToXml(this.ChatModel.CurrentCharacter.Name);
                        this.Events.GetEvent<ErrorEvent>()
                            .Publish("Channels saved.");
                    }));
            }
        }

        /// <summary>
        ///     Gets the status types.
        /// </summary>
        public IDictionary<string, StatusType> StatusTypes
        {
            get
            {
                return this.statusKinds;
            }
        }

        /// <summary>
        ///     Gets the toggle bar command.
        /// </summary>
        public ICommand ToggleBarCommand
        {
            get
            {
                return this.toggle ?? (this.toggle = new RelayCommand(this.OnExpanded));
            }
        }

        /// <summary>
        ///     Gets the toggle status window command.
        /// </summary>
        public ICommand ToggleStatusWindowCommand
        {
            get
            {
                return this.toggleStatus
                       ?? (this.toggleStatus = new RelayCommand(args => this.IsChangingStatus = !this.IsChangingStatus));
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
                this.Container.RegisterType<object, UserbarView>(UserbarView);
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
            var toSend =
                CommandDefinitions.CreateCommand("close", null, args as string).ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        private void OnExpanded(object args = null)
        {
            this.PMsAreExpanded = this.PMsAreExpanded || this.HasNewPM;
            this.ChannelsAreExpanded = this.ChannelsAreExpanded || this.HasNewMessage;
            this.IsExpanded = !this.IsExpanded;
        }

        private void RequestNavigate(bool? payload)
        {
            this.Events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(this.RequestNavigate);
            this.RegionManager.Regions[ChatWrapperView.UserbarRegion].Add(this.Container.Resolve<UserbarView>());
        }

        private void SendStatusChangedCommand()
        {
            var torSend =
                CommandDefinitions.CreateCommand(
                    "status", 
                    new List<string>
                        {
                            this.ChatModel.CurrentCharacter.Status.ToString(), 
                            this.ChatModel.CurrentCharacter.StatusMessage
                        }).ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(torSend);
        }

        // this will update the connection bars to show the user about how good our connection is to the server
        private void UpdateConnectionBars(object sender, EventArgs e)
        {
            var difference = DateTime.Now - this.ChatModel.LastMessageReceived;
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

        private void UpdateFlashingTabs()
        {
            if (this.ChatModel.CurrentChannel is PMChannelModel)
            {
                this.PmSelected = this.ChatModel.CurrentPMs.IndexOf(this.ChatModel.CurrentChannel as PMChannelModel);
            }
            else
            {
                this.ChannelSelected = this.ChatModel.CurrentChannels.IndexOf(this.ChatModel.CurrentChannel as GeneralChannelModel);
            }

            this.hasNewPm = this.ChatModel.CurrentPMs.Cast<ChannelModel>().Any(cm => cm.NeedsAttention);
            this.HasNewMessage = this.ChatModel.CurrentChannels.Cast<ChannelModel>().Any(cm => cm.NeedsAttention);
        }

        #endregion
    }
}