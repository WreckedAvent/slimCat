// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PMChannelViewModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
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
//   Used for most communications between users.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Timers;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using Services;

    using slimCat;

    using Views;

    /// <summary>
    ///     Used for most communications between users.
    /// </summary>
    public class PMChannelViewModel : ChannelViewModelBase, IDisposable
    {
        #region Fields

        private Timer _checkTick = new Timer(5000);

        private Timer _cooldownTimer = new Timer(500);

        private bool _isInCoolDown;

        private bool _isTyping;

        private int _typingLengthCache;

        public event EventHandler StatusChanged;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PMChannelViewModel"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        public PMChannelViewModel(
            string name, IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                var temp = this._container.Resolve<PMChannelModel>(name);
                this.Model = temp;

                this.Model.PropertyChanged += this.OnModelPropertyChanged;

                this._container.RegisterType<object, PMChannelView>(
                    HelperConverter.EscapeSpaces(this.Model.ID), new InjectionConstructor(this));
                this._events.GetEvent<NewUpdateEvent>()
                    .Subscribe(this.OnNewUpdateEvent, ThreadOption.PublisherThread, true, this.UpdateIsOurCharacter);

                this._cooldownTimer.Elapsed += (s, e) =>
                    {
                        this._isInCoolDown = false;
                        this._cooldownTimer.Enabled = false;
                        this.OnPropertyChanged("CanPost");
                    };

                this._checkTick.Elapsed += (s, e) =>
                    {
                        if (!this.IsTyping)
                        {
                            this._checkTick.Enabled = false;
                        }

                        if (this.Message != null && this._typingLengthCache == this.Message.Length)
                        {
                            this.IsTyping = false;
                            this.SendTypingNotification(Typing_Status.paused);
                            this._checkTick.Enabled = false;
                        }

                        if (this.IsTyping)
                        {
                            this._typingLengthCache = this.Message != null ? this.Message.Length : 0;
                        }
                    };

                this.Model.Settings = SettingsDaemon.GetChannelSettings(
                    cm.SelectedCharacter.Name, this.Model.Title, this.Model.ID, this.Model.Type);

                this.ChannelSettings.Updated += (s, e) =>
                    {
                        this.OnPropertyChanged("ChannelSettings");
                        if (!this.ChannelSettings.IsChangingSettings)
                        {
                            SettingsDaemon.UpdateSettingsFile(
                                this.ChannelSettings, cm.SelectedCharacter.Name, this.Model.Title, this.Model.ID);
                        }
                    };
            }
            catch (Exception ex)
            {
                ex.Source = "Utility Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether can post.
        /// </summary>
        public bool CanPost
        {
            get
            {
                return !this._isInCoolDown;
            }
        }

        /// <summary>
        ///     Gets the conversation with.
        /// </summary>
        public ICharacter ConversationWith
        {
            get
            {
                return this.CM.IsOnline(this.Model.ID) ? this.CM.FindCharacter(this.Model.ID) : new CharacterModel { Name = this.Model.ID };
            }
        }

        /// <summary>
        ///     Used for channel settings to display settings related to notify terms
        /// </summary>
        public bool HasNotifyTerms
        {
            get
            {
                return !string.IsNullOrEmpty(this.ChannelSettings.NotifyTerms);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has status.
        /// </summary>
        public bool HasStatus
        {
            get
            {
                return this.ConversationWith.StatusMessage.Length > 0;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is typing.
        /// </summary>
        public bool IsTyping
        {
            get
            {
                return this._isTyping;
            }

            set
            {
                this._isTyping = value;
                this.OnPropertyChanged("ShouldShowPostLength");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether should show post length.
        /// </summary>
        public bool ShouldShowPostLength
        {
            get
            {
                return !string.IsNullOrEmpty(this.Message) && this._isTyping;
            }
        }

        /// <summary>
        ///     This is used for the channel settings, if it should show settings like 'notify when this character is mentioned'
        /// </summary>
        public bool ShowAllSettings
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///     Gets the status string.
        /// </summary>
        public string StatusString
        {
            get
            {
                if (!this.CM.IsOnline(this.Model.ID))
                {
                    return string.Format("Warning: {0} is not online.", this.Model.ID);
                }

                switch (this.ConversationWith.Status)
                {
                    case StatusType.away:
                        return string.Format("Warning: {0} is currently away.", this.Model.ID);
                    case StatusType.busy:
                        return string.Format("Warning: {0} is currently busy.", this.Model.ID);
                    case StatusType.idle:
                        return string.Format("Warning: {0} is currently idle.", this.Model.ID);
                    case StatusType.looking:
                        return string.Format("{0} is looking for roleplay.", this.Model.ID);
                    case StatusType.dnd:
                        return string.Format("Warning: {0} does not wish to be disturbed.", this.Model.ID);
                    case StatusType.online:
                        return string.Format("{0} is online.", this.Model.ID);
                    case StatusType.crown:
                        return string.Format(
                            "{0} has been a good person and has been rewarded with a crown!", this.Model.ID);
                }

                return this.ConversationWith.Status.ToString();
            }
        }

        /// <summary>
        ///     Gets the typing string.
        /// </summary>
        public string TypingString
        {
            get
            {
                var PM = (PMChannelModel)this.Model;

                if (!this.CM.IsOnline(this.Model.ID))
                {
                    // visual indicator to help the user know when the other has gone offline
                    return string.Format("{0} is not online!", PM.ID);
                }

                switch (PM.TypingStatus)
                {
                    case Typing_Status.typing:
                        return string.Format("{0} is typing " + PM.TypingString, PM.ID);
                    case Typing_Status.paused:
                        return string.Format("{0} has entered text.", PM.ID);
                    default:
                        return string.Empty;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="IsManaged">
        /// The is managed.
        /// </param>
        protected override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                this._checkTick.Dispose();
                this._cooldownTimer.Dispose();
                this._checkTick = null;
                this._cooldownTimer = null;

                this.StatusChanged = null;
                this._events.GetEvent<NewUpdateEvent>().Unsubscribe(this.OnNewUpdateEvent);
            }

            base.Dispose(IsManaged);
        }

        /// <summary>
        /// The on model property changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TypingStatus" || e.PropertyName == "TypingString")
            {
                this.OnPropertyChanged("TypingString");
            }
        }

        /// <summary>
        /// The on this property changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected override void OnThisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Message")
            {
                return;
            }

            if (string.IsNullOrEmpty(this.Message))
            {
                this.SendTypingNotification(Typing_Status.clear);
                this.IsTyping = false;
            }
            else if (!this.IsTyping)
            {
                this.IsTyping = true;
                this.SendTypingNotification(Typing_Status.typing);
                this._checkTick.Enabled = true;
            }
        }

        /// <summary>
        ///     The send message.
        /// </summary>
        protected override void SendMessage()
        {
            if (this.Message.Length > 50000)
            {
                this.UpdateError("I can't let you post that. That's way too big. Try again, buddy.");
            }
            else if (this._isInCoolDown)
            {
                this.UpdateError("Where's the fire, son? Slow it down.");
            }
            else if (string.IsNullOrWhiteSpace(this.Message))
            {
                this.UpdateError("Hmm. Did you ... did you write anything?");
            }

            IDictionary<string, object> toSend =
                CommandDefinitions.CreateCommand(
                    CommandDefinitions.ClientSendPM, new List<string> { this.Message, this.ConversationWith.Name })
                                  .toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(toSend);
            this.Message = string.Empty;

            this._isInCoolDown = true;
            this._cooldownTimer.Enabled = true;
            this.OnPropertyChanged("CanPost");
            this.IsTyping = false;
            this._checkTick.Enabled = false;
        }

        private void OnNewUpdateEvent(NotificationModel param)
        {
            this.OnPropertyChanged("ConversationWith");
            this.OnPropertyChanged("StatusString");
            this.OnPropertyChanged("HasStatus");
            this.OnPropertyChanged("CanPost");
            this.OnPropertyChanged("TypingString");

            CharacterUpdateModel.CharacterUpdateEventArgs arguments = ((CharacterUpdateModel)param).Arguments;
            if (!(arguments is CharacterUpdateModel.PromoteDemoteEventArgs))
            {
                this.OnStatusChanged();
            }
        }

        private void OnStatusChanged()
        {
            if (this.StatusChanged != null)
            {
                this.StatusChanged(this, new EventArgs());
            }
        }

        private void SendTypingNotification(Typing_Status type)
        {
            IDictionary<string, object> toSend =
                CommandDefinitions.CreateCommand(
                    CommandDefinitions.ClientSendTypingStatus, 
                    new List<string> { type.ToString(), this.ConversationWith.Name }).toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        /// <summary>
        /// If the update is applicable to our PM tab
        /// </summary>
        /// <param name="param">
        /// The param.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool UpdateIsOurCharacter(NotificationModel param)
        {
            if (param is CharacterUpdateModel)
            {
                ICharacter args = ((CharacterUpdateModel)param).TargetCharacter;
                return args.Name.Equals(this.ConversationWith.Name, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        #endregion
    }
}