#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationService.cs">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Media;
    using System.Web;
    using System.Windows;
    using System.Windows.Threading;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     This handles pushing and creating all notifications. This means it plays all sounds, creates all toast
    ///     notifications,
    ///     and is responsible for managing the little tray icon. Additionally, it manages the singleton instance of the
    ///     notifications class.
    ///     It responds to NewMessageEvent, NewPmEvent, NewUpdateEvent
    /// </summary>
    public class NotificationService : DispatcherObject, IDisposable
    {
        #region Fields

        private readonly IChatModel cm;

        private readonly IEventAggregator events;

        private readonly ICharacterManager manager;

        private readonly ToastNotificationsViewModel toast;

        private readonly IIconService iserv; 

        private DateTime lastDingLinged;

        #endregion

        #region Constructors and Destructors

        public NotificationService(IChatState chatState, LoggingService loggingService, IIconService iconService)
        {
            ChatState = chatState;
            events = chatState.EventAggregator;
            cm = chatState.ChatModel;
            manager = chatState.CharacterManager;
            iserv = iconService;
            toast = new ToastNotificationsViewModel(chatState);
            ToastManager = new ToastManager
            {
                AddNotification =
                    notification =>
                    {
                        Dispatcher.Invoke((Action) (() => cm.Notifications.Backlog(notification, 100)));
                        loggingService.LogMessage("!Notifications", notification);
                    },
                ShowToast = toast.ShowNotifications,
                FlashWindow = () => Dispatcher.Invoke((Action) FlashWindow),
                PlaySound = () => Dispatcher.Invoke((Action) DingTheCrapOutOfTheUser),
                Toast = toast
            };

            events.GetEvent<NewMessageEvent>().Subscribe(HandleNewChannelMessage, true);
            events.GetEvent<NewPmEvent>().Subscribe(HandleNewMessage, true);
            events.GetEvent<NewUpdateEvent>().Subscribe(HandleNotification, true);
            events.GetEvent<UnreadUpdatesEvent>().Subscribe(HandleUnreadUpdates, true); 
        }

        #endregion

        #region Properties

        private bool WindowIsFocused
        {
            get { return (bool) Dispatcher.Invoke(new Func<bool>(() => Application.Current.MainWindow.IsActive)); }
        }

        private IChatState ChatState { get; set; }

        private IManageToasts ToastManager { get; set; }

        #endregion

        #region Public Methods and Operators

        public void Dispose()
        {
            Dispose(true);
        }

        public static void ShowWindow()
        {
            Dispatcher.CurrentDispatcher.Invoke((Action) (() =>
            {
                var window = Application.Current.MainWindow;
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;

                window.Activate();
                window.Topmost = true;
                window.Topmost = false;
                window.Focus();

            }));
        }

        #endregion

        #region Methods

        protected virtual void Dispose(bool isManagedDispose)
        {
            if (!isManagedDispose)
                return;

            toast.Dispose();
        }

        private void DingTheCrapOutOfTheUser()
        {
            if ((DateTime.Now - lastDingLinged) <= TimeSpan.FromSeconds(1) || !ApplicationSettings.AllowSound
                || (ApplicationSettings.DisallowNotificationsWhenDnd && ChatState.ChatModel.CurrentCharacter.Status == StatusType.Dnd))
            { return; }

            Log("Playing sound");
            (new SoundPlayer(Environment.CurrentDirectory + @"\sounds\" + "newmessage.wav")).Play();
            lastDingLinged = DateTime.Now;
        }

        private void HandleNewChannelMessage(IDictionary<string, object> update)
        {
            var channel = update.Get<GeneralChannelModel>("channel");
            var message = update.Get<IMessage>("message");

            if (message == null || channel == null) return;

            if (message.Poster.NameEquals(cm.CurrentCharacter.Name))
                return;

            var isFocusedAndSelected = (channel.IsSelected && WindowIsFocused);

            var cleanMessageText = HttpUtility.HtmlDecode(message.Message);

            var temp = new List<string>(channel.Settings.EnumerableTerms);
            temp.AddRange(ApplicationSettings.GlobalNotifyTermsList);
            if (ApplicationSettings.CheckForOwnName)
                temp.Add(cm.CurrentCharacter.Name.ToLower());

            var checkAgainst = temp.Distinct(StringComparer.OrdinalIgnoreCase).Select(x => x.Trim());

            // if any of these conditions hold true we have no reason to evaluate further
            if (manager.IsOnList(message.Poster.Name, ListKind.NotInterested) && message.Type == MessageType.Ad)
                return;

            var notifyLevel = message.Type == MessageType.Ad
                ? channel.Settings.AdNotifyLevel
                : channel.Settings.MessageNotifyLevel;

            // now we check to see if we should notify because of settings
            if (notifyLevel > (int) ChannelSettingsModel.NotifyLevel.NotificationOnly && !isFocusedAndSelected)
            {
                if (!channel.Settings.MessageNotifyOnlyForInteresting || IsOfInterest(message.Poster.Name))
                {
                    if (notifyLevel > (int) ChannelSettingsModel.NotifyLevel.NotificationAndToast)
                    {
                        ToastManager.PlaySound();
                        ToastManager.FlashWindow();
                    }

                    toast.TargetCharacter = message.Poster;
                    toast.Title =
                        "{0} #{1}".FormatWith(ApplicationSettings.ShowNamesInToasts ? message.Poster.Name : "A user",
                            channel.Title);
                    toast.Content = ApplicationSettings.ShowMessagesInToasts ? cleanMessageText : "Has a new message";
                    toast.ShowNotifications();
                    toast.Navigator = new SimpleNavigator(chatState =>
                    {
                        chatState.EventAggregator.GetEvent<RequestChangeTabEvent>().Publish(channel.Id);

                        ShowWindow();
                    });
                    toast.TargetCharacter = message.Poster;
                    if (ApplicationSettings.ShowAvatarsInToasts) message.Poster.GetAvatar();
                }
            }

            #region Ding Word evaluation

            // We have something to check for

            // Tokenized List is the list of terms the message has
            // Check against is a combined set of terms that the user has identified as ding words
            // Is Matching String uses Check against to see if any terms are a match
            var wordList = checkAgainst as IList<string> ?? checkAgainst.ToList();
            if (channel.Settings.NotifyIncludesCharacterNames)
            {
                // if the poster's name contains a ding word
                var match = wordList
                    .Select(dingword => message.Poster.Name.FirstMatch(dingword))
                    .FirstOrDefault(attemptedMatch => !string.IsNullOrWhiteSpace(attemptedMatch.Item1));

                if (match != null)
                {
                    var newUpdate = new CharacterUpdateModel(message.Poster, new ChannelMentionUpdateEventArgs
                    {
                        Channel = channel,
                        Context = match.Item2,
                        TriggeredWord = match.Item1,
                        IsNameMention = true
                    });
                    events.GetEvent<NewUpdateEvent>().Publish(newUpdate);

                    if (!isFocusedAndSelected)
                        channel.FlashTab();

                    message.IsOfInterest = true;
                    return;
                }
            }

            {
                // check the message content
                var match = wordList.Select(cleanMessageText.FirstMatch)
                    .FirstOrDefault(attemptedMatch => !string.IsNullOrWhiteSpace(attemptedMatch.Item1));

                if (match == null) return;
                var newUpdate = new CharacterUpdateModel(message.Poster, new ChannelMentionUpdateEventArgs
                {
                    Channel = channel,
                    Context = match.Item2,
                    TriggeredWord = match.Item1,
                    IsNameMention = false
                });
                events.GetEvent<NewUpdateEvent>().Publish(newUpdate);

                if (!isFocusedAndSelected)
                    channel.FlashTab();

                message.IsOfInterest = true;
            }

            #endregion
        }

        private void FlashWindow()
        {
            if (WindowIsFocused)
            {
                Log("Wanted to flash window, but window was focused");
                return;
            }
            if (ApplicationSettings.DisallowNotificationsWhenDnd
                && ChatState.ChatModel.CurrentCharacter.Status == StatusType.Dnd)
            { return; }

            Log("Flashing window");
            Application.Current.MainWindow.FlashWindow();
        }

        private void HandleNewMessage(IMessage message)
        {
            var poster = message.Poster;
            var channel = cm.CurrentPms.FirstByIdOrNull(message.Poster.Name);
            if (channel == null)
                return;

            if (!channel.HasAutoRepliedTo && cm.AutoReplyEnabled && !string.IsNullOrWhiteSpace(cm.AutoReplyMessage))
            {
                var respondWith = "[b]Auto Reply[/b]: {0}".FormatWith(cm.AutoReplyMessage);

                events.SendUserCommand(CommandDefinitions.ClientSendPm, new[] {respondWith, channel.Id});

                channel.HasAutoRepliedTo = true;
                Log("Auto replied to {0}".FormatWith(channel.Id));
            }
            else if (channel.HasAutoRepliedTo && !cm.AutoReplyEnabled)
                channel.HasAutoRepliedTo = false;

            if (WindowIsFocused)
            {
                if (channel.IsSelected && !ApplicationSettings.PlaySoundEvenWhenTabIsFocused)
                    return;
            }

            var notifyLevel = (ChannelSettingsModel.NotifyLevel) channel.Settings.MessageNotifyLevel;
            if (notifyLevel == ChannelSettingsModel.NotifyLevel.NoNotification) return;

            FlashWindow();
            iserv.SetIconNotificationLevel(true);

            if (notifyLevel == ChannelSettingsModel.NotifyLevel.NotificationOnly) return;

            toast.Title = ApplicationSettings.ShowNamesInToasts ? poster.Name : "A user";
            toast.Content = ApplicationSettings.ShowMessagesInToasts
                ? HttpUtility.HtmlDecode(message.Message)
                : "Sent you a new message";
            toast.Navigator = new SimpleNavigator(chatState =>
            {
                chatState.EventAggregator.SendUserCommand("priv", new[] {poster.Name});

                ShowWindow();
            });
            toast.TargetCharacter = message.Poster;
            if (ApplicationSettings.ShowAvatarsInToasts) message.Poster.GetAvatar();
            toast.ShowNotifications();

            if (notifyLevel == ChannelSettingsModel.NotifyLevel.NotificationAndSound)
            {
                ToastManager.PlaySound();
            }
        }

        private void HandleNotification(NotificationModel notification)
        {
            notification.DisplayNewToast(ChatState, ToastManager);
        }

        private void HandleUnreadUpdates(bool newMsgs)
        {
            iserv.SetIconNotificationLevel(newMsgs);
        }

        private bool IsOfInterest(string name, bool onlineOnly = true)
        {
            return cm.CurrentPms.Any(x => x.Id.Equals(name)) || manager.IsOfInterest(name, onlineOnly);
        }

        private void Log(string text)
        {
            Logging.Log(text, "notify serv");
        }

        #endregion
    }

    public class ToastManager : IManageToasts
    {
        public Action FlashWindow { get; set; }
        public Action PlaySound { get; set; }
        public Action<NotificationModel> AddNotification { get; set; }
        public ToastNotificationsViewModel Toast { get; set; }
        public Action ShowToast { get; set; }
    }

    public class SimpleNavigator : ICanNavigate
    {
        public SimpleNavigator(Action<IChatState> navigateAction)
        {
            Navigate = navigateAction;
        }

        public Action<IChatState> Navigate { get; set; }

        void ICanNavigate.Navigate(IChatState chatState)
        {
            Navigate(chatState);
        }
    }
}