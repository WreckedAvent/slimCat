using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Views;

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
        #endregion

        #region Constructors
        public NotificationsTabViewModel(IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            :base(contain, regman, eventagg, cm)
        {
            _container.RegisterType<object, NotificationsTabView>(NotificationsTabView);
        }
        #endregion
    }
}
