#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ToastNotificationsViewModel.cs">
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

    using System;
    using System.Timers;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Views;

    #endregion

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
        ///     Initializes a new instance of the <see cref="ToastNotificationsViewModel" /> class.
        /// </summary>
        /// <param name="eventAgg">
        ///     The _event agg.
        /// </param>
        public ToastNotificationsViewModel(IEventAggregator eventAgg)
        {
            hideDelay.Elapsed += (s, e) => HideNotifications();
            events = eventAgg;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the content.
        /// </summary>
        public string Content
        {
            get { return content; }

            private set
            {
                if (value.Length < CutoffLength)
                    content = value;
                else
                {
                    var brevity = value.Substring(0, CutoffLength);
                    brevity += " ...";
                    content = brevity;
                }

                OnPropertyChanged("Content");
            }
        }

        /// <summary>
        ///     Gets the hide command.
        /// </summary>
        public ICommand HideCommand
        {
            get { return hide ?? (hide = new RelayCommand(args => HideNotifications())); }
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
            get { return snap ?? (snap = new RelayCommand(OnSnapToLatestEvent)); }
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
            Dispatcher.Invoke(
                (Action) delegate
                    {
                        view.OnHideCommand();
                        hideDelay.Stop();
                    });
        }

        /// <summary>
        ///     The on snap to latest event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        public void OnSnapToLatestEvent(object args)
        {
            var toSend = CommandDefinitions.CreateCommand("lastupdate").ToDictionary();

            if (Target != null)
                toSend.Add("target", Target);

            if (Kind != null)
                toSend.Add("kind", Kind);

            HideNotifications();
            events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        /// <summary>
        ///     The show notifications.
        /// </summary>
        public void ShowNotifications()
        {
            hideDelay.Stop();
            if (view == null)
                view = new NotificationsView(this);

            Dispatcher.Invoke((Action) (() => view.OnShowCommand()));
            hideDelay.Start();
        }

        /// <summary>
        ///     The update notification.
        /// </summary>
        /// <param name="newContent">
        ///     The content.
        /// </param>
        public void UpdateNotification(string newContent)
        {
            Content = newContent;
            ShowNotifications();
            view.OnContentChanged();
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
            {
                hideDelay.Dispose();
                view.Close();
                view = null;
            }

            base.Dispose(isManaged);
        }

        #endregion
    }
}