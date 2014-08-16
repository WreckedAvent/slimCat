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

namespace slimCat.ViewModels
{
    #region Usings

    using System.Linq;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Timers;
    using System.Windows.Input;
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

        private Timer noteCooldownTimer = new Timer(21000);

        private Timer noteCooldownUpdateTick = new Timer(1000);

        private bool isInCoolDown;

        private bool isInNoteCoolDown;

        private bool isTyping;

        private bool isViewingChat = true;

        private FilteredCollection<IMessage, IViewableObject> messageManager;

        private int typingLengthCache;

        private readonly INoteService noteService;

        private RelayCommand @switch;

        private readonly PmChannelModel model;

        private string noteMessage;

        private DateTimeOffset noteTimeLeft;

        private bool showSubject;

        #endregion

        #region Constructors and Destructors

        public PmChannelViewModel(
            string name, IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager manager, INoteService notes)
            : base(contain, regman, events, cm, manager)
        {
            try
            {
                model = Container.Resolve<PmChannelModel>(name);
                Model = model;

                noteService = notes;
                notes.GetNotes(name);

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

                noteCooldownTimer.Elapsed += (s, e) =>
                {
                    isInNoteCoolDown = false;
                    noteCooldownTimer.Enabled = false;
                    noteCooldownUpdateTick.Enabled = false;
                    OnPropertyChanged("CanPost");
                    OnPropertyChanged("CanShowNoteTimeLeft");
                };

                noteCooldownUpdateTick.Elapsed += (s, e) => OnPropertyChanged("NoteTimeLeft");

                checkTick.Elapsed += (s, e) =>
                {
                    if (!IsTyping)
                        checkTick.Enabled = false;

                    if (!string.IsNullOrEmpty(Message) && typingLengthCache == Message.Length)
                    {
                        IsTyping = false;
                        SendTypingNotification(TypingStatus.Paused);
                        checkTick.Enabled = false;
                    }

                    if (IsTyping)
                        typingLengthCache = Message != null ? Message.Length : 0;
                };

                Model.Settings = SettingsService.GetChannelSettings(
                    cm.CurrentCharacter.Name, Model.Title, Model.Id, Model.Type);

                ChannelSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("ChannelSettings");
                    if (!ChannelSettings.IsChangingSettings)
                    {
                        SettingsService.UpdateSettingsFile(
                            ChannelSettings, cm.CurrentCharacter.Name, Model.Title, Model.Id);
                    }
                };

                messageManager = new FilteredCollection<IMessage, IViewableObject>(
                    Model.Messages, message => true);

                LoggingSection = "pm channel vm";
            }
            catch (Exception ex)
            {
                ex.Source = "PM Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Events

        public event EventHandler StatusChanged;

        #endregion

        #region Public Properties

        public bool CanPost
        {
            get
            {
                var isCooling = isViewingChat ? isInCoolDown : isInNoteCoolDown;
                return !isCooling || !ApplicationSettings.AllowTextboxDisable;
            }
        }

        public bool CanDisplayChat
        {
            get { return false; }
        }

        public bool CanDisplayAds
        {
            get { return false; }
        }

        public ICharacter ConversationWith
        {
            get { return CharacterManager.Find(Model.Id); }
        }

        public ObservableCollection<IViewableObject> CurrentMessages
        {
            get { return messageManager.Collection; }
        }

        public bool HasNotifyTerms
        {
            get { return !string.IsNullOrEmpty(ChannelSettings.NotifyTerms); }
        }

        public string Title
        {
            get { return isViewingChat ? "Chat" : "Notes"; }
        }

        public string NoteSubject
        {
            get { return model.NoteSubject; }
            set { model.NoteSubject = value; }
        }

        public bool HasStatus
        {
            get { return ConversationWith.StatusMessage.Length > 0; }
        }

        public bool IsTyping
        {
            get { return isTyping; }

            set
            {
                isTyping = value;
                OnPropertyChanged("ShouldShowPostLength");
            }
        }

        public bool CanShowSubject
        {
            get { return !IsViewingChat && showSubject; }
            set
            {
                showSubject = value;
                OnPropertyChanged("CanShowSubject");
            }
        }

        public string MaxMessageLength
        {
            get { return isViewingChat ? "50,000" : "200,000"; }
        }

        public bool IsViewingChat
        {
            get { return isViewingChat; }
            set
            {
                isViewingChat = value;

                messageManager.OriginalCollection = value ? model.Messages : model.Notes;

                var temp = Message;
                Message = noteMessage;
                noteMessage = temp;

                OnPropertyChanged("IsViewingChat");
                OnPropertyChanged("Title");
                OnPropertyChanged("CurrentMessages");
                OnPropertyChanged("MaxMessageLength");
                OnPropertyChanged("CanPost");
                OnPropertyChanged("CanShowNoteTimeLeft");
                OnPropertyChanged("CanShowSubject");
                OnPropertyChanged("EntryTextBoxIcon");
                OnPropertyChanged("EntryTextBoxLabel");
            }
        }

        public bool CanShowNoteTimeLeft
        {
            get { return !IsViewingChat && isInNoteCoolDown; }
        }

        public string NoteTimeLeft
        {
            get { return HelperConverter.DateTimeInFutureToRough(noteTimeLeft) + "remaining"; }
        }

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
                        return string.Format("Warning: {0} is currently {1}.", Model.Id,
                            ConversationWith.Status.ToString().ToLower());
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

        public ICommand SwitchCommand
        {
            get
            {
                return @switch
                       ?? (@switch = new RelayCommand(param => IsViewingChat = !IsViewingChat));
            }
        }

        public override string EntryTextBoxIcon
        {
            get
            {
                return isViewingChat
                    ? "pack://application:,,,/icons/send_chat.png"
                    : "pack://application:,,,/icons/send_note.png";
            }
        }

        public override string EntryTextBoxLabel
        {
            get
            {
                return isViewingChat
                    ? "Chat here ..."
                    : "Write a pretty note here ...";

            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                checkTick.Dispose();
                cooldownTimer.Dispose();
                noteCooldownTimer.Dispose();
                noteCooldownUpdateTick.Dispose();

                checkTick = null;
                cooldownTimer = null;
                noteCooldownTimer = null;
                noteCooldownUpdateTick = null;

                StatusChanged = null;
                Events.GetEvent<NewUpdateEvent>().Unsubscribe(OnNewUpdateEvent);

                messageManager.Dispose();
                messageManager = null;
            }

            base.Dispose(isManaged);
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TypingStatus" || e.PropertyName == "TypingString")
                OnPropertyChanged("TypingString");

            if (e.PropertyName == "IsSelected")
            {
                if (model.IsSelected == false) return;

                if (model.Notes.Count > model.LastNoteCount)
                    IsViewingChat = false;
            }

            if (e.PropertyName != "NoteSubject") return;

            OnPropertyChanged("NoteSubject");
            CanShowSubject = string.IsNullOrEmpty(NoteSubject);
        }

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

        protected override void SendMessage()
        {
            if (IsViewingChat)
                SendPrivateMessage();
            else
                SendNote();
        }

        private void SendPrivateMessage()
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

            Events.SendUserCommand(CommandDefinitions.ClientSendPm, new[] { Message, ConversationWith.Name });
            Message = string.Empty;

            isInCoolDown = true;
            cooldownTimer.Enabled = true;
            OnPropertyChanged("CanPost");
            IsTyping = false;
            checkTick.Enabled = false;
        }

