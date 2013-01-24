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
        private double _soundSaveVolume = 0.0;
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

            _events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(
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
                    #endregion

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

                    iconMenu.MenuItems.Add(new System.Windows.Forms.MenuItem(string.Format("slimCat Caracal ({0})", args)) { Enabled = false });
                    iconMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("-"));

                    iconMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Sounds Enabled", ToggleSound) { Checked = ApplicationSettings.Volume > 0.0, });
                    iconMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Toats Enabled", ToggleToast) { Checked = ApplicationSettings.ShowNotificationsGlobal });
                    iconMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("-"));

                    iconMenu.MenuItems.Add("Show", (s, e) => ShowWindow());
                    iconMenu.MenuItems.Add("Exit", (s, e) => ShutDown());

                    icon.Text = "slimCat - " + args;
                    icon.ContextMenu = iconMenu;
                    icon.Visible = true;
                    #endregion
                });
        }
        #endregion

        #region Methods
        private void NotifyUser(bool bingLing = false, bool flashWindow = false, string message = null, string target = null)
        {
            if (!ApplicationSettings.ShowNotificationsGlobal) return;

            Dispatcher.Invoke(
                (Action)delegate
                {
                    if (flashWindow && !WindowIsFocused)
                        Application.Current.MainWindow.FlashWindow();

                    if (bingLing)
                        DingTheCrapOutOfTheUser();

                    if (message != null)
                        toast.UpdateNotification(message);

                    toast.Target = target;
                });
        }

        /// <summary>
        /// Notification logic for new PM messages
        /// </summary>
        private void HandleNewMessage(IMessage Message)
        {
            var poster = Message.Poster;
            var channel = _cm.CurrentPMs.FirstByIdOrDefault(Message.Poster.Name);
            if (channel == null) return;

            if (channel.IsSelected && WindowIsFocused)
                return;

            switch ((Models.ChannelSettingsModel.NotifyLevel)channel.Settings.MessageNotifyLevel)
            {
                case ChannelSettingsModel.NotifyLevel.NotificationAndToast:
                    NotifyUser(false, false, poster.Name + '\n' + HttpUtility.HtmlDecode(Message.Message), poster.Name);
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndSound:
                    NotifyUser(true, true, poster.Name + '\n' + HttpUtility.HtmlDecode(Message.Message), poster.Name);
                    return;

                default: return;
            }
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

            if (ApplicationSettings.NotInterested.Contains(message.Poster.Name))
                return;
            #endregion

            // now we check to see if we should notify because of settings
            if (channel.Settings.MessageNotifyLevel > (int)Models.ChannelSettingsModel.NotifyLevel.NotificationOnly)
            {
                bool dingLing = channel.Settings.MessageNotifyLevel > (int)Models.ChannelSettingsModel.NotifyLevel.NotificationAndToast;
                NotifyUser(dingLing, dingLing, message.Poster.Name + '\n' + cleanMessageText, channel.ID);
                return; // and if we do, there is no need to evalutae further
            }

            if (channel.Settings.EnumerableTerms.Count() == 0 && ApplicationSettings.GlobalNotifyTermsList.Count() == 0)
                return; // if we don't have anything to check for, no need to evaluate further

            #region Ding Word evaluation
            // We have something to check for

            // Tokenized List is the list of terms the message has
            // Check against is a combined set of terms that the user has identified as ding words
            // Is Matching String uses Check against to see if any terms are a match

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

            // Now our character's name is always added
            {
                var name = _cm.SelectedCharacter.Name.ToLower();

                temp.Add(name); // fixes an issue where a user's name would ding constantly
                if (name.Last() != 's' && name.Last() != 'z')
                    temp.Add(name + @"'s"); // possessive fix
                else
                    temp.Add(name + @"'");

                checkAgainst = temp.Distinct(StringComparer.OrdinalIgnoreCase);
            }

            // check the message content
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
            // character update models will be *most* of the notification the user will see
            if (Notification is CharacterUpdateModel)
            {
                var targetCharacter = ((CharacterUpdateModel)Notification).TargetCharacter.Name;
                var args = ((CharacterUpdateModel)Notification).Arguments;

                // handle if the notification involves a character being promoted or demoted
                if (args is Models.CharacterUpdateModel.PromoteDemoteEventArgs)
                {
                    var channelID = ((Models.CharacterUpdateModel.PromoteDemoteEventArgs)args).TargetChannelID; // find by ID, not name
                    var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelID);

                    if (channel == null) return;

                    if (channel.Settings.PromoteDemoteNotifyOnlyForInteresting)
                        if (!_cm.IsOfInterest(targetCharacter)) return; // if we only want to know interesting people, no need to evalute further

                    convertNotificationLevelToAction(channel.Settings.PromoteDemoteNotifyLevel, channelID, Notification);
                }

                // handle if the notification involves a character joining or leaving
                else if (args is Models.CharacterUpdateModel.JoinLeaveEventArgs) // special check for this as it has settings per channel
                {
                    var target = ((Models.CharacterUpdateModel.JoinLeaveEventArgs)args).TargetChannelID; // find by ID, not name
                    var channel = _cm.CurrentChannels.FirstByIdOrDefault(target);

                    if (channel == null) return;

                    if (channel.Settings.JoinLeaveNotifyOnlyForInteresting)
                        if (!_cm.IsOfInterest(targetCharacter)) return;

                    convertNotificationLevelToAction(channel.Settings.JoinLeaveNotifyLevel, target, Notification);
                }

                // handle if the notification is an RTB event like a note or a new comment reply
                else if (args is Models.CharacterUpdateModel.NoteEventArgs || args is Models.CharacterUpdateModel.CommentEventArgs)
                {
                    AddNotification(Notification);


                    var link = (args is Models.CharacterUpdateModel.NoteEventArgs ?
                                    ((Models.CharacterUpdateModel.NoteEventArgs)args).Link
                                    : ((Models.CharacterUpdateModel.CommentEventArgs)args).Link);

                    NotifyUser(false, false, Notification.ToString(), link);
                }

                // handle if the notification is something like them being added to our interested/not list
                else if (args is Models.CharacterUpdateModel.ListChangedEventArgs)
                {
                    AddNotification(Notification);
                    NotifyUser(false, false, Notification.ToString(), targetCharacter);
                }

                // finally, if nothing else, add their update if we're interested in them in some way
                else if (_cm.IsOfInterest(targetCharacter))
                {
                    AddNotification(Notification);

                    if (_cm.SelectedChannel is PMChannelModel)
                        if ((_cm.SelectedChannel as PMChannelModel).ID.Equals(targetCharacter, StringComparison.OrdinalIgnoreCase))
                            return; // don't make a toast if we have their tab focused as it is redundant

                    NotifyUser(false, false, Notification.ToString(), targetCharacter);
                }
            }

            // the only other kind of update model is a channel update model
            else
            {
                var channelID = ((ChannelUpdateModel)Notification).ChannelID;
                var channel = _cm.CurrentChannels.FirstByIdOrDefault(channelID);
                var args = ((ChannelUpdateModel)Notification).Arguments;

                if (channel == null) return; // avoid null reference

                if (args is Models.ChannelUpdateModel.ChannelInviteEventArgs) // we always want to know about invites
                    NotifyUser(false, false, Notification.ToString(), channelID);

                AddNotification(Notification);
            }
        }

        private void convertNotificationLevelToAction(int NotificationLevel, string ActionID, NotificationModel Notification)
        {
            switch ((Models.ChannelSettingsModel.NotifyLevel)NotificationLevel) // convert our int into an enum to avoid magic numbers
            {
                case ChannelSettingsModel.NotifyLevel.NoNotification: return;

                case ChannelSettingsModel.NotifyLevel.NotificationOnly:
                    AddNotification(Notification); 
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndToast:
                    AddNotification(Notification);
                    NotifyUser(false, false, Notification.ToString(), ActionID);
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndSound:
                    AddNotification(Notification);
                    NotifyUser(true, true, Notification.ToString(), ActionID);
                    return;
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

        private void ToggleToast(object sender, EventArgs e)
        {
            ApplicationSettings.ShowNotificationsGlobal = !ApplicationSettings.ShowNotificationsGlobal;
            icon.ContextMenu.MenuItems[3].Checked = ApplicationSettings.ShowNotificationsGlobal;
        }

        private void ToggleSound(object sender, EventArgs e)
        {
            var temp = ApplicationSettings.Volume;
            ApplicationSettings.Volume = _soundSaveVolume;
            _soundSaveVolume = temp;

            icon.ContextMenu.MenuItems[2].Checked = ApplicationSettings.Volume > 0.0;
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
