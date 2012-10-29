using System;
using System.Linq;
using System.Media;
using System.Web;
using System.Windows;
using System.Windows.Threading;
using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using ViewModels;
using System.Collections.Generic;

namespace Services
{
    /// <summary>
    /// This handles pushing and creating all notifications. This means it plays all sounds, creates all toast notifications,
    /// and is responsible for managing the little tray icon. Additionally, it manages the singleton instance of the notifications class.
    /// 
    /// It responds to NewMessageEvent, NewPMEvent, NewUpdateEvent
    /// </summary>
    public class NotificationsDaemon : DispatcherObject, IDisposable
    {
        #region Fields
        IUnityContainer _contain;
        IEventAggregator _events;
        IChatModel _cm;
        SoundPlayer DingLing = new SoundPlayer(Environment.CurrentDirectory + @"\sounds\" + "newmessage.wav");
        DateTime _lastDingLinged;
        System.Windows.Forms.NotifyIcon icon = new System.Windows.Forms.NotifyIcon();
        ToastNotificationsViewModel toast;
        #endregion

        #region constructors
        public NotificationsDaemon(IUnityContainer contain, IEventAggregator eventagg, IChatModel cm)
        {
            _contain = contain;
            _events = eventagg;
            _cm = cm;
            toast = new ToastNotificationsViewModel(_events);

            _events.GetEvent<NewMessageEvent>().Subscribe(HandleNewChannelMessage, true);
            _events.GetEvent<NewPMEvent>().Subscribe(HandleNewMessage, true);
            _events.GetEvent<NewUpdateEvent>().Subscribe(HandleNotification, true);
            _events.GetEvent<LoginEvent>().Subscribe(
                args =>
                {
                    Application.Current.MainWindow.Closing += (s, e) =>
                        {
                            e.Cancel = true;
                            HideWindow();
                        };

                    #region some notifications based on mainwindow focus
                    Application.Current.MainWindow.MouseLeave += (s, e) =>
                    {
                        _events.GetEvent<ErrorEvent>().Publish(null);
                    };

                    _cm.SelectedChannelChanged += (s, e) =>
                    {
                        _events.GetEvent<ErrorEvent>().Publish(null);
                    };

                    icon.Visible = true;
                    #endregion
                });

            #region Icon Init
            icon.Icon = new System.Drawing.Icon(Environment.CurrentDirectory + @"\icons\catIcon.ico");
            icon.DoubleClick += (s, e) =>
                {
                    ShowWindow();
                };

            icon.BalloonTipClicked += (s, e) =>
                {
                    slimCat.Properties.Settings.Default.ShowStillRunning = false;
                    slimCat.Properties.Settings.Default.Save();
                };

            var iconMenu = new System.Windows.Forms.ContextMenu();
            iconMenu.MenuItems.Add("Show", (s, e) => ShowWindow());
            iconMenu.MenuItems.Add("Exit", (s, e) => ShutDown());

            icon.Text = "slimCat";
            icon.ContextMenu = iconMenu;
            #endregion
        }
        #endregion

        #region Methods
        private void NotifyUser(bool bingLing = false, bool flashWindow = false, string message = null, string target = null)
        {
            Dispatcher.Invoke(
                (Action)delegate
                {
                    if (!Application.Current.MainWindow.IsFocused)
                    {
                        if (flashWindow)
                            Application.Current.MainWindow.FlashWindow();
                        if (bingLing)
                            DingTheCrapOutOfTheUser();
                    }

                    if (message != null)
                        toast.UpdateNotification(message);
                    toast.Target = target;
                });
        }

        private void HandleNewMessage(IMessage Message)
        {
            var poster = Message.Poster;
            var channel = _cm.CurrentPMs.FirstByIdOrDefault(Message.Poster.Name);

            if (channel != null)
            {
                if (!channel.IsSelected && channel.Settings.ShouldDing)
                {
                    NotifyUser(true, true, poster.Name + '\n' + HttpUtility.UrlDecode(Message.Message), poster.Name);
                    return;
                }
            }

            Dispatcher.Invoke(
                (Action)delegate
                {
                    if (!Application.Current.MainWindow.IsActive && channel.Settings.ShouldDing)
                        NotifyUser(true, true, poster.Name + '\n' + HttpUtility.UrlDecode(Message.Message), poster.Name);
                });
        }

        private void HandleNewChannelMessage(IDictionary<string, object> update)
        {
            var channel = update["channel"] as GeneralChannelModel;
            var message = update["message"] as IMessage;

            if (channel != null)
            {
                if (!channel.IsSelected && channel.Settings.ShouldDing)
                {
                    NotifyUser(true, true, message.Poster.Name + '\n' + HttpUtility.UrlDecode(message.Message), channel.ID);
                    return;
                }
            }

            Dispatcher.Invoke(
                (Action)delegate
                {
                    if (!Application.Current.MainWindow.IsActive && channel.Settings.ShouldDing)
                        NotifyUser(true, true, message.Poster.Name + '\n' + HttpUtility.UrlDecode(message.Message), channel.ID);
                });
        }

        private void HandleNotification(NotificationModel Notification)
        {
            if (Notification is CharacterUpdateModel)
            {
                var targetCharacter = ((CharacterUpdateModel)Notification).TargetCharacter.Name;

                if (_cm.IsOfInterest(targetCharacter))
                {
                    AddNotification(Notification);
                    NotifyUser(false, false, Notification.ToString(), targetCharacter);
                }
            }

            else
            {
                var channel = (ChannelUpdateModel)Notification;
                var args = channel.Arguments;
                if (args is Models.ChannelUpdateModel.ChannelInviteEventArgs)
                    NotifyUser(false, false, Notification.ToString(), channel.ChannelID);

                AddNotification(Notification);
            }
        }

        /// <summary>
        /// Adds the notification to the notifications collection
        /// </summary>
        private void AddNotification(NotificationModel Notification)
        {
            Dispatcher.Invoke(
                (Action)delegate
                {
                    _cm.Notifications.Add(Notification);
                });
        }

        /// <summary>
        /// Sends a notification like an error, forcing the user's attention to it
        /// </summary>
        private void PushNotification(NotificationModel Notification)
        {
            _events.GetEvent<ErrorEvent>().Publish(Notification.ToString());
        }

        private void HideWindow()
        {
            App.Current.MainWindow.Hide();
            icon.Visible = true;
            if (slimCat.Properties.Settings.Default.ShowStillRunning)
                icon.ShowBalloonTip(5,
                    "slimCat", "slimCat is still running in the background."
                    +"\nClick on this to silence this notification (forever and ever).",
                    System.Windows.Forms.ToolTipIcon.Info);
        }

        public void ShowWindow()
        {
            App.Current.MainWindow.Show();
        }

        public void ShutDown()
        {
            icon.Dispose();
            Dispatcher.InvokeShutdown();
        }

        private void DingTheCrapOutOfTheUser()
        {
            if ((DateTime.Now - _lastDingLinged) > TimeSpan.FromSeconds(1))
            {
                DingLing.Play();
                _lastDingLinged = DateTime.Now;
            }
            
        }
        #endregion

        #region IDispose
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool IsManagedDispose)
        {
            if (IsManagedDispose)
            {
                icon.Dispose();
                DingLing.Dispose();
                toast.Dispose();
            }
        }
        #endregion
    }
}
