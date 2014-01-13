#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PMChannelViewModel.cs">
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

namespace Slimcat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Timers;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     Used for most communications between users.
    /// </summary>
    public class PmChannelViewModel : ChannelViewModelBase
    {
        #region Fields

        private Timer checkTick = new Timer(5000);

        private Timer cooldownTimer = new Timer(500);

        private bool isInCoolDown;

        private bool isTyping;

        private FilteredCollection<IMessage, IViewableObject> messageManager;

        private int typingLengthCache;

        private ICharacter conversationWith;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PmChannelViewModel" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="events">
        ///     The events.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        public PmChannelViewModel(
            string name, IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm, ICharacterManager manager)
            : base(contain, regman, events, cm, manager)
        {
            try
            {
                var temp = Container.Resolve<PmChannelModel>(name);
                Model = temp;

                Model.PropertyChanged += OnModelPropertyChanged;

                Container.RegisterType<object, PmChannelView>(
                    HelperConverter.EscapeSpaces(Model.Id), new InjectionConstructor(this));
                Events.GetEvent<NewUpdateEvent>()
                    .Subscribe(OnNewUpdateEvent, ThreadOption.PublisherThread, true, UpdateIsOurCharacter);

                cooldownTimer.Elapsed += (s, e) =>
                    {
                        isInCoolDown = false;
                        cooldownTimer.Enabled = false;
                        OnPropertyChanged("CanPost");
                    };

                checkTick.Elapsed += (s, e) =>
                    {
                        if (!IsTyping)
                            checkTick.Enabled = false;

                        if (Message != null && typingLengthCache == Message.Length)
                        {
                            IsTyping = false;
                            SendTypingNotification(TypingStatus.Paused);
                            checkTick.Enabled = false;
                        }

                        if (IsTyping)
                            typingLengthCache = Message != null ? Message.Length : 0;
                    };

                Model.Settings = SettingsDaemon.GetChannelSettings(
                    cm.CurrentCharacter.Name, Model.Title, Model.Id, Model.Type);

                ChannelSettings.Updated += (s, e) =>
                    {
                        OnPropertyChanged("ChannelSettings");
                        if (!ChannelSettings.IsChangingSettings)
                        {
                            SettingsDaemon.UpdateSettingsFile(
                                ChannelSettings, cm.CurrentCharacter.Name, Model.Title, Model.Id);
                        }
                    };

                messageManager = new FilteredCollection<IMessage, IViewableObject>(
                    Model.Messages, message => true);
            }
            catch (Exception ex)
            {
                ex.Source = "Utility Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Events

        public event EventHandler StatusChanged;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether can post.
        /// </summary>
        public bool CanPost
        {
            get { return !isInCoolDown; }
        }

        /// <summary>
        ///     Gets the conversation with.
        /// </summary>
        public ICharacter ConversationWith
        {
            get { return conversationWith ?? (conversationWith = CharacterManager.Find(Model.Id)); }
        }

        public ObservableCollection<IViewableObject> CurrentMessages
        {
            get { return messageManager.Collection; }
        }

        /// <summary>
        ///     Used for channel settings to display settings related to notify terms
        /// </summary>
        public bool HasNotifyTerms
        {
            get { return !string.IsNullOrEmpty(ChannelSettings.NotifyTerms); }
        }

        /// <summary>
        ///     Gets a value indicating whether has status.
        /// </summary>
        public bool HasStatus
        {
            get { return ConversationWith.StatusMessage.Length > 0; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is typing.
        /// </summary>
        public bool IsTyping
        {
            get { return isTyping; }

            set
            {
                isTyping = value;
                OnPropertyChanged("ShouldShowPostLength");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether should show post length.
        /// </summary>
        public bool ShouldShowPostLength
        {
            get { return !string.IsNullOrEmpty(Message) && isTyping; }
        }

        /// <summary>
        ///     This is used for the channel settings, if it should show settings like 'notify when this character is mentioned'
        /// </summary>
        public bool ShowAllSettings
        {
            get { return false; }
        }

        /// <summary>
        ///     Gets the status string.
        /// </summary>
        public string StatusString
        {
            get
            {
                switch (ConversationWith.Status)
                {
                    case StatusType.Offline:
                    case StatusType.Away:
                    case StatusType.Busy:
                    case StatusType.Idle:
                        return string.Format("Warning: {0} is currently {1}.", Model, conversationWith.Status.ToString().ToLower());
                    case StatusType.Looking:
                        return string.Format("{0} is looking for roleplay.", Model.Id);
                    case StatusType.Dnd:
                        return string.Format("Warning: {0} does not wish to be disturbed.", Model.Id);
                    case StatusType.Online:
                        return string.Format("{0} is online.", Model.Id);
                    case StatusType.Crown:
                        return string.Format(
                            "{0} has been a good person and has been rewarded with a crown!", Model.Id);
                }

                return ConversationWith.Status.ToString();
            }
        }

        /// <summary>
        ///     Gets the typing string.
        /// </summary>
        public string TypingString
        {
            get
            {
                var pm = (PmChannelModel) Model;

                if (ConversationWith.Status == StatusType.Offline)
                {
                    // visual indicator to help the user know when the other has gone offline
                    return string.Format("{0} is not online!", pm.Id);
                }

                switch (pm.TypingStatus)
                {
                    case TypingStatus.Typing:
                        return string.Format("{0} is typing " + pm.TypingString, pm.Id);
                    case TypingStatus.Paused:
                        return string.Format("{0} has entered text.", pm.Id);
                    default:
                        return string.Empty;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="isManaged">
        ///     The is managed.
        /// </param>
        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                checkTick.Dispose();
                cooldownTimer.Dispose();
                checkTick = null;
                cooldownTimer = null;

                StatusChanged = null;
                Events.GetEvent<NewUpdateEvent>().Unsubscribe(OnNewUpdateEvent);

                messageManager.Dispose();
                messageManager = null;
            }

            base.Dispose(isManaged);
        }

        /// <summary>
        ///     The on model property changed.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TypingStatus" || e.PropertyName == "TypingString")
                OnPropertyChanged("TypingString");
        }

        /// <summary>
        ///     The on this property changed.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        protected override void OnThisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Message")
                return;

            if (string.IsNullOrEmpty(Message))
            {
                SendTypingNotification(TypingStatus.Clear);
                IsTyping = false;
            }
            else if (!IsTyping)
            {
                IsTyping = true;
                SendTypingNotification(TypingStatus.Typing);
                checkTick.Enabled = true;
            }
        }

        /// <summary>
        ///     The send message.
        /// </summary>
        protected override void SendMessage()
        {
            if (Message.Length > 50000)
            {
                UpdateError("I can't let you post that. That's way too big. Try again, buddy.");
                return;
            }

            if (isInCoolDown)
            {
                UpdateError("Where's the fire, son? Slow it down.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                UpdateError("Hmm. Did you ... did you write anything?");
                return;
            }

            var toSend =
                CommandDefinitions.CreateCommand(
                    CommandDefinitions.ClientSendPm, new List<string> {Message, ConversationWith.Name})
                    .ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(toSend);
            Message = string.Empty;

            isInCoolDown = true;
            cooldownTimer.Enabled = true;
            OnPropertyChanged("CanPost");
            IsTyping = false;
            checkTick.Enabled = false;
        }

        private void OnNewUpdateEvent(NotificationModel param)
        {
            OnPropertyChanged("ConversationWith");
            OnPropertyChanged("StatusString");
            OnPropertyChanged("HasStatus");
            OnPropertyChanged("CanPost");
            OnPropertyChanged("TypingString");

            var arguments = ((CharacterUpdateModel) param).Arguments;
            if (!(arguments is CharacterUpdateModel.PromoteDemoteEventArgs))
                OnStatusChanged();
        }

        private void OnStatusChanged()
        {
            if (StatusChanged != null)
                StatusChanged(this, new EventArgs());
        }

        private void SendTypingNotification(TypingStatus type)
        {
            var toSend =
                CommandDefinitions.CreateCommand(
                    CommandDefinitions.ClientSendTypingStatus,
                    new List<string> {type.ToString(), ConversationWith.Name}).ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        /// <summary>
        ///     If the update is applicable to our Pm tab
        /// </summary>
        /// <param name="param">
        ///     The param.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool UpdateIsOurCharacter(NotificationModel param)
        {
            var updateModel = param as CharacterUpdateModel;
            if (updateModel != null)
            {
                var args = updateModel.TargetCharacter;
                return args.Name.Equals(ConversationWith.Name, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        #endregion
    }
}