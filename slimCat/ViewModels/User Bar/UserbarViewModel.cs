#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserbarViewModel.cs">
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
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     The UserbarViewCM allows the user to navigate the current conversations they have open.
    ///     It responds to the ChatOnDisplayEvent to partially create the chat wrapper.
    /// </summary>
    public class UserbarViewModel : ViewModelBase
    {
        #region Constants

        internal const string UserbarView = "UserbarView";

        #endregion

        #region Fields

        private readonly IDictionary<string, StatusType> statusKinds = new Dictionary<string, StatusType>
        {
            {
                "Online", StatusType.Online
            },
            {
                "Busy", StatusType.Busy
            },
            {
                "Do not Disturb", StatusType.Dnd
            },
            {
                "Looking For Play", StatusType.Looking
            },
            {
                "Away", StatusType.Away
            }
        };

        private readonly Timer updateTick = new Timer(2500);

        private bool autoReplyEnabled;

        private bool channelsExpanded = true;

        private RelayCommand close;

        private bool hasNewChanMessage;

        private bool hasNewPm;

        private bool isChangingAuto;

        private bool isChangingStatus;

        private bool isExpanded = true;

        private string newAutoReplyString = string.Empty;

        private string newStatusString = string.Empty;

        private StatusType newStatusType = StatusType.Online;

        private bool pmsExpanded = true;

        private RelayCommand saveChannels;

        private int selChannelIndex;

        private int selPmIndex = -1;

        private RelayCommand toggle;

        private ICommand toggleAuto;

        private RelayCommand toggleStatus;

        #endregion

        #region Constructors and Destructors

        public UserbarViewModel(IChatState chatState)
            : base(chatState)
        {
            try
            {
                ChatModel.CurrentPms.CollectionChanged += (s, e) => OnPropertyChanged("HasPms");

                // this checks if we need to hide/show the Pm tab
                Events.GetEvent<ChatOnDisplayEvent>().Subscribe(RequestNavigate, ThreadOption.UIThread, true);

                Events.GetEvent<NewPmEvent>()
                    .Subscribe(param => UpdateFlashingTabs(), ThreadOption.UIThread, true);

                Events.GetEvent<NewMessageEvent>()
                    .Subscribe(param => UpdateFlashingTabs(), ThreadOption.UIThread, true);

                Events.GetEvent<ConnectionClosedEvent>()
                    .Subscribe(param => UpdateFlashingTabs(), ThreadOption.UIThread, true);

                ChatModel.SelectedChannelChanged += (s, e) => UpdateFlashingTabs();

                updateTick.Enabled = true;
                updateTick.Elapsed += UpdateConnectionBars;

                LoggingSection = "userbar vm";
                IsDisconnected = true;
                OnPropertyChanged("IsDisconnected");
            }
            catch (Exception ex)
            {
                ex.Source = "Userbar vm, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        public int ChannelSelected
        {
            get { return selChannelIndex; }

            set
            {
                if (selChannelIndex == value)
                    return;

                selChannelIndex = value;
                OnPropertyChanged("ChannelSelected");

                if (value != -1 && ChatModel.CurrentChannel != ChatModel.CurrentChannels[value])
                    Events.GetEvent<RequestChangeTabEvent>().Publish(ChatModel.CurrentChannels[value].Id);

                if (ChannelSelected != -1)
                    PmSelected = -1;
            }
        }

        public bool ChannelsAreExpanded
        {
            get { return channelsExpanded; }

            set
            {
                channelsExpanded = value;
                OnPropertyChanged("ChannelsAreExpanded");
            }
        }

        public ICommand CloseCommand
        {
            get { return close ?? (close = new RelayCommand(TabCloseEvent)); }
        }

        public bool ConnectionIsConnected { get; set; }

        public bool ConnectionIsGood { get; set; }

        public bool ConnectionIsModerate { get; set; }

        public bool ConnectionIsPerfect { get; set; }

        public bool IsDisconnected { get; set; }

        public string ExpandString
        {
            get
            {
                if (HasUpdate && !IsExpanded)
                    return "!";

                return IsExpanded ? "<" : ">";
            }
        }

        public string CloseOrSave
        {
            get { return HasChanges ? "Save" : "Close"; }
        }

        public bool HasChanges { get; set; }

        public string NewStatusString
        {
            get { return newStatusString; }
            set
            {
                newStatusString = value;
                OnPropertyChanged("NewStatusString");

                HasChanges = newStatusString != ChatModel.CurrentCharacter.StatusMessage;

                if (!HasChanges)
                    HasChanges = newStatusType != ChatModel.CurrentCharacter.Status;

                OnPropertyChanged("CloseOrSave");
            }
        }

        public StatusType NewStatusType
        {
            get { return newStatusType; }
            set
            {
                newStatusType = value;
                OnPropertyChanged("NewStatusType");

                HasChanges = newStatusType != ChatModel.CurrentCharacter.Status;

                if (!HasChanges)
                    HasChanges = newStatusString != ChatModel.CurrentCharacter.StatusMessage;

                OnPropertyChanged("CloseOrSave");
            }
        }

        public string NewAutoReplyString
        {
            get { return newAutoReplyString; }
            set
            {
                newAutoReplyString = value;
                OnPropertyChanged("NewAutoReplyString");

                HasChanges = newAutoReplyString != ChatModel.AutoReplyMessage;
                OnPropertyChanged("CloseOrSave");
            }
        }

        public bool HasNewMessage
        {
            get { return hasNewChanMessage; }

            set
            {
                if (hasNewChanMessage == value) return;

                if (value) Log("Displaying new channel message");
                hasNewChanMessage = value;
                OnPropertyChanged("HasNewMessage");
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString");
            }
        }

        public bool HasNewPm
        {
            get { return hasNewPm; }

            set
            {
                if (hasNewPm == value) return;

                if (value) Log("Displaying new pm");
                hasNewPm = value;
                OnPropertyChanged("HasNewPm");
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString");
            }
        }

        public bool HasPms
        {
            get { return ChatModel.CurrentPms.Count > 0; }
        }

        public bool HasUpdate
        {
            get { return hasNewChanMessage || hasNewPm; }
        }

        public bool IsChangingStatus
        {
            get { return isChangingStatus; }

            set
            {
                if (isChangingStatus == value)
                    return;

                isChangingStatus = value;
                OnPropertyChanged("IsChangingStatus");

                if (value
                    || (newStatusString == ChatModel.CurrentCharacter.StatusMessage
                        && newStatusType == ChatModel.CurrentCharacter.Status))
                    return;

                HasChanges = false;
                ChatModel.CurrentCharacter.StatusMessage = newStatusString;
                ChatModel.CurrentCharacter.Status = newStatusType;

                SendStatusChangedCommand();
                OnPropertyChanged("CloseOrSave");
            }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }

            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
                OnPropertyChanged("ExpandString");
            }
        }

        public int PmSelected
        {
            get { return selPmIndex; }

            set
            {
                if (selPmIndex == value)
                    return;

                selPmIndex = value;
                OnPropertyChanged("PmSelected");

                if (value != -1 && ChatModel.CurrentChannel != ChatModel.CurrentPms[value])
                    Events.GetEvent<RequestChangeTabEvent>().Publish(ChatModel.CurrentPms[value].Id);

                if (PmSelected != -1)
                    ChannelSelected = -1;
            }
        }

        public bool PmsAreExpanded
        {
            get { return pmsExpanded; }

            set
            {
                pmsExpanded = value;
                OnPropertyChanged("PmsAreExpanded");
            }
        }

        public ICommand SaveChannelsCommand
        {
            get
            {
                return saveChannels ?? (saveChannels = new RelayCommand(
                    args =>
                    {
                        Log("Saving channels");
                        ApplicationSettings.SavedChannels.Clear();

                        foreach (
                            var channel in
                                ChatModel.CurrentChannels.Where(
                                    channel => !channel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase)))
                            ApplicationSettings.SavedChannels.Add(channel.Id);

                        SettingsService.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                        Events.GetEvent<ErrorEvent>()
                            .Publish("Channels saved.");
                    }));
            }
        }

        public IDictionary<string, StatusType> StatusTypes
        {
            get { return statusKinds; }
        }

        public ICommand ToggleBarCommand
        {
            get { return toggle ?? (toggle = new RelayCommand(OnExpanded)); }
        }

        public ICommand ToggleStatusWindowCommand
        {
            get
            {
                return toggleStatus
                       ?? (toggleStatus = new RelayCommand(args => IsChangingStatus = !IsChangingStatus));
            }
        }

        public ICommand ToggleAutoReplyWindowCommand
        {
            get { return toggleAuto ?? (toggleAuto = new RelayCommand(ToggleAutoReplyEvent)); }
        }


        public bool IsChangingAuto
        {
            get { return isChangingAuto; }
            set
            {
                isChangingAuto = value;
                OnPropertyChanged("IsChangingAuto");

                if (value) return;

                HasChanges = false;
                OnPropertyChanged("CloseOrSave");

                ChatModel.AutoReplyMessage = newAutoReplyString;
            }
        }

        public bool AutoReplyEnabled
        {
            get { return autoReplyEnabled; }
            set
            {
                autoReplyEnabled = value;
                ChatModel.AutoReplyEnabled = value;
                OnPropertyChanged("AutoReplyEnabled");
            }
        }

        private void ToggleAutoReplyEvent(object o)
        {
            IsChangingAuto = !IsChangingAuto;
            if (isChangingAuto) AutoReplyEnabled = !AutoReplyEnabled;
        }

        #endregion

        #region Public Methods and Operators

        public override void Initialize()
        {
            try
            {
                Container.RegisterType<object, UserbarView>(UserbarView);
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
            Events.SendUserCommand("close", null, args as string);
        }

        private void OnExpanded(object args = null)
        {
            PmsAreExpanded = PmsAreExpanded || HasNewPm;
            ChannelsAreExpanded = ChannelsAreExpanded || HasNewMessage;
            IsExpanded = !IsExpanded;
            Log(IsExpanded ? "Expanding" : "Hiding");
        }

        private void RequestNavigate(bool? payload)
        {
            var region = RegionManager.Regions[ChatWrapperView.UserbarRegion];

            if (!region.Views.Any())
                region.Add(Container.Resolve<UserbarView>());
            Log("Requesting userbar view");
        }

        private void SendStatusChangedCommand()
        {
            var character = ChatModel.CurrentCharacter;
            Events.SendUserCommand("status", new[] {character.Status.ToString(), character.StatusMessage});
        }

        // this will update the connection bars to show the user about how good our connection is to the server
        private void UpdateConnectionBars(object sender, EventArgs e)
        {
            var difference = DateTime.Now - ChatModel.LastMessageReceived;
            ConnectionIsPerfect = ChatModel.IsAuthenticated;
            ConnectionIsGood = ChatModel.IsAuthenticated;
            ConnectionIsModerate = ChatModel.IsAuthenticated;
            ConnectionIsConnected = ChatModel.IsAuthenticated;
            IsDisconnected = !ChatModel.IsAuthenticated;

            if (difference.TotalSeconds > 5)
                ConnectionIsPerfect = false;

            if (difference.TotalSeconds > 10)
                ConnectionIsGood = false;

            if (difference.TotalSeconds > 20)
                ConnectionIsModerate = false;

            if (difference.TotalSeconds > 40)
                ConnectionIsConnected = false;

            OnPropertyChanged("ConnectionIsPerfect");
            OnPropertyChanged("ConnectionIsGood");
            OnPropertyChanged("ConnectionIsModerate");
            OnPropertyChanged("ConnectionIsConnected");
            OnPropertyChanged("IsDisconnected");
        }

        private void UpdateFlashingTabs()
        {
            if (ChatModel.CurrentChannel is PmChannelModel)
                PmSelected = ChatModel.CurrentPms.IndexOf(ChatModel.CurrentChannel as PmChannelModel);
            else
                ChannelSelected = ChatModel.CurrentChannels.IndexOf(ChatModel.CurrentChannel as GeneralChannelModel);

            HasNewPm = ChatModel.CurrentPms.Cast<ChannelModel>().Any(cm => cm.NeedsAttention);
            HasNewMessage = ChatModel.CurrentChannels.Cast<ChannelModel>().Any(cm => cm.NeedsAttention);
            Events.GetEvent<UnreadUpdatesEvent>().Publish(HasUpdate);
        }

        #endregion
    }
}