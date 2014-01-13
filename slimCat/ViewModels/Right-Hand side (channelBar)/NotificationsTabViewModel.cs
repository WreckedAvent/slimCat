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

namespace Slimcat.ViewModels
{
    #region Usings

    using System.Collections.ObjectModel;
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
    ///     This is the tab labled "notifications" in the channel bar, or the bar on the right-hand side
    /// </summary>
    public class NotificationsTabViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        /// <summary>
        ///     The notifications tab view.
        /// </summary>
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

        /// <summary>
        ///     Initializes a new instance of the <see cref="NotificationsTabViewModel" /> class.
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
        /// <param name="eventagg">
        ///     The eventagg.
        /// </param>
        public NotificationsTabViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg, ICharacterManager manager)
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

        /// <summary>
        ///     Gets the clear notifications command.
        /// </summary>
        public ICommand ClearNotificationsCommand
        {
            get
            {
                return clearNoti
                       ?? (clearNoti = new RelayCommand(args => ChatModel.Notifications.Clear()));
            }
        }

        // removed useless code which kept unread count of notifications

        /// <summary>
        ///     Gets a value indicating whether has no notifications.
        /// </summary>
        public bool HasNoNotifications
        {
            get { return notificationManager.Collection.Count == 0; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is selected.
        /// </summary>
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

        /// <summary>
        ///     Gets the remove notification command.
        /// </summary>
        public ICommand RemoveNotificationCommand
        {
            get { return killNoti ?? (killNoti = new RelayCommand(RemoveNotification)); }
        }

        /// <summary>
        ///     Gets or sets the search string.
        /// </summary>
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

        /// <summary>
        ///     Gets the sorted notifications.
        /// </summary>
        public ObservableCollection<IViewableObject> CurrentNotifications
        {
            get { return notificationManager.Collection; }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The remove notification.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
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