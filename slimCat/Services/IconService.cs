#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationService.cs">
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
    using Properties;
    using Utilities;
    using Application = System.Windows.Application;
    #endregion

    public class IconService : DispatcherObject, IDisposable, IIconService
    {
        #region Fields
        private IEventAggregator events;

        private IChatModel cm;

        private readonly NotifyIcon icon = new NotifyIcon();

        public event EventHandler SettingsChanged;
        #endregion

        #region Constructors
        public IconService(IEventAggregator eventagg, IChatModel chatModel)
        {
            events = eventagg;
            cm = chatModel;

            Application.Current.MainWindow.Closing += (s, e) =>
            {
                if (!ApplicationSettings.AllowMinimizeToTray) return;

                e.Cancel = true;
                HideWindow();
            };

            Application.Current.MainWindow.MouseLeave += (s, e) => events.GetEvent<ErrorEvent>().Publish(null);

            cm.SelectedChannelChanged += (s, e) => events.GetEvent<ErrorEvent>().Publish(null);
            BuildIcon();
        }
        #endregion

        #region Public Methods
        public void ToggleSound()
        {
            ApplicationSettings.AllowSound = !ApplicationSettings.AllowSound;
            AllowSound.Checked = ApplicationSettings.AllowSound;

            if (SettingsChanged != null)
            {
                SettingsChanged(this, new EventArgs());
            }
        }

        public void ToggleToasts()
        {
            ApplicationSettings.ShowNotificationsGlobal = !ApplicationSettings.ShowNotificationsGlobal;
            AllowToast.Checked = ApplicationSettings.ShowNotificationsGlobal;

            if (SettingsChanged != null)
            {
                SettingsChanged(this, new EventArgs());
            }
        }

        public void ShutDown()
        {
            icon.Dispose();
            Dispatcher.InvokeShutdown();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool isManagedDispose)
        {
            if (!isManagedDispose)
                return;

            icon.Dispose();
        }
        #endregion

        #region Methods
        private void BuildIcon()
        {
            icon.Icon = new Icon(Environment.CurrentDirectory + @"\icons\catIcon.ico");
            icon.DoubleClick += (s, e) => ShowWindow();

            icon.BalloonTipClicked += (s, e) =>
            {
                Settings.Default.ShowStillRunning = false;
                Settings.Default.Save();
            };

            var iconMenu = new ContextMenu();

            iconMenu.MenuItems.Add(
                new MenuItem(
                    string.Format(
                        "{0} {1} ({2}) - {3}",
                        Constants.ClientId,
                        Constants.ClientName,
                        Constants.ClientVer,
                        cm.CurrentCharacter.Name))
            {
                Enabled = false
            });

            iconMenu.MenuItems.Add(new MenuItem("-"));

            iconMenu.MenuItems.Add(new MenuItem("Sounds Enabled", ToggleSound) { Checked = ApplicationSettings.AllowSound });
            iconMenu.MenuItems.Add(new MenuItem("Toasts Enabled", ToggleToasts) { Checked = ApplicationSettings.ShowNotificationsGlobal });
            iconMenu.MenuItems.Add(new MenuItem("-"));

            iconMenu.MenuItems.Add("Show", (s, e) => ShowWindow());
            iconMenu.MenuItems.Add("Exit", (s, e) => ShutDown());

            icon.Text = string.Format("{0} - {1}", Constants.ClientId, cm.CurrentCharacter.Name);
            icon.ContextMenu = iconMenu;
            icon.Visible = true;
        }

        private MenuItem AllowSound
        {
            get { return icon.ContextMenu.MenuItems[2]; }
        }

        private MenuItem AllowToast
        {
            get { return icon.ContextMenu.MenuItems[3]; }
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
            if (Settings.Default.ShowStillRunning)
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

        event EventHandler SettingsChanged;
    }
}
