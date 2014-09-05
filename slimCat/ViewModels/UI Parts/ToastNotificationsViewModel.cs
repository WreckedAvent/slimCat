#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ToastNotificationsViewModel.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//  
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Timers;
    using System.Web;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     A light-weight viewmodel for toast notifications
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
        private RelayCommand link;

        private RelayCommand snap;

        private ICharacter targetCharacter;

        private string title;
        private NotificationsView view;

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

        public ICommand NavigateTo
        {
            get { return link ?? (link = new RelayCommand(StartLinkInDefaultBrowser)); }
        }

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
            if (newContent.Contains('\n'))
            {
                var split = newContent.Split(splitChars, 2);
                Title = split[0];
                Content = split[1];
                Logging.LogLine("Showing toast \"{0}\"".FormatWith(title), "toast vm");
            }
            else
            {
                Content = newContent;
                Title = "slimCat notification";
                Logging.LogLine("Showing toast", "toast vm");
            }

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

        private void StartLinkInDefaultBrowser(object linkToOpen)
        {
            var interpret = linkToOpen as string;
            if (string.IsNullOrEmpty(interpret)) return;

            if (!interpret.Contains(".") || interpret.Contains(" "))
            {
                if (interpret.EndsWith("/notes"))
                {
                    events.SendUserCommand("priv", new[] {interpret});
                    return;
                }

                if (!ApplicationSettings.OpenProfilesInClient)
                {
                    Process.Start(Constants.UrlConstants.CharacterPage + HttpUtility.HtmlEncode(interpret));
                    return;
                }

                events.SendUserCommand("priv", new[] {interpret + "/profile"});
                return;
            }

            Process.Start(interpret);
        }

        #endregion
    }
}