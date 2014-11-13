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
    using System.Timers;
    using System.Web;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     A light-weight viewmodel for toast notifications
    /// </summary>
    public sealed class ToastNotificationsViewModel : SysProp
    {
        private readonly IChatState chatState;

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

        public ToastNotificationsViewModel(IChatState chatState)
        {
            this.chatState = chatState;
            hideDelay.Elapsed += (s, e) => HideNotifications();
            events = chatState.EventAggregator;
        }

        #endregion

        #region Public Properties

        public string Content
        {
            get { return content; }

            set
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

        public ICommand SnapToLatestCommand
        {
            get { return snap ?? (snap = new RelayCommand(OnSnapToLatestEvent)); }
        }

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

        public ICanNavigate Navigator { get; set; }

        public bool ShouldShowAvatar
        {
            get { return targetCharacter != null && ApplicationSettings.ShowAvatarsInToasts; }
        }

        public string Title
        {
            get { return title; }
            set
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
            HideNotifications();
            Navigator.Navigate(chatState);
        }

        public void ShowNotifications()
        {
            if (ApplicationSettings.DisallowNotificationsWhenDnd && chatState.ChatModel.CurrentCharacter.Status == StatusType.Dnd)
                return;

            if (!ApplicationSettings.ShowNotificationsGlobal)
                return;

            hideDelay.Stop();
            if (view == null)
                Dispatcher.Invoke((Action) (() => view = new NotificationsView(this)));

            Dispatcher.Invoke((Action) (() => view.OnShowCommand()));
            hideDelay.Start();
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