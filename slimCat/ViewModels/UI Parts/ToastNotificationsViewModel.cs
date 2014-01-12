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

namespace Slimcat.ViewModels
{
    using System;
    using System.Timers;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;

    using Libraries;
    using Models;
    using Views;

    /// <summary>
    ///     A light-weight viewmodel for toastnofications
    /// </summary>
    public sealed class ToastNotificationsViewModel : SysProp
    {
        #region Constants

        private const int CutoffLength = 300;

        #endregion

        #region Fields

        private readonly IEventAggregator events;

        private readonly Timer hideDelay = new Timer(5000);

        private string content = string.Empty;

        private RelayCommand hide;

        private RelayCommand snap;

        private NotificationsView view;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ToastNotificationsViewModel"/> class.
        /// </summary>
        /// <param name="eventAgg">
        /// The _event agg.
        /// </param>
        public ToastNotificationsViewModel(IEventAggregator eventAgg)
        {
            this.hideDelay.Elapsed += (s, e) => this.HideNotifications();
            this.events = eventAgg;
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
                return this.content;
            }

            private set
            {
                if (value.Length < CutoffLength)
                {
                    this.content = value;
                }
                else
                {
                    var brevity = value.Substring(0, CutoffLength);
                    brevity += " ...";
                    this.content = brevity;
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
                return this.hide ?? (this.hide = new RelayCommand(args => this.HideNotifications()));
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
                return this.snap ?? (this.snap = new RelayCommand(this.OnSnapToLatestEvent));
            }
        }

        /// <summary>
        ///     Who we will try and snap to when the user clicks on it if this event doesn't generate an actual notification
        /// </summary>
        public string Target { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The hide notifications.
        /// </summary>
        public void HideNotifications()
        {
            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        this.view.OnHideCommand();
                        this.hideDelay.Stop();
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
            var toSend = CommandDefinitions.CreateCommand("lastupdate").ToDictionary();

            if (this.Target != null)
            {
                toSend.Add("target", this.Target);
            }

            if (this.Kind != null)
            {
                toSend.Add("kind", this.Kind);
            }

            this.HideNotifications();
            this.events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        /// <summary>
        ///     The show notifications.
        /// </summary>
        public void ShowNotifications()
        {
            this.hideDelay.Stop();
            if (this.view == null)
            {
                this.view = new NotificationsView(this);
            }

            this.Dispatcher.Invoke((Action)(() => this.view.OnShowCommand()));
            this.hideDelay.Start();
        }

        /// <summary>
        /// The update notification.
        /// </summary>
        /// <param name="newContent">
        /// The content.
        /// </param>
        public void UpdateNotification(string newContent)
        {
            this.Content = newContent;
            this.ShowNotifications();
            this.view.OnContentChanged();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
            {
                this.hideDelay.Dispose();
                this.view.Close();
                this.view = null;
            }

            base.Dispose(isManaged);
        }

        #endregion
    }
}