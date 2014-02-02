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

        /// <summary>
        ///     The channelbar view.
        /// </summary>
        internal const string ChannelbarView = "ChannelbarView";

        private const string TabViewRegion = "TabViewRegion";

        #endregion

        #region Fields

        private string currentSelected;

        private bool hasUpdate;

        private bool isExpanded = true;

        private RelayCommand @select;

        private RelayCommand toggle;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelbarViewModel" /> class.
        /// </summary>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="events">
        ///     The events.
        /// </param>
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

                ChatModel.Notifications.CollectionChanged += (s, e) =>
                    {
                        if (!IsExpanded)
                        {
                            // removed checking logic, allow the notifications daemon to worry about that
                            HasUpdate = true;
                        }
                    };
            }
            catch (Exception ex)
            {
                ex.Source = "Channelbar ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     The on jump to notifications.
        /// </summary>
        public event EventHandler OnJumpToNotifications;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the change tab command.
        /// </summary>
        public ICommand ChangeTabCommand
        {
            get { return @select ?? (@select = new RelayCommand(NavigateToTabEvent)); }
        }

        /// <summary>
        ///     Gets the expand string.
        /// </summary>
        public string ExpandString
        {
            get
            {
                if (HasUpdate && !IsExpanded)
                    return "!";

                return IsExpanded ? ">" : "<";
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether has update.
        /// </summary>
        public bool HasUpdate
        {
            get { return hasUpdate; }

            set
            {
                hasUpdate = value;
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString"); // bug fix where the exclaimation point would never show
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

        /// <summary>
        ///     Gets the toggle bar command.
        /// </summary>
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
                                if (hasUpdate)
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

        /// <summary>
        ///     The initialize.
        /// </summary>
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
                    RegionManager.Regions[TabViewRegion].RequestNavigate(
                        NotificationsTabViewModel.NotificationsTabView);
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
        }

        #endregion
    }
}