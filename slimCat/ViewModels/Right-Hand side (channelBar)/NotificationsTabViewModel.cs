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
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

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

        public bool HasNoNotifications
        {
            get { return CM.Notifications.Count == 0; }
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
                            string characterName = ((CharacterUpdateModel)args).TargetCharacter.Name;
                            return (arguments.ContainsOrd(SearchString,true)
                                || characterName.ContainsOrd(SearchString,true));
                        }

                        if (args is ChannelUpdateModel)
                        {
                            string channelName = ((ChannelUpdateModel)args).ChannelTitle;
                            return (arguments.ContainsOrd(SearchString, true)
                                || channelName.ContainsOrd(SearchString, true));
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
            }
        }

        RelayCommand _clearNoti;
        public ICommand ClearNotificationsCommand
        {
            get
            {
                if (_clearNoti == null)
                    _clearNoti = new RelayCommand(
                        args => CM.Notifications.Clear());
                return _clearNoti;
            }
        }
        #endregion

        #region Constructors
        public NotificationsTabViewModel(IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator eventagg)
            :base(contain, regman, eventagg, cm)
        {
            _container.RegisterType<object, NotificationsTabView>(NotificationsTabView);

            CM.Notifications.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged("NeedsAttention");
                OnPropertyChanged("SortedNotifications");
                OnPropertyChanged("HasNoNotifications");
            };
        }
        #endregion
    }
}
