using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using slimCat;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using Services;
using Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;

namespace ViewModels
{
    /// <summary>
    /// Used for most communications between users.
    /// </summary>
    public class PMChannelViewModel : ChannelViewModelBase, IDisposable
    {
        #region Fields
        private bool _isInCoolDown = false;
        private System.Timers.Timer _cooldownTimer = new System.Timers.Timer(500);
        private System.Timers.Timer _checkTick = new System.Timers.Timer(5000);
        private bool _isTyping;
        private int _typingLengthCache;
        public event EventHandler StatusChanged;
        public EventHandler NewMessageArrived;
        #endregion

        #region Properties
        public ICharacter ConversationWith
        {
            get
            {
                if (CM.IsOnline(Model.ID))
                    return CM.FindCharacter(Model.ID);
                else
                    return new CharacterModel() { Name = Model.ID };
            }
        }

        public string StatusString
        {
            get
            {
                if (!CM.IsOnline(Model.ID))
                    return string.Format("Warning: {0} is not online.", Model.ID);

                switch (ConversationWith.Status)
                {
                    case StatusType.away:
                            return string.Format("Warning: {0} is currently away.", Model.ID);
                    case StatusType.busy:
                            return string.Format("Warning: {0} is currently busy.", Model.ID);
                    case StatusType.idle:
                            return string.Format("Warning: {0} is currently idle.", Model.ID);
                    case StatusType.looking:
                            return string.Format("{0} is looking for roleplay.", Model.ID);
                    case StatusType.dnd:
                            return string.Format("Warning: {0} does not wish to be disturbed.", Model.ID);
                    case StatusType.online:
                            return string.Format("{0} is online.", Model.ID);
                    case StatusType.crown:
                            return string.Format("{0} has been a good person and has been rewarded with a crown!", Model.ID);
                }

                return ConversationWith.Status.ToString();
            }
        }

        public bool HasStatus
        {
            get
            {
                return ConversationWith.StatusMessage.Length > 0;
            }
        }

        public bool CanPost
        {
            get
            {
                return (CM.IsOnline(Model.ID) && !_isInCoolDown);
            }
        }

        public string TypingString
        {
            get
            {
                var PM = (PMChannelModel)Model;
                switch (PM.TypingStatus)
                {
                    case Typing_Status.typing: return string.Format("{0} is typing " + PM.TypingString, PM.ID);
                    case Typing_Status.paused: return string.Format("{0} has entered text.", PM.ID);
                    default: return "";
                }
            }
        }

        public bool ShouldShowPostLength
        {
            get
            {
                return (Message != null && Message.Length > 0 && _isTyping);
            }
        }

        public bool IsTyping
        {
            get { return _isTyping; }
            set
            {
                _isTyping = value;
                OnPropertyChanged("ShouldShowPostLength");
            }
        }
        #endregion

        #region Constructors
        public PMChannelViewModel(string name, IUnityContainer contain, IRegionManager regman,
                                  IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                PMChannelModel temp = _container.Resolve<PMChannelModel>(name);
                Model = temp;

                Model.PropertyChanged += OnModelPropertyChanged;

                _container.RegisterType<Object, PMChannelView>(HelperConverter.EscapeSpaces(Model.ID), new InjectionConstructor(this));
                _events.GetEvent<NewUpdateEvent>().Subscribe(OnNewUpdateEvent, ThreadOption.PublisherThread, true, UpdateIsOurCharacter);

                Model.Messages.CollectionChanged += OnMessagesChanged;

                #region disposable events
                _cooldownTimer.Elapsed += (s, e) =>
                {
                    _isInCoolDown = _isInCoolDown && false;
                    _cooldownTimer.Enabled = false;
                    OnPropertyChanged("CanPost");
                };

                _checkTick.Elapsed += (s, e) =>
                    {
                        if (!IsTyping)
                            _checkTick.Enabled = false;

                        if (Message != null && _typingLengthCache == Message.Length)
                        {
                            IsTyping = false;
                            SendTypingNotification(Typing_Status.paused);
                            _checkTick.Enabled = false;
                        }

                        if (IsTyping)
                            _typingLengthCache = (Message != null ? Message.Length : 0);
                    };
                #endregion

                #region load settings
                Model.Settings = Services.SettingsDaemon.GetChannelSettings(cm.SelectedCharacter.Name, Model.Title, Model.ID, Model.Type);

                ChannelSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("ChannelSettings");
                    if (!ChannelSettings.IsChangingSettings)
                        Services.SettingsDaemon.UpdateSettingsFile(ChannelSettings, cm.SelectedCharacter.Name, Model.Title, Model.ID);
                };
                #endregion
            }

