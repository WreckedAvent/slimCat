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
using System.Windows.Input;
using lib;
using Views;
using Models;
using Microsoft.Practices.Prism.Events;
using slimCat;

namespace ViewModels
{
    /// <summary>
    /// A light-weight viewmodel for toastnofications
    /// </summary>
    public class ToastNotificationsViewModel : SysProp, IDisposable
    {
        #region Fields
        private string _content = "";
        private const int cutoffLength = 300;
        System.Timers.Timer _hideDelay = new System.Timers.Timer(5000);
        NotificationsView _view;
        IEventAggregator _events;
        #endregion

        #region Properties
        public ToastNotificationsViewModel(IEventAggregator _eventAgg)
        {
            _hideDelay.Elapsed += (s, e) =>
                {
                    HideNotifications();
                };
            _events = _eventAgg;
        }
        public string Content
        {
            get { return _content; }
            set
            {
                if (value.Length < cutoffLength)
                    _content = value;

                else
                {
                    var brevity = value.Substring(0, cutoffLength);
                    brevity += " ...";
                    _content = brevity;
                }

                OnPropertyChanged("Content");
            }
        }

        /// <summary>
        /// Who we will try and snap to when the user clicks on it if this event doesn't generate an actual notification
        /// </summary>
        public string Target { get; set; }
        #endregion

        #region Methods
        public void ShowNotifications()
        {
            _hideDelay.Stop();
            if (_view == null) _view = new NotificationsView(this);
            Dispatcher.Invoke(
                (Action)delegate
                {
                    _view.OnShowCommand();
                });
            _hideDelay.Start();
        }

        public void HideNotifications()
        {
            Dispatcher.Invoke(
            (Action)delegate
            {
                _view.OnHideCommand();
                _hideDelay.Stop();
            });
        }

        public void UpdateNotification(string content)
        {
            Content = content;
            ShowNotifications();
            _view.OnContentChanged();
        }

        #region Commands
        private RelayCommand _hide;
        public ICommand HideCommand
        {
            get
            {
                if (_hide == null)
                    _hide = new RelayCommand(args => HideNotifications());
                return _hide;
            }
        }
        #endregion
        #endregion

        #region Commands
        private RelayCommand _snap;
        public ICommand SnapToLatestCommand
        {
            get
            {
                if (_snap == null)
                    _snap = new RelayCommand(OnSnapToLatestEvent);
                return _snap;
            }
        }

        public void OnSnapToLatestEvent(object args)
        {
            IDictionary<string, object> toSend = CommandDefinitions.CreateCommand("lastupdate").toDictionary();

            if (Target != null)
                toSend.Add("target", Target);

            HideNotifications();
            _events.GetEvent<UserCommandEvent>().Publish(toSend);
        }
        #endregion

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool isManagedDispose)
        {
            if (isManagedDispose)
            {
                _hideDelay.Dispose();
                _view.Close();
                _view = null;
            }
        }
    }
}
