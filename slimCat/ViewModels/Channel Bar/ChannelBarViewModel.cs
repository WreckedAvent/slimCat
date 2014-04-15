#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelBarViewModel.cs">
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
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     The ChannebarViewModel is a wrapper to hold the other viewmodels which form the gist of the interaction network.
    ///     It responds to the ChatOnDisplayEvent to paritally create the chat wrapper.
    /// </summary>
    public class ChannelbarViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        internal const string ChannelbarView = "ChannelbarView";

        private const string TabViewRegion = "TabViewRegion";

        #endregion

        #region Fields

        private string currentSelected;

        private bool needsAttention;

        private bool isExpanded = true;

        private RelayCommand @select;

        private RelayCommand toggle;

        private bool hasUpdate;

        #endregion

        #region Constructors and Destructors

        public ChannelbarViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator events,
            ICharacterManager manager)
            : base(contain, regman, events, cm, manager)
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

                ChatModel.Notifications.CollectionChanged += (s, e) => HasUpdate = true;

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
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString");
            }
        }

        public bool HasUpdate
        {
            get { return hasUpdate; }
            set
            {
                if (value == hasUpdate) return;

                NeedsAttention = (value && !IsExpanded);
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
            RegionManager.Regions[ChatWrapperView.ChannelbarRegion].Add(Container.Resolve<ChannelbarView>());
            Log("Requesting channel bar view");
        }

        #endregion
    }
}