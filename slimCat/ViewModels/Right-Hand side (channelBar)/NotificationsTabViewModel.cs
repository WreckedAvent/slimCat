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

namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using Views;

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

        private RelayCommand _clearNoti;

        private bool _isSelected;

        private RelayCommand _killNoti;

        private string _search = string.Empty;

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
            this._container.RegisterType<object, NotificationsTabView>(NotificationsTabView);

            this.CM.Notifications.CollectionChanged += (s, e) =>
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
                if (this._clearNoti == null)
                {
                    this._clearNoti = new RelayCommand(args => this.CM.Notifications.Clear());
                }

                return this._clearNoti;
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
                return this.CM.Notifications.Count == 0;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is selected.
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return this._isSelected;
            }

            set
            {
                if (this._isSelected != value)
                {
                    this._isSelected = value;
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
                if (this._killNoti == null)
                {
                    this._killNoti = new RelayCommand(this.RemoveNotification);
                }

                return this._killNoti;
            }
        }

        /// <summary>
        ///     Gets or sets the search string.
        /// </summary>
        public string SearchString
        {
            get
            {
                return this._search.ToLower();
            }

            set
            {
                this._search = value;
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
                if (this._search == string.Empty)
                {
                    return this.CM.Notifications;
                }

                Func<NotificationModel, bool> MeetsString = args =>
                    {
                        string arguments = args.ToString().ToLower();
                        if (args is CharacterUpdateModel)
                        {
                            string characterName = ((CharacterUpdateModel)args).TargetCharacter.Name;
                            return arguments.ContainsOrd(this.SearchString, true)
                                   || characterName.ContainsOrd(this.SearchString, true);
                        }

                        if (args is ChannelUpdateModel)
                        {
                            string channelName = ((ChannelUpdateModel)args).ChannelTitle;
                            return arguments.ContainsOrd(this.SearchString, true)
                                   || channelName.ContainsOrd(this.SearchString, true);
                        }

                        return arguments.Contains(this.SearchString);
                    };

                return this.CM.Notifications.Where(MeetsString);
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
            if (args is NotificationModel)
            {
                var toRemove = args as NotificationModel;
                this.CM.Notifications.Remove(toRemove);
            }
        }

        #endregion
    }
}