// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ToastNotificationsViewModel.cs" company="Justin Kadrovach">
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
//   A light-weight viewmodel for toastnofications
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Timers;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;

    using Models;

    using slimCat;

    using Views;

    /// <summary>
    ///     A light-weight viewmodel for toastnofications
    /// </summary>
    public class ToastNotificationsViewModel : SysProp, IDisposable
    {
        #region Constants

        private const int cutoffLength = 300;

        #endregion

        #region Fields

        private readonly IEventAggregator _events;

        private readonly Timer _hideDelay = new Timer(5000);

        private string _content = string.Empty;

        private RelayCommand _hide;

        private RelayCommand _snap;

        private NotificationsView _view;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ToastNotificationsViewModel"/> class.
        /// </summary>
        /// <param name="_eventAgg">
        /// The _event agg.
        /// </param>
        public ToastNotificationsViewModel(IEventAggregator _eventAgg)
        {
            this._hideDelay.Elapsed += (s, e) => { this.HideNotifications(); };
            this._events = _eventAgg;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the content.
        /// </summary>
        public string Content
        {
            get
            {
                return this._content;
            }

            set
            {
                if (value.Length < cutoffLength)
                {
                    this._content = value;
                }
                else
                {
                    string brevity = value.Substring(0, cutoffLength);
                    brevity += " ...";
                    this._content = brevity;
                }

                this.OnPropertyChanged("Content");
            }
        }

        /// <summary>
        ///     Gets the hide command.
        /// </summary>
        public ICommand HideCommand
        {
            get
            {
                if (this._hide == null)
                {
                    this._hide = new RelayCommand(args => this.HideNotifications());
                }

                return this._hide;
            }
        }

        /// <summary>
        ///     The kind of target specified
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        ///     Gets the snap to latest command.
        /// </summary>
        public ICommand SnapToLatestCommand
        {
            get
            {
                if (this._snap == null)
                {
                    this._snap = new RelayCommand(this.OnSnapToLatestEvent);
                }

                return this._snap;
            }
        }

        /// <summary>
        ///     Who we will try and snap to when the user clicks on it if this event doesn't generate an actual notification
        /// </summary>
        public string Target { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        ///     The hide notifications.
        /// </summary>
        public void HideNotifications()
        {
            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        this._view.OnHideCommand();
                        this._hideDelay.Stop();
                    });
        }

        /// <summary>
        /// The on snap to latest event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public void OnSnapToLatestEvent(object args)
        {
            IDictionary<string, object> toSend = CommandDefinitions.CreateCommand("lastupdate").toDictionary();

            if (this.Target != null)
            {
                toSend.Add("target", this.Target);
            }

            if (this.Kind != null)
            {
                toSend.Add("kind", this.Kind);
            }

            this.HideNotifications();
            this._events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        /// <summary>
        ///     The show notifications.
        /// </summary>
        public void ShowNotifications()
        {
            this._hideDelay.Stop();
            if (this._view == null)
            {
                this._view = new NotificationsView(this);
            }

            this.Dispatcher.Invoke((Action)delegate { this._view.OnShowCommand(); });
            this._hideDelay.Start();
        }

        /// <summary>
        /// The update notification.
        /// </summary>
        /// <param name="content">
        /// The content.
        /// </param>
        public void UpdateNotification(string content)
        {
            this.Content = content;
            this.ShowNotifications();
            this._view.OnContentChanged();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="isManagedDispose">
        /// The is managed dispose.
        /// </param>
        protected virtual void Dispose(bool isManagedDispose)
        {
            if (isManagedDispose)
            {
                this._hideDelay.Dispose();
                this._view.Close();
                this._view = null;
            }
        }

        #endregion
    }
}