            catch (Exception ex)
            {
                ex.Source = "Utility Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        protected override void SendMessage()
        {
            if (Message.Length > 50000)
                UpdateError("I can't let you post that. That's way too big. Try again, buddy.");

            else if (_isInCoolDown)
                UpdateError("Where's the fire, son? Slow it down.");

            else if (String.IsNullOrWhiteSpace(Message))
                UpdateError("Hmm. Did you ... did you write anything?");

            else if (CM.IsOnline(Model.ID))
            {

                IDictionary<string, object> toSend = CommandDefinitions
                    .CreateCommand(CommandDefinitions.ClientSendPM, new List<string>() { this.Message, ConversationWith.Name })
                    .toDictionary();

                _events.GetEvent<UserCommandEvent>().Publish(toSend);
                this.Message = "";

                _isInCoolDown = true;
                _cooldownTimer.Enabled = true;
                OnPropertyChanged("CanPost");
                IsTyping = false;
                _checkTick.Enabled = false;
            }

            else UpdateError(Model.ID + " is not online!");
        }

        private void SendTypingNotification(Typing_Status type)
        {
            IDictionary<string, object> toSend = CommandDefinitions
                .CreateCommand(CommandDefinitions.ClientSendTypingStatus, new List<string>() { type.ToString(), ConversationWith.Name })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        #region Event methods
        private void OnStatusChanged()
        {
            if (StatusChanged != null)
                StatusChanged(this, new EventArgs());
        }

        protected override void OnThisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Message")
            {
                if (Message == null || Message.Length == 0)
                {
                    SendTypingNotification(Typing_Status.clear);
                    IsTyping = false;
                }

                else if (!IsTyping)
                {
                    IsTyping = true;
                    SendTypingNotification(Typing_Status.typing);
                    _checkTick.Enabled = true;
                }
            }
        }

        private void OnMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Model.IsSelected && NewMessageArrived != null)
                NewMessageArrived(this, new EventArgs());
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TypingStatus" || e.PropertyName == "TypingString")
                OnPropertyChanged("TypingString");
        }

        private void OnNewUpdateEvent(NotificationModel param)
        {
            OnPropertyChanged("ConversationWith");
            OnPropertyChanged("StatusString");
            OnPropertyChanged("HasStatus");
            OnPropertyChanged("CanPost");

            var arguments = ((CharacterUpdateModel)param).Arguments;
            if (!(arguments is Models.CharacterUpdateModel.ListOperationEventArgs))
                OnStatusChanged();
        }

        private bool UpdateIsOurCharacter(NotificationModel param)
        {
            return (param is CharacterUpdateModel && ((CharacterUpdateModel)param).TargetCharacter == ConversationWith);
        }
        #endregion
        #endregion

        #region IDisposable
        override protected void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _checkTick.Dispose();
                _cooldownTimer.Dispose();
                _checkTick = null;
                _cooldownTimer = null;

                StatusChanged = null;
                NewMessageArrived = null;

                Model.Messages.CollectionChanged -= OnMessagesChanged;
                _events.GetEvent<NewUpdateEvent>().Unsubscribe(OnNewUpdateEvent);
            }

            base.Dispose(IsManaged);
        }
        #endregion
    }
}
