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
    using System.Linq;
    using System.Timers;
    using System.Windows.Automation.Peers;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
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

        private readonly char[] splitChars = {'\n'};

        #endregion

        #region Fields

        private readonly IEventAggregator events;

        private readonly Timer hideDelay = new Timer(5000);

        private string content = string.Empty;

        private RelayCommand hide;

        private RelayCommand snap;

        private NotificationsView view;

        private ICharacter targetCharacter;
        private string title;

        #endregion

        #region Constructors and Destructors

        public ToastNotificationsViewModel(IEventAggregator eventAgg)
        {
            hideDelay.Elapsed += (s, e) => HideNotifications();
            events = eventAgg;
        }

        #endregion

        #region Public Properties

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

        public ICommand HideCommand
        {
            get { return hide ?? (hide = new RelayCommand(args => HideNotifications())); }
        }

        public string Kind { get; set; }

        public ICommand SnapToLatestCommand
        {
            get { return snap ?? (snap = new RelayCommand(OnSnapToLatestEvent)); }
        }

        /// <summary>
        ///     Who we will try and snap to when the user clicks on it if this event doesn't generate an actual notification
        /// </summary>
        public string Target { get; set; }

        public ICharacter TargetCharacter
        {
            get { return targetCharacter; }
            set
            {
                targetCharacter = value;
                OnPropertyChanged("TargetCharacter");
                OnPropertyChanged("ShouldShowAvatar");
            }
        }

        public bool ShouldShowAvatar
        {
            get { return targetCharacter != null; }
        }

        public string Title
        {
            get { return title; }
            private set
            {
                title = value;
                OnPropertyChanged("Title");
            }
        }

        #endregion

        #region Public Methods and Operators

        public void HideNotifications()
        {
            Dispatcher.Invoke(
                (Action) delegate
                    {
                        view.OnHideCommand();
                        hideDelay.Stop();
                    });
        }

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

        public void ShowNotifications()
        {
            hideDelay.Stop();
            if (view == null)
                view = new NotificationsView(this);

            Dispatcher.Invoke((Action) (() => view.OnShowCommand()));
            hideDelay.Start();
        }

        public void UpdateNotification(string newContent)
        {
            var split = newContent.Split(splitChars,2);
            Title = split[0];
            Content = split[1];
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