        private void SendNote()
        {
            if (Message.Length > 200000)
            {
                UpdateError("You expect me to post all of that? Try something shorter!");
                return;
            }


            if (isInNoteCoolDown)
            {
                UpdateError("Spamming isn't nice!");
                return;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                UpdateError("Hmm. I can't send nothing.");
                return;
            }

            noteService.SendNote(Message, ConversationWith.Name, NoteSubject);
            isInNoteCoolDown = true;
            noteCooldownTimer.Enabled = true;
            noteCooldownUpdateTick.Enabled = true;

            noteTimeLeft = DateTime.Now.AddMilliseconds(noteCooldownTimer.Interval);
            OnPropertyChanged("NoteTimeLeft");
            OnPropertyChanged("CanShowNoteTimeLeft");
            OnPropertyChanged("CanPost");
            CanShowSubject = false;

            Message = string.Empty;
        }

        private void OnNewUpdateEvent(NotificationModel param)
        {
            OnPropertyChanged("ConversationWith");
            OnPropertyChanged("StatusString");
            OnPropertyChanged("HasStatus");
            OnPropertyChanged("CanPost");
            OnPropertyChanged("TypingString");

            var arguments = ((CharacterUpdateModel) param).Arguments;
            if (!(arguments is PromoteDemoteEventArgs))
                OnStatusChanged();
        }

        private void OnStatusChanged()
        {
            if (StatusChanged != null)
                StatusChanged(this, new EventArgs());
        }

        private void SendTypingNotification(TypingStatus type)
        {
            if (!IsViewingChat) return;

            Events.SendUserCommand(CommandDefinitions.ClientSendTypingStatus,
                new[] {type.ToString().ToLower(), ConversationWith.Name});
        }

        private bool UpdateIsOurCharacter(NotificationModel param)
        {
            var updateModel = param as CharacterUpdateModel;
            if (updateModel == null) return false;

            var args = updateModel.TargetCharacter;
            return args.Name.Equals(ConversationWith.Name, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}