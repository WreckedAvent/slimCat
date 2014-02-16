#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsTabViewModel.cs">
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

    using System.Collections.ObjectModel;
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
    ///     This is the tab labled "notifications" in the channel bar, or the bar on the right-hand side
    /// </summary>
    public class NotificationsTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        public const string NotificationsTabView = "NotificationsTabView";

        #endregion

        #region Fields

        private readonly FilteredCollection<NotificationModel, IViewableObject> notificationManager;

        private RelayCommand clearNoti;

        private bool isSelected;

        private RelayCommand killNoti;

        private string search = string.Empty;

        #endregion

        #region Constructors and Destructors

        public NotificationsTabViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg,
            ICharacterManager manager)
            : base(contain, regman, eventagg, cm, manager)
        {
            Container.RegisterType<object, NotificationsTabView>(NotificationsTabView);

            notificationManager =
                new FilteredCollection<NotificationModel, IViewableObject>(
                    ChatModel.Notifications, MeetsFilter, true);

            notificationManager.Collection.CollectionChanged += (sender, args) =>
                {
                    OnPropertyChanged("HasNoNotifications");
                    OnPropertyChanged("NeedsAttention");
                };
        }

        #endregion

        #region Public Properties

        public ICommand ClearNotificationsCommand
        {
            get
            {
                return clearNoti
                       ?? (clearNoti = new RelayCommand(args => ChatModel.Notifications.Clear()));
            }
        }

        public bool HasNoNotifications
        {
            get { return notificationManager.Collection.Count == 0; }
        }

        public bool IsSelected
        {
            get { return isSelected; }

            set
            {
                if (isSelected == value)
                    return;

                isSelected = value;
                OnPropertyChanged("NeedsAttention");
            }
        }

        public ICommand RemoveNotificationCommand
        {
            get { return killNoti ?? (killNoti = new RelayCommand(RemoveNotification)); }
        }

        public string SearchString
        {
            get { return search.ToLower(); }

            set
            {
                search = value;
                OnPropertyChanged("SearchString");
                notificationManager.RebuildItems();
            }
        }

        public ObservableCollection<IViewableObject> CurrentNotifications
        {
            get { return notificationManager.Collection; }
        }

        #endregion

        #region Public Methods and Operators

        public void RemoveNotification(object args)
        {
            var model = args as NotificationModel;
            if (model != null)
                ChatModel.Notifications.Remove(model);
        }

        #endregion

        #region Methods

        private bool MeetsFilter(NotificationModel item)
        {
            return item.ToString().ContainsOrdinal(search);
        }

        #endregion
    }
}