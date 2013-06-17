/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Windows.Input;
using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using Views;

namespace ViewModels
{
    /// <summary>
    /// The ChannebarViewModel is a wrapper to hold the other viewmodels which form the gist of the interaction network.
    /// It responds to the ChatOnDisplayEvent to paritally create the chat wrapper.
    /// </summary>
    public class ChannelbarViewModel : ChannelbarViewModelCommon
    {
        #region Fields
        private bool _isExpanded = true;
        private bool _hasUpdate = false;
        private string _currentSelected;

        public const string ChannelbarView = "ChannelbarView";
        private const string TabViewRegion = "TabViewRegion";
        public event EventHandler OnJumpToNotifications;
        #endregion

        #region Properties
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; OnPropertyChanged("IsExpanded"); OnPropertyChanged("ExpandString"); }
        }

        public string ExpandString
        {
            get
            {
                if (HasUpdate && !IsExpanded) return "!";
                return (IsExpanded ? ">" : "<");
            }
        }

        public bool HasUpdate
        {
            get { return _hasUpdate; }
            set
            {
                _hasUpdate = value;
                OnPropertyChanged("HasUpdate");
                OnPropertyChanged("ExpandString"); // bug fix where the exclaimation point would never show
            }
        }
        #endregion

        #region Constructors
        public ChannelbarViewModel(IChatModel cm, IUnityContainer contain, IRegionManager regman,
                                IEventAggregator events)
            : base(contain, regman, events, cm)
        {
            try
            {
                _events.GetEvent<ChatOnDisplayEvent>().Subscribe(requestNavigate, ThreadOption.UIThread, true);

                // create the tabs
                _container.Resolve<ChannelsTabViewModel>();
                _container.Resolve<UsersTabViewModel>();
                _container.Resolve<NotificationsTabViewModel>();
                _container.Resolve<GlobalTabViewModel>();
                _container.Resolve<ManageListsTabView>();

                _cm.Notifications.CollectionChanged += (s, e) =>
                    {
                        if (!IsExpanded) // removed checking logic, allow the notifications daemon to worry about that
                            HasUpdate = HasUpdate || true;
                    };
            }

            catch (Exception ex)
            {
                ex.Source = "Channelbar ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        public override void Initialize()
        {
            try
            {
                _container.RegisterType<object, ChannelbarView>(ChannelbarView);
            }
            catch (Exception ex)
            {
                ex.Source = "Channelbar ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        private void requestNavigate(bool? payload)
        {
            _events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(requestNavigate);
            _region.Regions[ChatWrapperView.ChannelbarRegion].Add(_container.Resolve<ChannelbarView>());
        }
        #endregion

        #region Commands
        private RelayCommand _select;
        public ICommand ChangeTabCommand
        {
            get
            {
                if (_select == null)
                    _select = new RelayCommand(NavigateToTabEvent);
                return _select;
            }
        }

        private RelayCommand _toggle;
        public ICommand ToggleBarCommand
        {
            get
            {
                if (_toggle == null)
                {
                    _toggle = new RelayCommand(
                        delegate
                        {
                            IsExpanded = !IsExpanded;

                            if (IsExpanded)
                            {
                                // this shoots us to the notifications tab if we have something to see there
                                if (_hasUpdate)
                                {
                                    NavigateToTabEvent("Notifications"); // used to check if we weren't already here; now that isn't possible

                                    if (OnJumpToNotifications != null)
                                        OnJumpToNotifications(this, new EventArgs()); // this lets the view sync our jump

                                    HasUpdate = false;
                                }
                                else if (!String.IsNullOrWhiteSpace(_currentSelected))
                                    // this fixes a very subtle bug where a list won't load or won't load properly after switching tabs
                                    NavigateToTabEvent(_currentSelected);
                            }
                            else // when we close it, unload the tab, but _currentSelected remains what it was so we remember user input
                                NavigateToTabEvent("NoTab");
                        });
                }

                return _toggle;
            }
        }

        private void NavigateToTabEvent(object args)
        {
            var newSelected = args as string;

            if (newSelected as string != "NoTab") // this isn't really a selected state
                _currentSelected = newSelected;

            switch (args as string)
            {
                case "Channels":
                    {
                        _region.Regions[TabViewRegion].RequestNavigate(ChannelsTabViewModel.ChannelsTabView);
                        break;
                    }

                case "Users":
                    {
                        _region.Regions[TabViewRegion].RequestNavigate(UsersTabViewModel.UsersTabView);
                        break;
                    }

                case "Notifications":
                    {
                        _region.Regions[TabViewRegion].RequestNavigate(NotificationsTabViewModel.NotificationsTabView);
                        break;
                    }

                case "Global":
                    {
                        _region.Regions[TabViewRegion].RequestNavigate(GlobalTabViewModel.GlobalTabView);
                        break;
                    }

                case "ManageLists":
                    {
                        _region.Regions[TabViewRegion].RequestNavigate(ManageListsViewModel.ManageListsTabView);
                        break;
                    }

                case "NoTab":
                    {
                        foreach (var view in _region.Regions[TabViewRegion].Views)
                            _region.Regions[TabViewRegion].Remove(view);
                        break;
                    }

                default: break;
            }
        }
        #endregion
    }
}
