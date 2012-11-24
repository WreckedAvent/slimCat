using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Unity;
using Models;
using slimCat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ViewModels;

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
        MediaPlayer DingLing = new MediaPlayer();
        DateTime _lastDingLinged;
        System.Windows.Forms.NotifyIcon icon = new System.Windows.Forms.NotifyIcon();
        ToastNotificationsViewModel toast;
        bool _windowHasFocus;
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
            _events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(
                args => icon.Text = "slimCat - " + args);

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
                    if (!Application.Current.MainWindow.IsActive)
                    {
                        if (flashWindow)
                            Application.Current.MainWindow.FlashWindow();
                        if (bingLing)
                            DingTheCrapOutOfTheUser();
                    }

                    if (ApplicationSettings.ShowNotificationsGlobal)
                    {
                        if (message != null)
                            toast.UpdateNotification(message);
                        toast.Target = target;
                    }
                });
        }

        /// <summary>
        /// Notification logic for new PM messages
        /// </summary>
        private void HandleNewMessage(IMessage Message)
        {
            var poster = Message.Poster;
            var channel = _cm.CurrentPMs.FirstByIdOrDefault(Message.Poster.Name);

            if (channel != null)
            {
                if (!channel.IsSelected && channel.Settings.ShouldDing)
                {
                    NotifyUser(true, true, poster.Name + '\n' + HttpUtility.HtmlDecode(Message.Message), poster.Name);
                    return;
                }
            }

            if (!WindowIsFocused && channel.Settings.ShouldDing)
                NotifyUser(true, true, poster.Name + '\n' + HttpUtility.HtmlDecode(Message.Message), poster.Name);
        }

        /// <summary>
        /// Notification logic for new channel ads and messages
        /// </summary>
        private void HandleNewChannelMessage(IDictionary<string, object> update)
        {
            #region init
            var channel = update["channel"] as GeneralChannelModel;
            var message = update["message"] as IMessage;
            var cleanMessageText = HttpUtility.HtmlDecode(message.Message);

            var temp = new List<string>(channel.Settings.EnumerableTerms);
            foreach (var term in ApplicationSettings.GlobalNotifyTermsList)
                temp.Add(term); // get our combined list of terms

            var checkAgainst = temp.Distinct(StringComparer.OrdinalIgnoreCase);
            #endregion

            #region Early terminate logic
            // if any of these conditions hold true we have no reason to evaluate further
            if (channel == null)
                return;

            if (channel.IsSelected && WindowIsFocused)
                return;

            if (channel.Settings.IgnoreNotInterested && ApplicationSettings.NotInterested.Contains(message.Poster.Name))
                return; 
            #endregion

            if (channel.Settings.ShouldDing) // if we told to ding on new message, no need to evaluate further
            {
                NotifyUser(true, true, message.Poster.Name + '\n' + cleanMessageText, channel.ID);
                return; // return simplifies application logic down below
            }

            if (channel.Settings.EnumerableTerms.Count() == 0
                    && channel.Settings.NotifyCharacterMention
                    && ApplicationSettings.GlobalNotifyTermsList.Count() == 0)
                return; // if we don't have anything to check for, no need to evaluate further

            #region Ding Word evaluation
            // We have something to check for

            // Tokenized List is the list of terms the message has
            // Check against is a combined set of terms that the user has identified as ding words
            // Is Matching String uses Check against to see if any terms are a match

            // TODO: refactor this bit to keep to DRY
            // check if the message's poster meets any check terms
            if (channel.Settings.NotifyIncludesCharacterNames)
            {
                // if the poster's name contains a ding word
                Tuple<string, string> match = null;
                foreach (var dingword in checkAgainst)
                {
                    var attemptedMatch = message.Poster.Name.FirstMatch(dingword);
                    if (!string.IsNullOrWhiteSpace(attemptedMatch.Item1))
                    {
                        match = attemptedMatch;
                        break;
                    }
                }

                if (match != null) 
                {
                    var notifyMessage = string.Format("{0}'s name matches {1}:\n{2}", message.Poster.Name, match.Item1, match.Item2);

                    NotifyUser(true, true, notifyMessage, channel.ID);
                    channel.FlashTab();
                    return;
                }
            }

            if (channel.Settings.NotifyCharacterMention)
                temp.Add(_cm.SelectedCharacter.Name.ToLower()); // fixes an issue where a user's name would ding constantly

            checkAgainst = temp.Distinct(StringComparer.OrdinalIgnoreCase);

            // check if we need to look in the message itself
            if (channel.Settings.NotifyIncludesMessages)
            {
                Tuple<string, string> match = null;

                foreach (var dingWord in checkAgainst)
                {
                    var attemptedMatch = cleanMessageText.FirstMatch(dingWord);
                    if (!string.IsNullOrWhiteSpace(attemptedMatch.Item1)) // if it didn't return empty it found a match
                    {
                        match = attemptedMatch;
                        break;
                    }
                }

                if (match != null) // if one of our words is a dingling word
                {
                    var notifyMessage = string.Format("{0} mentioned {1}:\n{2}", message.Poster.Name, match.Item1, match.Item2);

                    NotifyUser(true, true, notifyMessage, channel.ID);
                    channel.FlashTab();
                    return;
                }
            }
            #endregion
        }

        private void HandleNotification(NotificationModel Notification)
        {
            if (Notification is CharacterUpdateModel)
            {
                var targetCharacter = ((CharacterUpdateModel)Notification).TargetCharacter.Name;
                var args = ((CharacterUpdateModel)Notification).Arguments;

                if (args is Models.CharacterUpdateModel.PromoteDemoteEventArgs)
                {
                    var channelID = (args as Models.CharacterUpdateModel.PromoteDemoteEventArgs).TargetChannelID; // find by ID, not name
                    var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelID);

                    if (channel != null)
                    {
                        if (channel.Settings.NotifyModPromoteDemote)
                            AddNotification(Notification);
                    }
                }

                if (args is Models.CharacterUpdateModel.JoinLeaveEventArgs) // special check for this as it has settings per channel
                {
                    var target = (args as Models.CharacterUpdateModel.JoinLeaveEventArgs).TargetChannelID; // find by ID, not name
                    var chan = _cm.CurrentChannels.FirstByIdOrDefault(target);

                    if (chan != null) // avoid null references
                    {
                        if (!chan.Settings.NotifyOnJoinLeave) // if we don't want any join/leave notifications, just return
                            return;
                        if (!_cm.IsOfInterest(targetCharacter) && !chan.Settings.NotifyOnNormalJoinLeave) // if we want them and they don't apply, return
                            return;
                        // otherwise, fall through and add the notification as normal
                    }
                    else
                        return; // adding a notification to a null channel would be bad

                    AddNotification(Notification);
                }

                else if (_cm.IsOfInterest(targetCharacter))
                {
                    AddNotification(Notification);

                    if (_cm.SelectedChannel is PMChannelModel)
                        if ((_cm.SelectedChannel as PMChannelModel).ID.Equals(targetCharacter, StringComparison.OrdinalIgnoreCase))
                            return; // don't make a toast if we have their tab focused as it is redundant

                    NotifyUser(false, false, Notification.ToString(), targetCharacter);
                }
            }

            else
            {
                var channel = (ChannelUpdateModel)Notification;
                var args = channel.Arguments;

                if (args is Models.ChannelUpdateModel.ChannelInviteEventArgs) // we always want to know about invites
                    NotifyUser(false, false, Notification.ToString(), channel.ChannelID);

                AddNotification(Notification); // if we got this far, it must be OK to add it to our collection
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
            // this will ensure the window is showed no matter of its state.
            Application.Current.MainWindow.Show();
            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            Application.Current.MainWindow.Focus();
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
                ResetDingLing();
                DingLing.Volume = ApplicationSettings.Volume;

                if (DingLing.Volume != 0.0)
                {
                    DingLing.Play();
                    _lastDingLinged = DateTime.Now;
                }
            }
            
        }

        private void ResetDingLing()
        {
            DingLing.Close();
            DingLing.Open(new Uri(Environment.CurrentDirectory + @"\sounds\" + "newmessage.wav"));
        }

        private bool WindowIsFocused
        {
            get
            {
                Dispatcher.Invoke(
                    (Action)delegate
                    {
                        _windowHasFocus = Application.Current.MainWindow.IsActive;
                    });

                return _windowHasFocus;
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
                DingLing.Close();
                DingLing = null;
                toast.Dispose();
            }
        }
        #endregion
    }
}
