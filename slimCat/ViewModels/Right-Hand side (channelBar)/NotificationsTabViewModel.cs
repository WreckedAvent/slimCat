// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsTabViewModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   This is the tab labled "notifications" in the channel bar, or the bar on the right-hand side
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat.Libraries;
    using Slimcat.Models;
    using Slimcat.Utilities;
    using Slimcat.Views;

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

        private RelayCommand clearNoti;

        private bool isSelected;

        private RelayCommand killNoti;

        private string search = string.Empty;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsTabViewModel"/> class.
        /// </summary>
        /// <param name="cm">
        /// The cm.
        /// </param>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="eventagg">
        /// The eventagg.
        /// </param>
        public NotificationsTabViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            : base(contain, regman, eventagg, cm)
        {
            this.Container.RegisterType<object, NotificationsTabView>(NotificationsTabView);

            this.ChatModel.Notifications.CollectionChanged += (s, e) =>
                {
                    this.OnPropertyChanged("NeedsAttention");
                    this.OnPropertyChanged("SortedNotifications");
                    this.OnPropertyChanged("HasNoNotifications");
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
                return this.clearNoti
                       ?? (this.clearNoti = new RelayCommand(args => this.ChatModel.Notifications.Clear()));
            }
        }

        // removed useless code which kept unread count of notifications

        /// <summary>
        ///     Gets a value indicating whether has no notifications.
        /// </summary>
        public bool HasNoNotifications
        {
            get
            {
                return this.ChatModel.Notifications.Count == 0;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is selected.
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return this.isSelected;
            }

            set
            {
                if (this.isSelected != value)
                {
                    this.isSelected = value;
                    this.OnPropertyChanged("NeedsAttention");
                }
            }
        }

        /// <summary>
        ///     Gets the remove notification command.
        /// </summary>
        public ICommand RemoveNotificationCommand
        {
            get
            {
                return this.killNoti ?? (this.killNoti = new RelayCommand(this.RemoveNotification));
            }
        }

        /// <summary>
        ///     Gets or sets the search string.
        /// </summary>
        public string SearchString
        {
            get
            {
                return this.search.ToLower();
            }

            set
            {
                this.search = value;
                this.OnPropertyChanged("SearchString");
                this.OnPropertyChanged("SortedNotifications");
            }
        }

        /// <summary>
        ///     Gets the sorted notifications.
        /// </summary>
        public IEnumerable<NotificationModel> SortedNotifications
        {
            get
            {
                if (this.search == string.Empty)
                {
                    return this.ChatModel.Notifications;
                }

                Func<NotificationModel, bool> MeetsString = args =>
                    {
                        var arguments = args.ToString().ToLower();
                        if (args is CharacterUpdateModel)
                        {
                            var characterName = ((CharacterUpdateModel)args).TargetCharacter.Name;
                            return arguments.ContainsOrd(this.SearchString, true)
                                   || characterName.ContainsOrd(this.SearchString, true);
                        }

                        if (args is ChannelUpdateModel)
                        {
                            var channelName = ((ChannelUpdateModel)args).ChannelTitle;
                            return arguments.ContainsOrd(this.SearchString, true)
                                   || channelName.ContainsOrd(this.SearchString, true);
                        }

                        return arguments.Contains(this.SearchString);
                    };

                return this.ChatModel.Notifications.Where(MeetsString);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The remove notification.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public void RemoveNotification(object args)
        {
            if (!(args is NotificationModel))
            {
                return;
            }

            var toRemove = args as NotificationModel;
            this.ChatModel.Notifications.Remove(toRemove);
        }

        #endregion
    }
}