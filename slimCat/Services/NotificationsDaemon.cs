#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationsDaemon.cs">
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
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Media;
    using System.Web;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Threading;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Properties;
    using Utilities;
    using ViewModels;
    using Application = System.Windows.Application;

    #endregion

    /// <summary>
    ///     This handles pushing and creating all notifications. This means it plays all sounds, creates all toast
    ///     notifications,
    ///     and is responsible for managing the little tray icon. Additionally, it manages the singleton instance of the
    ///     notifications class.
    ///     It responds to NewMessageEvent, NewPmEvent, NewUpdateEvent
    /// </summary>
    public class NotificationsDaemon : DispatcherObject, IDisposable
    {
        #region Fields

        private readonly IChatModel cm;

        private readonly IEventAggregator events;

        private readonly NotifyIcon icon = new NotifyIcon();
        private readonly ICharacterManager manager;

        private readonly ToastNotificationsViewModel toast;

        private DateTime lastDingLinged;

        private double soundSaveVolume;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="NotificationsDaemon" /> class.
        /// </summary>
        /// <param name="eventagg">
        ///     The eventagg.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        /// <param name="manager"></param>
        public NotificationsDaemon(IEventAggregator eventagg, IChatModel cm, ICharacterManager manager)
        {
            events = eventagg;
            this.cm = cm;
            this.manager = manager;
            toast = new ToastNotificationsViewModel(events);

            events.GetEvent<NewMessageEvent>().Subscribe(HandleNewChannelMessage, true);
            events.GetEvent<NewPmEvent>().Subscribe(HandleNewMessage, true);
            events.GetEvent<NewUpdateEvent>().Subscribe(HandleNotification, true);

            events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(
                args =>
                    {
                        Application.Current.MainWindow.Closing += (s, e) =>
                            {
                                e.Cancel = true;
                                HideWindow();
                            };

                        Application.Current.MainWindow.MouseLeave +=
                            (s, e) => events.GetEvent<ErrorEvent>().Publish(null);

                        this.cm.SelectedChannelChanged += (s, e) => events.GetEvent<ErrorEvent>().Publish(null);

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
                                    args))
                                {
                                    Enabled = false
                                });
                        iconMenu.MenuItems.Add(new MenuItem("-"));

                        iconMenu.MenuItems.Add(
                            new MenuItem("Sounds Enabled", ToggleSound)
                                {
                                    Checked =
                                        ApplicationSettings.Volume > 0.0,
                                });
                        iconMenu.MenuItems.Add(
                            new MenuItem("Toasts Enabled", ToggleToast)
                                {
                                    Checked =
                                        ApplicationSettings
                                            .ShowNotificationsGlobal
                                });
                        iconMenu.MenuItems.Add(new MenuItem("-"));

                        iconMenu.MenuItems.Add("Show", (s, e) => ShowWindow());
                        iconMenu.MenuItems.Add("Exit", (s, e) => ShutDown());

                        icon.Text = string.Format("{0} - {1}", Constants.ClientId, args);
                        icon.ContextMenu = iconMenu;
                        icon.Visible = true;
                    });
        }

        #endregion

        #region Properties

        private bool WindowIsFocused
        {
            get { return (bool) Dispatcher.Invoke(new Func<bool>(() => Application.Current.MainWindow.IsActive)); }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     The show window.
        /// </summary>
        public static void ShowWindow()
        {
            Application.Current.MainWindow.Show();
            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                Application.Current.MainWindow.WindowState = WindowState.Normal;

            Application.Current.MainWindow.Activate();
        }

        /// <summary>
        ///     The shut down.
        /// </summary>
        public void ShutDown()
        {
            icon.Dispose();
            Dispatcher.InvokeShutdown();
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="isManagedDispose">
        ///     The is managed dispose.
        /// </param>
        protected virtual void Dispose(bool isManagedDispose)
        {
            if (!isManagedDispose)
                return;

            icon.Dispose();
            toast.Dispose();
        }

        /// <summary>
        ///     Adds the notification to the notifications collection
        /// </summary>
        /// <param name="notification">
        ///     The Notification.
        /// </param>
        private void AddNotification(NotificationModel notification)
        {
            Dispatcher.Invoke((Action) (() => cm.Notifications.Add(notification)));
        }

        private void DingTheCrapOutOfTheUser()
        {
            if ((DateTime.Now - lastDingLinged) <= TimeSpan.FromSeconds(1) ||
                Math.Abs(ApplicationSettings.Volume) < 0.01)
                return;

            (new SoundPlayer(Environment.CurrentDirectory + @"\sounds\" + "newmessage.wav")).Play();
            lastDingLinged = DateTime.Now;
        }

        /// <summary>
        ///     Notification logic for new channel Ads and messages
        /// </summary>
        /// <param name="update">
        ///     The update.
        /// </param>
        private void HandleNewChannelMessage(IDictionary<string, object> update)
        {
            var channel = update["channel"] as GeneralChannelModel;
            var message = update["message"] as IMessage;

            if (message == null || channel == null) return;

            var cleanMessageText = HttpUtility.HtmlDecode(message.Message);

            var temp = new List<string>(channel.Settings.EnumerableTerms);
            temp.AddRange(ApplicationSettings.GlobalNotifyTermsList);

            var checkAgainst = temp.Distinct(StringComparer.OrdinalIgnoreCase);

            // if any of these conditions hold true we have no reason to evaluate further
            if (channel.IsSelected && WindowIsFocused)
                return;

            if (ApplicationSettings.NotInterested.Contains(message.Poster.Name))
                return;


            // now we check to see if we should notify because of settings
            if (channel.Settings.MessageNotifyLevel > (int) ChannelSettingsModel.NotifyLevel.NotificationOnly)
            {
                var shouldDing = channel.Settings.MessageNotifyLevel
                                 > (int) ChannelSettingsModel.NotifyLevel.NotificationAndToast;

                if ((channel.Settings.MessageNotifyOnlyForInteresting && IsOfInterest(message.Poster.Name))
                    || !channel.Settings.MessageNotifyOnlyForInteresting)
                {
                    NotifyUser(shouldDing, shouldDing, message.Poster.Name + '\n' + cleanMessageText, channel.Id);
                    return; // and if we do, there is no need to evalutae further
                }
            }

            if (!channel.Settings.EnumerableTerms.Any() && !ApplicationSettings.GlobalNotifyTermsList.Any())
                return; // if we don't have anything to check for, no need to evaluate further

            #region Ding Word evaluation

            // We have something to check for

            // Tokenized List is the list of terms the message has
            // Check against is a combined set of terms that the user has identified as ding words
            // Is Matching String uses Check against to see if any terms are a match
            if (channel.Settings.NotifyIncludesCharacterNames)
            {
                // if the poster's name contains a ding word
                var match = checkAgainst
                    .Select(dingword => message.Poster.Name.FirstMatch(dingword))
                    .FirstOrDefault(attemptedMatch => !string.IsNullOrWhiteSpace(attemptedMatch.Item1));

                if (match != null)
                {
                    var notifyMessage = string.Format(
                        "{0}'s name matches {1}:\n{2}", message.Poster.Name, match.Item1, match.Item2);

                    NotifyUser(true, true, notifyMessage, channel.Id);
                    channel.FlashTab();
                    return;
                }
            }

            {
                // Now our character's name is always added
                var name = cm.CurrentCharacter.Name.ToLower();

                temp.Add(name); // fixes an issue where a user's name would ding constantly
                if (name.Last() != 's' && name.Last() != 'z')
                    temp.Add(name + @"'s"); // possessive fix
                else
                    temp.Add(name + @"'");

                checkAgainst = temp.Distinct(StringComparer.OrdinalIgnoreCase);
            }

            {
                // check the message content
                var match = checkAgainst.Select(cleanMessageText.FirstMatch)
                    .FirstOrDefault(attemptedMatch => !string.IsNullOrWhiteSpace(attemptedMatch.Item1));

                if (match == null) return;

                // if one of our words is a dingling word
                var notifyMessage = string.Format(
                    "{0} mentioned {1}:\n{2}", message.Poster.Name, match.Item1, match.Item2);

                NotifyUser(true, true, notifyMessage, channel.Id);
                channel.FlashTab();
            }

            #endregion
        }

        private void FlashWindow()
        {
            if (!WindowIsFocused)
                Application.Current.MainWindow.FlashWindow();
        }

        /// <summary>
        ///     Notification logic for new Pm messages
        /// </summary>
        /// <param name="message">
        ///     The Message.
        /// </param>
        private void HandleNewMessage(IMessage message)
        {
            var poster = message.Poster;
            var channel = cm.CurrentPms.FirstByIdOrDefault(message.Poster.Name);
            if (channel == null)
                return;

            var windowFocusMatters = WindowIsFocused && !ApplicationSettings.PlaySoundEvenWhenWindowIsFocused;
            var channelFocusMatters = channel.IsSelected && !ApplicationSettings.PlaySoundEvenWhenTabIsFocused;

            if (channelFocusMatters && windowFocusMatters)
                return;

            switch ((ChannelSettingsModel.NotifyLevel) channel.Settings.MessageNotifyLevel)
            {
                case ChannelSettingsModel.NotifyLevel.NotificationAndToast:
                    NotifyUser(
                        false, false, poster.Name + '\n' + HttpUtility.HtmlDecode(message.Message), poster.Name);
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndSound:
                    NotifyUser(
                        true, true, poster.Name + '\n' + HttpUtility.HtmlDecode(message.Message), poster.Name);
                    return;

                default:
                    FlashWindow();
                    return;
            }
        }

        private void HandleNotification(NotificationModel notification)
        {
            // TODO: I'M DYIN' OVER HERE! REFACTOR ME!

            // character update models will be *most* of the notification the user will see
            var model = notification as CharacterUpdateModel;
            if (model != null)
            {
                var targetCharacter = model.TargetCharacter.Name;
                var args = model.Arguments;

                // handle if the notification involves a character being promoted or demoted
                var eventArgs = args as CharacterUpdateModel.PromoteDemoteEventArgs;
                if (eventArgs != null)
                {
                    var channelId = eventArgs.TargetChannelId;

                    // find by ID, not name
                    var channel = cm.CurrentChannels.FirstByIdOrDefault(channelId);

                    if (channel == null)
                        return;

                    if (channel.Settings.PromoteDemoteNotifyOnlyForInteresting)
                    {
                        if (!IsOfInterest(targetCharacter))
                            return; // if we only want to know interesting people, no need to evalute further
                    }

                    ConvertNotificationLevelToAction(
                        channel.Settings.PromoteDemoteNotifyLevel, channelId, model);
                }
                else if (args is CharacterUpdateModel.JoinLeaveEventArgs)
                {
                    // special check for this as it has settings per channel
                    var target = ((CharacterUpdateModel.JoinLeaveEventArgs) args).TargetChannelId;

                    // find by ID, not name
                    var channel = cm.CurrentChannels.FirstByIdOrDefault(target);

                    if (channel == null)
                        return;

                    if (channel.Settings.JoinLeaveNotifyOnlyForInteresting)
                    {
                        if (!IsOfInterest(targetCharacter))
                            return;
                    }

                    ConvertNotificationLevelToAction(channel.Settings.JoinLeaveNotifyLevel, target, model);
                }
                else if (args is CharacterUpdateModel.NoteEventArgs || args is CharacterUpdateModel.CommentEventArgs)
                {
                    AddNotification(model);

                    var link = args is CharacterUpdateModel.NoteEventArgs
                        ? ((CharacterUpdateModel.NoteEventArgs) args).Link
                        : ((CharacterUpdateModel.CommentEventArgs) args).Link;

                    NotifyUser(false, false, notification.ToString(), link);
                }
                else if (args is CharacterUpdateModel.ListChangedEventArgs)
                {
                    AddNotification(model);
                    NotifyUser(false, false, notification.ToString(), targetCharacter);
                }
                else if (args is CharacterUpdateModel.ReportHandledEventArgs)
                {
                    AddNotification(model);
                    NotifyUser(true, true, notification.ToString(), targetCharacter);
                }
                else if (args is CharacterUpdateModel.ReportFiledEventArgs)
                {
                    AddNotification(model);
                    NotifyUser(true, true, notification.ToString(), targetCharacter, "report");
                }
                else if (args is CharacterUpdateModel.BroadcastEventArgs)
                {
                    AddNotification(model);
                    NotifyUser(true, true, notification.ToString(), targetCharacter);
                }
                else if (IsOfInterest(targetCharacter, false) && !model.TargetCharacter.IgnoreUpdates)
                {
                    AddNotification(model);

                    if (cm.CurrentChannel is PmChannelModel)
                    {
                        if ((cm.CurrentChannel as PmChannelModel).Id.Equals(
                            targetCharacter, StringComparison.OrdinalIgnoreCase))
                            return; // don't make a toast if we have their tab focused as it is redundant
                    }

                    NotifyUser(false, false, notification.ToString(), targetCharacter);
                }
            }
            else
            {
                var channelUpdate = (ChannelUpdateModel) notification;

                if (!channelUpdate.TargetChannel.Settings.AlertAboutUpdates) return;

                AddNotification(notification);
                NotifyUser(false, false, notification.ToString(), channelUpdate.TargetChannel.Id);
            }
        }

        private bool IsOfInterest(string name, bool onlineOnly = true)
        {
            return cm.CurrentPms.Any(x => x.Id.Equals(name)) || manager.IsOfInterest(name, onlineOnly);
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

        private void NotifyUser(
            bool bingLing = false,
            bool flashWindow = false,
            string message = null,
            string target = null,
            string kind = null)
        {
            Action notify = () =>
                {
                    if (flashWindow)
                        FlashWindow();

                    if (bingLing)
                        DingTheCrapOutOfTheUser();

                    if (message != null && ApplicationSettings.ShowNotificationsGlobal)
                        toast.UpdateNotification(message);

                    toast.Target = target;
                    toast.Kind = kind;
                };

            Dispatcher.Invoke(notify);
        }

        private void ToggleSound(object sender, EventArgs e)
        {
            var temp = ApplicationSettings.Volume;
            ApplicationSettings.Volume = soundSaveVolume;
            soundSaveVolume = temp;

            icon.ContextMenu.MenuItems[2].Checked = ApplicationSettings.Volume > 0.0;
        }

        private void ToggleToast(object sender, EventArgs e)
        {
            ApplicationSettings.ShowNotificationsGlobal = !ApplicationSettings.ShowNotificationsGlobal;
            icon.ContextMenu.MenuItems[3].Checked = ApplicationSettings.ShowNotificationsGlobal;
        }

        private void ConvertNotificationLevelToAction(
            int notificationLevel, string actionId, NotificationModel notification)
        {
            switch ((ChannelSettingsModel.NotifyLevel) notificationLevel)
            {
                    // convert our int into an enum to avoid magic numbers
                case ChannelSettingsModel.NotifyLevel.NoNotification:
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationOnly:
                    AddNotification(notification);
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndToast:
                    AddNotification(notification);
                    NotifyUser(false, false, notification.ToString(), actionId);
                    return;

                case ChannelSettingsModel.NotifyLevel.NotificationAndSound:
                    AddNotification(notification);
                    NotifyUser(true, true, notification.ToString(), actionId);
                    return;
            }
        }

        #endregion
    }
}