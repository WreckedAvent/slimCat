#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IconService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Threading;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Utilities;
    using Application = System.Windows.Application;

    #endregion

    public class IconService : DispatcherObject, IDisposable, IIconService
    {
        #region Fields

        private readonly IChatModel cm;
        private readonly IEventAggregator events;

        private readonly NotifyIcon icon = new NotifyIcon();
        private readonly Icon catIcon;
        private readonly Icon balloonIcon;
        private bool hasNotification; 

        #endregion

        #region Constructors

        public IconService(IEventAggregator eventagg, IChatModel chatModel)
        {
            events = eventagg;
            cm = chatModel;

            eventagg.GetEvent<CharacterSelectedLoginEvent>().Subscribe(OnCharacterSelected);
            eventagg.GetEvent<LoginAuthenticatedEvent>().Subscribe(OnLoginAuthenticated);
            catIcon = new Icon(Environment.CurrentDirectory + @"\icons\catIcon.ico");
            balloonIcon = new Icon(Environment.CurrentDirectory + @"\icons\balloonIcon.ico");
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            Dispose(true);
        }

        public void ToggleSound()
        {
            ApplicationSettings.AllowSound = !ApplicationSettings.AllowSound;
            AllowSoundUpdate();
        }

        public void ToggleToasts()
        {
            ApplicationSettings.ShowNotificationsGlobal = !ApplicationSettings.ShowNotificationsGlobal;
            AllowToastUpdate();
        }


        public void SetIconNotificationLevel(bool newMsgs)
        {
            if (newMsgs & !hasNotification)
            {
                icon.Icon = balloonIcon;
                hasNotification = true;
            }
            else if (!newMsgs & hasNotification)
            {
                icon.Icon = catIcon;
                hasNotification = false;
            }

        }

        public void ShutDown()
        {
            icon.Dispose();
            catIcon.Dispose();
            balloonIcon.Dispose(); 
            Dispatcher.InvokeShutdown();
        }

        public void Dispose(bool isManagedDispose)
        {
            if (!isManagedDispose)
                return;

            icon.Dispose();
            catIcon.Dispose();
            balloonIcon.Dispose(); 
        }

        #endregion

        #region Methods

        public void AllowSoundUpdate()
        {
            icon.ContextMenu.MenuItems[2].Checked = ApplicationSettings.AllowSound;
        }

        public void AllowToastUpdate()
        {
            icon.ContextMenu.MenuItems[3].Checked = ApplicationSettings.ShowNotificationsGlobal;
        }

        private void BuildIcon(string character)
        {

            icon.Icon = catIcon; 
            icon.DoubleClick += (s, e) => ShowWindow();

            icon.BalloonTipClicked += (s, e) =>
            {
                SettingsService.Preferences.ShowStillRunning = false;
                SettingsService.Preferences = SettingsService.Preferences;
            };

            var iconMenu = new ContextMenu();

            iconMenu.MenuItems.Add(
                new MenuItem(
                    string.Format(
                        "{0} {1} ({2}) - {3}",
                        Constants.ClientId,
                        Constants.ClientName,
                        Constants.ClientVer,
                        character))
                {
                    Enabled = false
                });

            iconMenu.MenuItems.Add(new MenuItem("-"));

            iconMenu.MenuItems.Add(new MenuItem("Sounds Enabled", ToggleSound)
            {
                Checked = ApplicationSettings.AllowSound
            });
            iconMenu.MenuItems.Add(new MenuItem("Toasts Enabled", ToggleToasts)
            {
                Checked = ApplicationSettings.ShowNotificationsGlobal
            });
            iconMenu.MenuItems.Add(new MenuItem("-"));

            iconMenu.MenuItems.Add("Show", (s, e) => ShowWindow());
            iconMenu.MenuItems.Add("Exit", (s, e) => ShutDown());

            icon.Text = string.Format("{0} - {1}", Constants.ClientId, character);
            icon.ContextMenu = iconMenu;
            icon.Visible = true;
        }

        private void OnCharacterSelected(string character)
        {
            Application.Current.MainWindow.Closing += (s, e) =>
            {
                if (!ApplicationSettings.AllowMinimizeToTray) return;

                e.Cancel = true;
                HideWindow();
            };

            cm.SelectedChannelChanged += (s, e) => events.GetEvent<ErrorEvent>().Publish(null);

            BuildIcon(character);
        }

        private void OnLoginAuthenticated(bool? obj)
        {
            AllowSoundUpdate();
            AllowToastUpdate();
        }

        private void ToggleSound(object sender, EventArgs e)
        {
            ToggleSound();
        }

        private void ToggleToasts(object sender, EventArgs e)
        {
            ToggleToasts();
        }

        private void HideWindow()
        {
            Application.Current.MainWindow.Hide();
            icon.Visible = true;
            if (SettingsService.Preferences.ShowStillRunning)
            {
                icon.ShowBalloonTip(
                    5,
                    "slimCat",
                    "slimCat is still running in the background." +
                    "\nClick on this to silence this notification (forever and ever).",
                    ToolTipIcon.Info);
            }
        }

        private static void ShowWindow()
        {
            Application.Current.MainWindow.Show();
            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                Application.Current.MainWindow.WindowState = WindowState.Normal;

            Application.Current.MainWindow.Activate();
        }

        #endregion
    }

    public interface IIconService
    {
        void ToggleSound();
        void ToggleToasts();
        void SetIconNotificationLevel(bool newMsg);
    }
}