// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsDaemon.cs" company="Justin Kadrovach">
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
//   This handles pushing and creating all notifications. This means it plays all sounds, creates all toast notifications,
//   and is responsible for managing the little tray icon. Additionally, it manages the singleton instance of the notifications class.
//   It responds to NewMessageEvent, NewPMEvent, NewUpdateEvent
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Services
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Web;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media;
    using System.Windows.Threading;

    using lib;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Unity;

    using Models;

    using slimCat;
    using slimCat.Properties;

    using ViewModels;

    using Application = System.Windows.Application;

    /// <summary>
    ///     This handles pushing and creating all notifications. This means it plays all sounds, creates all toast notifications,
    ///     and is responsible for managing the little tray icon. Additionally, it manages the singleton instance of the notifications class.
    ///     It responds to NewMessageEvent, NewPMEvent, NewUpdateEvent
    /// </summary>
    public class NotificationsDaemon : DispatcherObject, IDisposable
    {
        #region Fields

        private readonly IChatModel _cm;

        private readonly IEventAggregator _events;

        private readonly NotifyIcon icon = new NotifyIcon();

        private readonly ToastNotificationsViewModel toast;

        private MediaPlayer DingLing = new MediaPlayer();

        private IUnityContainer _contain;

        private DateTime _lastDingLinged;

        private double _soundSaveVolume;

        private bool _windowHasFocus;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsDaemon"/> class.
        /// </summary>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="eventagg">
        /// The eventagg.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        public NotificationsDaemon(IUnityContainer contain, IEventAggregator eventagg, IChatModel cm)
        {
            this._contain = contain;
            this._events = eventagg;
            this._cm = cm;
            this.toast = new ToastNotificationsViewModel(this._events);

            this._events.GetEvent<NewMessageEvent>().Subscribe(this.HandleNewChannelMessage, true);
            this._events.GetEvent<NewPMEvent>().Subscribe(this.HandleNewMessage, true);
            this._events.GetEvent<NewUpdateEvent>().Subscribe(this.HandleNotification, true);

            this._events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(
                args =>
                    {
                        Application.Current.MainWindow.Closing += (s, e) =>
                            {
                                e.Cancel = true;
                                this.HideWindow();
                            };

                        Application.Current.MainWindow.MouseLeave +=
                            (s, e) => { this._events.GetEvent<ErrorEvent>().Publish(null); };

                        this._cm.SelectedChannelChanged +=
                            (s, e) => { this._events.GetEvent<ErrorEvent>().Publish(null); };

                        

                        this.icon.Icon = new Icon(Environment.CurrentDirectory + @"\icons\catIcon.ico");
                        this.icon.DoubleClick += (s, e) => { this.ShowWindow(); };

                        this.icon.BalloonTipClicked += (s, e) =>
                            {
                                Settings.Default.ShowStillRunning = false;
                                Settings.Default.Save();
                            };

                        var iconMenu = new ContextMenu();

                        iconMenu.MenuItems.Add(
                            new MenuItem(
                                string.Format(
                                    "{0} {1} ({2}) - {3}", 
                                    Constants.CLIENT_ID, 
                                    Constants.CLIENT_NAME, 
                                    Constants.CLIENT_VER, 
                                    args)) {
                                              Enabled = false 
                                           });
                        iconMenu.MenuItems.Add(new MenuItem("-"));

                        iconMenu.MenuItems.Add(
                            new MenuItem("Sounds Enabled", this.ToggleSound)
                                {
                                    Checked =
                                        ApplicationSettings.Volume > 0.0, 
                                });
                        iconMenu.MenuItems.Add(
                            new MenuItem("Toasts Enabled", this.ToggleToast)
                                {
                                    Checked =
                                        ApplicationSettings
                                        .ShowNotificationsGlobal
                                });
                        iconMenu.MenuItems.Add(new MenuItem("-"));

                        iconMenu.MenuItems.Add("Show", (s, e) => this.ShowWindow());
                        iconMenu.MenuItems.Add("Exit", (s, e) => this.ShutDown());

                        this.icon.Text = string.Format("{0} - {1}", Constants.CLIENT_ID, args);
                        this.icon.ContextMenu = iconMenu;
                        this.icon.Visible = true;

                        
                    });
        }

        #endregion

        #region Properties

        private bool WindowIsFocused
        {
            get
            {
                this.Dispatcher.Invoke(
                    (Action)delegate { this._windowHasFocus = Application.Current.MainWindow.IsActive; });

                return this._windowHasFocus;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        ///     The show window.
        /// </summary>
        public void ShowWindow()
        {
            // this will ensure the window is showed no matter of its state.
            Application.Current.MainWindow.Show();
            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }

            Application.Current.MainWindow.Focus();
        }

        /// <summary>
        ///     The shut down.
        /// </summary>
        public void ShutDown()
        {
            this.icon.Dispose();
            this.Dispatcher.InvokeShutdown();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="IsManagedDispose">
        /// The is managed dispose.
        /// </param>
        protected virtual void Dispose(bool IsManagedDispose)
        {
            if (IsManagedDispose)
            {
                this.icon.Dispose();
                this.DingLing.Close();
                this.DingLing = null;
                this.toast.Dispose();
            }
        }

        /// <summary>
        /// Adds the notification to the notifications collection
        /// </summary>
        /// <param name="Notification">
        /// The Notification.
        /// </param>
        private void AddNotification(NotificationModel Notification)
        {
            this.Dispatcher.Invoke((Action)delegate { this._cm.Notifications.Add(Notification); });
        }

        private void DingTheCrapOutOfTheUser()
        {
            if ((DateTime.Now - this._lastDingLinged) > TimeSpan.FromSeconds(1))
            {
                this.ResetDingLing();
                this.DingLing.Volume = ApplicationSettings.Volume;

                if (this.DingLing.Volume != 0.0)
                {
                    this.DingLing.Play();
                    this._lastDingLinged = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Notification logic for new channel ads and messages
        /// </summary>
        /// <param name="update">
        /// The update.
        /// </param>
        private void HandleNewChannelMessage(IDictionary<string, object> update)
        {
            var channel = update["channel"] as GeneralChannelModel;
            var message = update["message"] as IMessage;
            string cleanMessageText = HttpUtility.HtmlDecode(message.Message);

            var temp = new List<string>(channel.Settings.EnumerableTerms);
            foreach (string term in ApplicationSettings.GlobalNotifyTermsList)
            {
                temp.Add(term); // get our combined list of terms
            }

            IEnumerable<string> checkAgainst = temp.Distinct(StringComparer.OrdinalIgnoreCase);

            

            // if any of these conditions hold true we have no reason to evaluate further
            if (channel == null)
            {
                return;
            }

            if (channel.IsSelected && this.WindowIsFocused)
            {
                return;
            }

            if (ApplicationSettings.NotInterested.Contains(message.Poster.Name))
            {
                return;
            }

            

            // now we check to see if we should notify because of settings
            if (channel.Settings.MessageNotifyLevel > (int)ChannelSettingsModel.NotifyLevel.NotificationOnly)
            {
                bool dingLing = channel.Settings.MessageNotifyLevel
                                > (int)ChannelSettingsModel.NotifyLevel.NotificationAndToast;

                if ((channel.Settings.MessageNotifyOnlyForInteresting && this._cm.IsOfInterest(message.Poster.Name))
                    || !channel.Settings.MessageNotifyOnlyForInteresting)
                {
                    this.NotifyUser(dingLing, dingLing, message.Poster.Name + '\n' + cleanMessageText, channel.ID);
                    return; // and if we do, there is no need to evalutae further
                }
            }

            if (channel.Settings.EnumerableTerms.Count() == 0 && ApplicationSettings.GlobalNotifyTermsList.Count() == 0)
            {
                return; // if we don't have anything to check for, no need to evaluate further
            }

            #region Ding Word evaluation

            // We have something to check for

            // Tokenized List is the list of terms the message has
            // Check against is a combined set of terms that the user has identified as ding words
            // Is Matching String uses Check against to see if any terms are a match
            if (channel.Settings.NotifyIncludesCharacterNames)
            {
                // if the poster's name contains a ding word
                Tuple<string, string> match = null;
                foreach (string dingword in checkAgainst)
                {
                    Tuple<string, string> attemptedMatch = message.Poster.Name.FirstMatch(dingword);
                    if (!string.IsNullOrWhiteSpace(attemptedMatch.Item1))
                    {
                        match = attemptedMatch;
                        break;
                    }
                }

                if (match != null)
                {
                    string notifyMessage = string.Format(
                        "{0}'s name matches {1}:\n{2}", message.Poster.Name, match.Item1, match.Item2);

                    this.NotifyUser(true, true, notifyMessage, channel.ID);
                    channel.FlashTab();
                    return;
                }
            }
            {
                // Now our character's name is always added
                string name = this._cm.SelectedCharacter.Name.ToLower();

                temp.Add(name); // fixes an issue where a user's name would ding constantly
                if (name.Last() != 's' && name.Last() != 'z')
                {
                    temp.Add(name + @"'s"); // possessive fix
                }
                else
                {
                    temp.Add(name + @"'");
                }

                checkAgainst = temp.Distinct(StringComparer.OrdinalIgnoreCase);
            }
            {
                // check the message content
                Tuple<string, string> match = null;

                foreach (string dingWord in checkAgainst)
                {
                    Tuple<string, string> attemptedMatch = cleanMessageText.FirstMatch(dingWord);
                    if (!string.IsNullOrWhiteSpace(attemptedMatch.Item1))
                    {
                        // if it didn't return empty it found a match
                        match = attemptedMatch;
                        break;
                    }
                }

                if (match != null)
                {
                    // if one of our words is a dingling word
                    string notifyMessage = string.Format(
                        "{0} mentioned {1}:\n{2}", message.Poster.Name, match.Item1, match.Item2);

                    this.NotifyUser(true, true, notifyMessage, channel.ID);
                    channel.FlashTab();
                    return;
                }
            }

            #endregion
        }

        /// <summary>
        /// Notification logic for new PM messages
        /// </summary>
        /// <param name="Message">
        /// The Message.
        /// </param>
        private void HandleNewMessage(IMessage Message)
        {
            ICharacter poster = Message.Poster;
            PMChannelModel channel = this._cm.CurrentPMs.FirstByIdOrDefault(Message.Poster.Name);
            if (channel == null)
            {
                return;
            }

            if (channel.IsSelected && this.WindowIsFocused)
            {
                return;
            }

            switch ((ChannelSettingsModel.NotifyLevel)channel.Settings.MessageNotifyLevel)
            {
                case ChannelSettingsModel.NotifyLevel.NotificationAndToast:
                    this.NotifyUser(
                        false, false, poster.Name + '\n' + HttpUtility.HtmlDecode(Message.Message), poster.Name);
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndSound:
                    this.NotifyUser(
                        true, true, poster.Name + '\n' + HttpUtility.HtmlDecode(Message.Message), poster.Name);
                    return;

                default:
                    return;
            }
        }

        private void HandleNotification(NotificationModel Notification)
        {
            // character update models will be *most* of the notification the user will see
            if (Notification is CharacterUpdateModel)
            {
                string targetCharacter = ((CharacterUpdateModel)Notification).TargetCharacter.Name;
                CharacterUpdateModel.CharacterUpdateEventArgs args = ((CharacterUpdateModel)Notification).Arguments;

                // handle if the notification involves a character being promoted or demoted
                if (args is CharacterUpdateModel.PromoteDemoteEventArgs)
                {
                    string channelID = ((CharacterUpdateModel.PromoteDemoteEventArgs)args).TargetChannelID;

                    // find by ID, not name
                    GeneralChannelModel channel = this._cm.CurrentChannels.FirstByIdOrDefault(channelID);

                    if (channel == null)
                    {
                        return;
                    }

                    if (channel.Settings.PromoteDemoteNotifyOnlyForInteresting)
                    {
                        if (!this._cm.IsOfInterest(targetCharacter))
                        {
                            return; // if we only want to know interesting people, no need to evalute further
                        }
                    }

                    this.convertNotificationLevelToAction(
                        channel.Settings.PromoteDemoteNotifyLevel, channelID, Notification);
                }

                    // handle if the notification involves a character joining or leaving
                else if (args is CharacterUpdateModel.JoinLeaveEventArgs)
                {
                    // special check for this as it has settings per channel
                    string target = ((CharacterUpdateModel.JoinLeaveEventArgs)args).TargetChannelID;

                    // find by ID, not name
                    GeneralChannelModel channel = this._cm.CurrentChannels.FirstByIdOrDefault(target);

                    if (channel == null)
                    {
                        return;
                    }

                    if (channel.Settings.JoinLeaveNotifyOnlyForInteresting)
                    {
                        if (!this._cm.IsOfInterest(targetCharacter))
                        {
                            return;
                        }
                    }

                    this.convertNotificationLevelToAction(channel.Settings.JoinLeaveNotifyLevel, target, Notification);
                }

                    // handle if the notification is an RTB event like a note or a new comment reply
                else if (args is CharacterUpdateModel.NoteEventArgs || args is CharacterUpdateModel.CommentEventArgs)
                {
                    this.AddNotification(Notification);

                    string link = args is CharacterUpdateModel.NoteEventArgs
                                      ? ((CharacterUpdateModel.NoteEventArgs)args).Link
                                      : ((CharacterUpdateModel.CommentEventArgs)args).Link;

                    this.NotifyUser(false, false, Notification.ToString(), link);
                }

                    // handle if the notification is something like them being added to our interested/not list
                else if (args is CharacterUpdateModel.ListChangedEventArgs)
                {
                    this.AddNotification(Notification);
                    this.NotifyUser(false, false, Notification.ToString(), targetCharacter);
                }

                    // handle moderator events
                else if (args is CharacterUpdateModel.ReportHandledEventArgs)
                {
                    this.AddNotification(Notification);
                    this.NotifyUser(true, true, Notification.ToString(), targetCharacter);
                }
                else if (args is CharacterUpdateModel.ReportFiledEventArgs)
                {
                    this.AddNotification(Notification);
                    this.NotifyUser(true, true, Notification.ToString(), targetCharacter, "report");
                }

                    // finally, if nothing else, add their update if we're interested in them in some way
                else if (this._cm.IsOfInterest(targetCharacter))
                {
                    this.AddNotification(Notification);

                    if (this._cm.SelectedChannel is PMChannelModel)
                    {
                        if ((this._cm.SelectedChannel as PMChannelModel).ID.Equals(
                            targetCharacter, StringComparison.OrdinalIgnoreCase))
                        {
                            return; // don't make a toast if we have their tab focused as it is redundant
                        }
                    }

                    this.NotifyUser(false, false, Notification.ToString(), targetCharacter);
                }
            }

                // the only other kind of update model is a channel update model
            else
            {
                string channelID = ((ChannelUpdateModel)Notification).ChannelID;
                ChannelUpdateModel.ChannelUpdateEventArgs args = ((ChannelUpdateModel)Notification).Arguments;

                this.AddNotification(Notification);
                this.NotifyUser(false, false, Notification.ToString(), channelID);
            }
        }

        private void HideWindow()
        {
            Application.Current.MainWindow.Hide();
            this.icon.Visible = true;
            if (Settings.Default.ShowStillRunning)
            {
                this.icon.ShowBalloonTip(
                    5, 
                    "slimCat", 
                    "slimCat is still running in the background."
                    + "\nClick on this to silence this notification (forever and ever).", 
                    ToolTipIcon.Info);
            }
        }

        private void NotifyUser(
            bool bingLing = false, 
            bool flashWindow = false, 
            string message = null, 
            string target = null, 
            string kind = null)
        {
            if (!ApplicationSettings.ShowNotificationsGlobal)
            {
                return;
            }

            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        if (flashWindow && !this.WindowIsFocused)
                        {
                            Application.Current.MainWindow.FlashWindow();
                        }

                        if (bingLing)
                        {
                            this.DingTheCrapOutOfTheUser();
                        }

                        if (message != null)
                        {
                            this.toast.UpdateNotification(message);
                        }

                        this.toast.Target = target;
                        this.toast.Kind = kind;
                    });
        }

        private void ResetDingLing()
        {
            this.DingLing.Close();
            this.DingLing.Open(new Uri(Environment.CurrentDirectory + @"\sounds\" + "newmessage.wav"));
        }

        private void ToggleSound(object sender, EventArgs e)
        {
            double temp = ApplicationSettings.Volume;
            ApplicationSettings.Volume = this._soundSaveVolume;
            this._soundSaveVolume = temp;

            this.icon.ContextMenu.MenuItems[2].Checked = ApplicationSettings.Volume > 0.0;
        }

        private void ToggleToast(object sender, EventArgs e)
        {
            ApplicationSettings.ShowNotificationsGlobal = !ApplicationSettings.ShowNotificationsGlobal;
            this.icon.ContextMenu.MenuItems[3].Checked = ApplicationSettings.ShowNotificationsGlobal;
        }

        private void convertNotificationLevelToAction(
            int NotificationLevel, string ActionID, NotificationModel Notification)
        {
            switch ((ChannelSettingsModel.NotifyLevel)NotificationLevel)
            {
                    // convert our int into an enum to avoid magic numbers
                case ChannelSettingsModel.NotifyLevel.NoNotification:
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationOnly:
                    this.AddNotification(Notification);
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndToast:
                    this.AddNotification(Notification);
                    this.NotifyUser(false, false, Notification.ToString(), ActionID);
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndSound:
                    this.AddNotification(Notification);
                    this.NotifyUser(true, true, Notification.ToString(), ActionID);
                    return;
            }
        }

        #endregion
    }
}