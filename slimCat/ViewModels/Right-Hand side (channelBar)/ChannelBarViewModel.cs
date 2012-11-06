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

                _events.GetEvent<NewUpdateEvent>().Subscribe(args => 
                    {
                        if (args is CharacterUpdateModel)
                        {
                            string name = ((CharacterUpdateModel)args).TargetCharacter.Name;
                            if (CM.IsOfInterest(name))
                                HasUpdate = HasUpdate || !IsExpanded;
                        }

                        if (args is ChannelUpdateModel)
                        {
                            var update = ((ChannelUpdateModel)args);

                            if (CM.SelectedChannel.ID.Equals(update.ChannelID))
                                HasUpdate = HasUpdate || !IsExpanded;

                            else if (update.Arguments is Models.ChannelUpdateModel.ChannelInviteEventArgs)
                                HasUpdate = HasUpdate || !IsExpanded;
                        }
                    });
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
                                    if (_currentSelected != "Notifications")
                                        NavigateToTabEvent("Notifications");

                                    if (OnJumpToNotifications != null)
                                        OnJumpToNotifications(this, new EventArgs()); // this lets the view sync our jump

                                    _hasUpdate = false;
                                    OnPropertyChanged("HasUpdate");
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
