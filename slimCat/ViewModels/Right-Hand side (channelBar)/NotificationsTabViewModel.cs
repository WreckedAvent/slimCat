using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Views;
using lib;
using System.Windows.Input;

namespace ViewModels
{
    /// <summary>
    /// This is the tab labled "notifications" in the channel bar, or the bar on the right-hand side
    /// </summary>
    public class NotificationsTabViewModel : ChannelbarViewModelCommon
    {
        #region Fields
        private string _search = "";
        public const string NotificationsTabView = "NotificationsTabView";
        private bool _isSelected;
        // removed useless code which kept unread count of notifications
        #endregion

        #region Properties
        public string SearchString
        {
            get { return _search.ToLower(); }
            set
            {
                _search = value;
                OnPropertyChanged("SearchString");
                OnPropertyChanged("SortedNotifications");
            }
        }

        public IEnumerable<NotificationModel> SortedNotifications
        {
            get 
            {
                if (_search == "") return CM.Notifications;

                Func<NotificationModel, bool> MeetsString = args =>
                    {
                        string arguments = args.ToString().ToLower();
                        if (args is CharacterUpdateModel)
                        {
                            string characterName = ((CharacterUpdateModel)args).TargetCharacter.Name.ToLower();
                            return (arguments.Contains(SearchString) ||
                                (characterName.Contains(SearchString)));
                        }
                        return arguments.Contains(SearchString);
                    };

                return CM.Notifications.Where(MeetsString); 
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("NeedsAttention");
                }
            }
        }
        #endregion

        #region Commands
        private RelayCommand _killNoti;
        public ICommand RemoveNotificationCommand
        {
            get
            {
                if (_killNoti == null)
                    _killNoti = new RelayCommand(RemoveNotification);
                return _killNoti;
            }
        }

        public void RemoveNotification(object args)
        {
            if (args is NotificationModel)
            {
                var toRemove = args as NotificationModel;
                CM.Notifications.Remove(toRemove);
                OnPropertyChanged("SortedNotifications");
            }
        }
        #endregion

        #region Constructors
        public NotificationsTabViewModel(IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            :base(contain, regman, eventagg, cm)
        {
            _container.RegisterType<object, NotificationsTabView>(NotificationsTabView);

            CM.Notifications.CollectionChanged += (s, e) => OnPropertyChanged("NeedsAttention");
        }
        #endregion
    }
}
