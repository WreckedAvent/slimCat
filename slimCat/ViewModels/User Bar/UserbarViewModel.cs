#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserbarViewModel.cs">
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
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

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
        ///     Initializes a new instance of the <see cref="UserbarViewModel" /> class.
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
        public UserbarViewModel(IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager manager)
            : base(contain, regman, events, cm, manager)
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

                ChatModel.SelectedChannelChanged += (s, e) => UpdateFlashingTabs();

                updateTick.Enabled = true;
                updateTick.Elapsed += UpdateConnectionBars;
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

        /// <summary>
        ///     Gets or sets a value indicating whether channels are expanded.
        /// </summary>
        public bool ChannelsAreExpanded
        {
            get { return channelsExpanded; }

            set
            {
                channelsExpanded = value;
                OnPropertyChanged("ChannelsAreExpanded");
            }
        }

        /// <summary>
        ///     Gets the close command.
        /// </summary>
        public ICommand CloseCommand
        {
            get { return close ?? (close = new RelayCommand(TabCloseEvent)); }
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
                if (HasUpdate && !IsExpanded)
                    return "!";

                return IsExpanded ? "<" : ">";
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether has new message.
        /// </summary>
        public bool HasNewMessage
        {
            get { return hasNewChanMessage; }

            set
            {
                hasNewChanMessage = value;
                OnPropertyChanged("HasNewMessage");
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether has new PrivateMessage.
        /// </summary>
        public bool HasNewPm
        {
            get { return hasNewPm; }

            set
            {
                hasNewPm = value;
                OnPropertyChanged("HasNewPm");
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has p ms.
        /// </summary>
        public bool HasPms
        {
            get { return ChatModel.CurrentPms.Count > 0; }
        }

        /// <summary>
        ///     Gets a value indicating whether has update.
        /// </summary>
        public bool HasUpdate
        {
            get { return hasNewChanMessage || hasNewPm; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is changing status.
        /// </summary>
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
                    || (statusCache == ChatModel.CurrentCharacter.StatusMessage
                        && statusTypeCache == ChatModel.CurrentCharacter.Status))
                    return;

                SendStatusChangedCommand();
                statusCache = ChatModel.CurrentCharacter.StatusMessage;
                statusTypeCache = ChatModel.CurrentCharacter.Status;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is expanded.
        /// </summary>
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

        // this links the Pm and Channel boxes so they act as one

        /// <summary>
        ///     Gets or sets the p m_ selected.
        /// </summary>
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

        /// <summary>
        ///     Gets or sets a value indicating whether p ms are expanded.
        /// </summary>
        public bool PmsAreExpanded
        {
            get { return pmsExpanded; }

            set
            {
                pmsExpanded = value;
                OnPropertyChanged("PmsAreExpanded");
            }
        }

        /// <summary>
        ///     Gets the save channels command.
        /// </summary>
        public ICommand SaveChannelsCommand
        {
            get
            {
                return saveChannels ?? (saveChannels = new RelayCommand(
                    args =>
                        {
                            ApplicationSettings.SavedChannels.Clear();

                            foreach (
                                var channel in
                                    ChatModel.CurrentChannels.Where(
                                        channel => !channel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase)))
                                ApplicationSettings.SavedChannels.Add(channel.Id);

                            SettingsDaemon.SaveApplicationSettingsToXml(ChatModel.CurrentCharacter.Name);
                            Events.GetEvent<ErrorEvent>()
                                .Publish("Channels saved.");
                        }));
            }
        }

        /// <summary>
        ///     Gets the status types.
        /// </summary>
        public IDictionary<string, StatusType> StatusTypes
        {
            get { return statusKinds; }
        }

        /// <summary>
        ///     Gets the toggle bar command.
        /// </summary>
        public ICommand ToggleBarCommand
        {
            get { return toggle ?? (toggle = new RelayCommand(OnExpanded)); }
        }

        /// <summary>
        ///     Gets the toggle status window command.
        /// </summary>
        public ICommand ToggleStatusWindowCommand
        {
            get
            {
                return toggleStatus
                       ?? (toggleStatus = new RelayCommand(args => IsChangingStatus = !IsChangingStatus));
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
            var toSend =
                CommandDefinitions.CreateCommand("close", null, args as string).ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        private void OnExpanded(object args = null)
        {
            PmsAreExpanded = PmsAreExpanded || HasNewPm;
            ChannelsAreExpanded = ChannelsAreExpanded || HasNewMessage;
            IsExpanded = !IsExpanded;
        }

        private void RequestNavigate(bool? payload)
        {
            Events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(RequestNavigate);
            RegionManager.Regions[ChatWrapperView.UserbarRegion].Add(Container.Resolve<UserbarView>());
        }

        private void SendStatusChangedCommand()
        {
            var torSend =
                CommandDefinitions.CreateCommand(
                    "status",
                    new List<string>
                        {
                            ChatModel.CurrentCharacter.Status.ToString(),
                            ChatModel.CurrentCharacter.StatusMessage
                        }).ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(torSend);
        }

        // this will update the connection bars to show the user about how good our connection is to the server
        private void UpdateConnectionBars(object sender, EventArgs e)
        {
            var difference = DateTime.Now - ChatModel.LastMessageReceived;
            ConnectionIsPerfect = true;
            ConnectionIsGood = true;
            ConnectionIsModerate = true;
            ConnectionIsConnected = true;

            if (difference.TotalSeconds > 5)
                ConnectionIsPerfect = false;

            if (difference.TotalSeconds > 10)
                ConnectionIsGood = false;

            if (difference.TotalSeconds > 15)
                ConnectionIsModerate = false;

            if (difference.TotalSeconds > 30)
                ConnectionIsConnected = false;

            OnPropertyChanged("ConnectionIsPerfect");
            OnPropertyChanged("ConnectionIsGood");
            OnPropertyChanged("ConnectionIsModerate");
            OnPropertyChanged("ConnectionIsConnected");
        }

        private void UpdateFlashingTabs()
        {
            if (ChatModel.CurrentChannel is PmChannelModel)
                PmSelected = ChatModel.CurrentPms.IndexOf(ChatModel.CurrentChannel as PmChannelModel);
            else
                ChannelSelected = ChatModel.CurrentChannels.IndexOf(ChatModel.CurrentChannel as GeneralChannelModel);

            hasNewPm = ChatModel.CurrentPms.Cast<ChannelModel>().Any(cm => cm.NeedsAttention);
            HasNewMessage = ChatModel.CurrentChannels.Cast<ChannelModel>().Any(cm => cm.NeedsAttention);
        }

        #endregion
    }
}