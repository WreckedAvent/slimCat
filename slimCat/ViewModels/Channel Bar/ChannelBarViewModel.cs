#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelBarViewModel.cs">
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
    using System.Linq;
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
    ///     The ChannelbarViewModel is a wrapper to hold the other viewmodels which form the gist of the interaction network.
    ///     It responds to the ChatOnDisplayEvent to partially create the chat wrapper.
    /// </summary>
    public class ChannelbarViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        internal const string ChannelbarView = "ChannelbarView";

        private const string TabViewRegion = "TabViewRegion";

        #endregion

        #region Fields

        private string currentSelected;
        private bool hasUpdate;

        private bool isExpanded = true;
        private bool needsAttention;

        private RelayCommand @select;

        private RelayCommand toggle;

        #endregion

        #region Constructors and Destructors

        public ChannelbarViewModel(IChatState chatState)
            : base(chatState)
        {
            try
            {
                Events.GetEvent<ChatOnDisplayEvent>().Subscribe(RequestNavigate, ThreadOption.UIThread, true);

                // create the tabs
                Container.Resolve<ChannelsTabViewModel>();
                Container.Resolve<UsersTabViewModel>();
                Container.Resolve<NotificationsTabViewModel>();
                Container.Resolve<GlobalTabViewModel>();
                Container.Resolve<ManageListsTabView>();
                Container.Resolve<SearchTabViewModel>();

                ChatModel.Notifications.CollectionChanged += (s, e) => HasUpdate = ChatModel.Notifications.Any();

                Events.GetEvent<ChatSearchResultEvent>().Subscribe(success =>
                {
                    if (!success) return;

                    if (!IsExpanded) 
                        IsExpanded = true;

                    if (currentSelected != "ManageLists")
                        NavigateToTabEvent("ManageLists");

                    if (OnJumpToSearch != null)
                        OnJumpToSearch(this, null);
                }, ThreadOption.UIThread);

                LoggingSection = "channel bar vm";
            }
            catch (Exception ex)
            {
                ex.Source = "Channelbar ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Events

        public event EventHandler OnJumpToNotifications;

        public event EventHandler OnJumpToSearch;

        #endregion

        #region Public Properties

        public ICommand ChangeTabCommand
        {
            get { return @select ?? (@select = new RelayCommand(NavigateToTabEvent)); }
        }

        public string ExpandString
        {
            get
            {
                if (NeedsAttention && !IsExpanded)
                    return "!";

                return IsExpanded ? ">" : "<";
            }
        }

        public bool NeedsAttention
        {
            get { return needsAttention; }

            set
            {
                if (value) Log("displaying update");
                needsAttention = value;
                OnPropertyChanged("NeedsAttention");
                OnPropertyChanged("ExpandString");
            }
        }

        public bool HasUpdate
        {
            get { return hasUpdate; }
            set
            {
                if (value == hasUpdate && needsAttention == value) return;

                NeedsAttention = value && !IsExpanded;
                hasUpdate = value;
                OnPropertyChanged("HasUpdate");
            }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }

            set
            {
                if (isExpanded == value) return;

                Log(value ? "Expanding" : "Hiding");
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
                OnPropertyChanged("ExpandString");
            }
        }

        public ICommand ToggleBarCommand
        {
            get
            {
                return toggle ?? (toggle = new RelayCommand(
                    delegate
                    {
                        IsExpanded = !IsExpanded;

                        if (IsExpanded)
                        {
                            // this shoots us to the notifications tab if we have something to see there
                            if (HasUpdate)
                            {
                                NavigateToTabEvent("Notifications");

                                // used to check if we weren't already here; now that isn't possible
                                if (OnJumpToNotifications != null)
                                {
                                    OnJumpToNotifications(
                                        this, new EventArgs());

                                    // this lets the view sync our jump
                                }

                                HasUpdate = false;
                            }
                            else if (
                                !string.IsNullOrWhiteSpace(
                                    currentSelected))
                            {
                                // this fixes a very subtle bug where a list won't load or won't load properly after switching tabs
                                NavigateToTabEvent(
                                    currentSelected);
                            }
                        }
                        else
                        {
                            // when we close it, unload the tab, but _currentSelected remains what it was so we remember user input
                            NavigateToTabEvent("NoTab");
                        }
                    }));
            }
        }

        #endregion

        #region Public Methods and Operators

        public override void Initialize()
        {
            try
            {
                Container.RegisterType<object, ChannelbarView>(ChannelbarView);
            }
            catch (Exception ex)
            {
                ex.Source = "Channelbar ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        private void NavigateToTabEvent(object args)
        {
            if (currentSelected == "Notifications")
            {
                // if we just came from notifications, we have nothing new to see
                HasUpdate = false;

                if (ApplicationSettings.WipeNotificationsOnTabChange && !isExpanded)
                    ChatModel.Notifications.Clear();
            }

            var newSelected = args as string;

            if (newSelected != "NoTab")
            {
                // this isn't really a selected state
                currentSelected = newSelected;
            }

            Log("Requesting " + args + " tab view");

            switch (args as string)
            {
                case "Channels":
                {
                    RegionManager.Regions[TabViewRegion].RequestNavigate(ChannelsTabViewModel.ChannelsTabView);
                    break;
                }

                case "Users":
                {
                    RegionManager.Regions[TabViewRegion].RequestNavigate(UsersTabViewModel.UsersTabView);
                    break;
                }

                case "Notifications":
                {
                    RegionManager.Regions[TabViewRegion].RequestNavigate(NotificationsTabViewModel.NotificationsTabView);
                    HasUpdate = false;
                    break;
                }

                case "Global":
                {
                    RegionManager.Regions[TabViewRegion].RequestNavigate(GlobalTabViewModel.GlobalTabView);
                    break;
                }

                case "ManageLists":
                {
                    RegionManager.Regions[TabViewRegion].RequestNavigate(ManageListsViewModel.ManageListsTabView);
                    break;
                }

                case "Search":
                {
                    RegionManager.Regions[TabViewRegion].RequestNavigate(SearchTabViewModel.SearchTabView);
                    break;
                }

                case "NoTab":
                {
                    foreach (var view in RegionManager.Regions[TabViewRegion].Views)
                        RegionManager.Regions[TabViewRegion].Remove(view);

                    break;
                }
            }
        }

        private void RequestNavigate(bool? payload)
        {
            Events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(RequestNavigate);
            var region = RegionManager.Regions[ChatWrapperView.ChannelbarRegion];

            if (!region.Views.Any())
                region.Add(Container.Resolve<ChannelbarView>());
            Log("Requesting channel bar view");
        }

        #endregion
    }
}