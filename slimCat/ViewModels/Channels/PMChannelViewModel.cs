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

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Windows.Data;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
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

        private string messageMessage;

        private DateTimeOffset noteTimeLeft;

        private bool showSubject;

        private bool isViewingProfile;

        private ProfileImage currentImage;

        private bool isViewingFullImage;

        private RelayCommand switchViewingImageCommand;

        private RelayCommand openInBrowserCommand;

        #endregion

        #region Constructors and Destructors

        public PmChannelViewModel(string name, IChatState chatState, INoteService notes, IProfileService profile)
            : base(chatState)
        {
            try
            {
                model = Container.Resolve<PmChannelModel>(name);
                Model = model;

                noteService = notes;
                notes.GetNotesAsync(name);

                profile.GetProfileDataAsync(name);

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

                AllKinks = new ListCollectionView(new ProfileKink[0]);

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
                    ChatModel.CurrentCharacter.Name, Model.Title, Model.Id, Model.Type);

                ChannelSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("ChannelSettings");
                    if (!ChannelSettings.IsChangingSettings)
                    {
                        SettingsService.UpdateSettingsFile(
                            ChannelSettings, ChatModel.CurrentCharacter.Name, Model.Title, Model.Id);
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
            get { return  IsViewingProfile ? "Profile" : isViewingChat ? "Chat" : "Notes"; }
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
                if (isViewingChat != value)
                {
                    if (!value)
                    {
                        messageMessage = Message;
                        Message = noteMessage;
                    }
                    else
                    {
                        noteMessage = Message;
                        Message = messageMessage;
                    }
                }

                isViewingChat = value;

                messageManager.OriginalCollection = value ? model.Messages : model.Notes;

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

        public bool IsViewingProfile
        {
            get { return isViewingProfile; }
            set
            {
                isViewingProfile = value;
                IsViewingChat = true;

                OnPropertyChanged("IsViewingProfile");
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

        public bool IsViewingFullImage
        {
            get { return isViewingFullImage; }
            set
            {
                isViewingFullImage = value;
                OnPropertyChanged("IsViewingFullImage");
            }
        }

        public bool IsConversationWithSelf
        {
            get { return ConversationWith != null && ConversationWith.NameEquals(ChatModel.CurrentCharacter.Name); }
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

        public ProfileImage CurrentImage
        {
            get { return currentImage; }
            set
            {
                currentImage = value;
                OnPropertyChanged("CurrentImage");
            }
        }

        public ICommand SwitchImageViewCommand

        {
            get
            {
                return switchViewingImageCommand ??
                       (switchViewingImageCommand = new RelayCommand(_ => IsViewingFullImage = !IsViewingFullImage));
            }
        }

        public ICommand OpenBrowserCommand
        {
            get
            {
                return openInBrowserCommand ??
                       (openInBrowserCommand =
                           new RelayCommand(_ => Process.Start(Constants.UrlConstants.CharacterPage + WebUtility.HtmlEncode(ConversationWith.Name))));
            }
        }

        public IList<ProfileKink> KinksInCommon
        {
            get
            {
                if (model.ProfileData == null || model.ProfileData.Kinks == null)
                    return new ProfileKink[0];

                return (from otherKinks in model.ProfileData.Kinks 
                        where otherKinks.KinkListKind == KinkListKind.Fave || otherKinks.KinkListKind == KinkListKind.Yes
                        join ourKinks in ChatModel.CurrentCharacterData.Kinks on otherKinks.Id equals ourKinks.Id
                        where ourKinks.KinkListKind == KinkListKind.Fave || ourKinks.KinkListKind == KinkListKind.Yes
                        orderby ourKinks.KinkListKind, ourKinks.Name
                        select ourKinks).ToList();
            }
        }

        public IList<ProfileKink> OurTroubleKinks
        {
            get
            {
                if (model.ProfileData == null || model.ProfileData.Kinks == null)
                    return new ProfileKink[0];

                return (from otherKinks in model.ProfileData.Kinks
                        where otherKinks.KinkListKind == KinkListKind.No
                        join ourKinks in ChatModel.CurrentCharacterData.Kinks on otherKinks.Id equals ourKinks.Id
                        where ourKinks.KinkListKind == KinkListKind.Fave || ourKinks.KinkListKind == KinkListKind.Yes
                        orderby ourKinks.KinkListKind, ourKinks.Name
                        select ourKinks).ToList();
            }
        }

        public IList<ProfileKink> TheirTroubleKinks
        {
            get
            {
                if (model.ProfileData == null || model.ProfileData.Kinks == null)
                    return new ProfileKink[0];

                return (from otherKinks in model.ProfileData.Kinks
                        where otherKinks.KinkListKind == KinkListKind.Fave || otherKinks.KinkListKind == KinkListKind.Yes
                        join ourKinks in ChatModel.CurrentCharacterData.Kinks on otherKinks.Id equals ourKinks.Id
                        where ourKinks.KinkListKind == KinkListKind.No
                        orderby ourKinks.KinkListKind, ourKinks.Name
                        select ourKinks).ToList();
            }
        } 

        public ICollectionView AllKinks { get; private set; }

        public double MatchPercent
        {
            get
            {
                if (model.ProfileData == null || model.ProfileData.Kinks == null)
                    return 0;

                var numberOfOurInterests = ChatModel.CurrentCharacterData.Kinks
                   .Where(x => x.IsCustomKink == false)
                   .Count(x => x.KinkListKind == KinkListKind.Fave || x.KinkListKind == KinkListKind.Yes);

                var numberOfTheirInterests = model.ProfileData.Kinks
                    .Where(x => x.IsCustomKink == false)
                    .Count(x => x.KinkListKind == KinkListKind.Fave || x.KinkListKind == KinkListKind.Yes);

                if (numberOfOurInterests == 0 || numberOfTheirInterests == 0)
                    return 0;

                var applicableFavorites = (from ourKinks in ChatModel.CurrentCharacterData.Kinks
                                           where ourKinks.KinkListKind == KinkListKind.Fave
                                           join theirKinks in model.ProfileData.Kinks on ourKinks.Id equals theirKinks.Id
                                           where theirKinks.KinkListKind == KinkListKind.Fave || theirKinks.KinkListKind == KinkListKind.Yes
                                           select ourKinks).Count();

                var applicableYes = (from ourKinks in ChatModel.CurrentCharacterData.Kinks
                                     where ourKinks.KinkListKind == KinkListKind.Yes
                                     join theirKinks in model.ProfileData.Kinks on ourKinks.Id equals theirKinks.Id
                                     where theirKinks.KinkListKind == KinkListKind.Fave || theirKinks.KinkListKind == KinkListKind.Yes
                                     select ourKinks).Count();

                var ourTotalFavorite = (from ourKinks in ChatModel.CurrentCharacterData.Kinks
                                        where ourKinks.KinkListKind == KinkListKind.Fave
                                        join theirKinks in model.ProfileData.Kinks on ourKinks.Id equals theirKinks.Id
                                        select ourKinks).Count();

                var ourTotalYes = (from ourKinks in ChatModel.CurrentCharacterData.Kinks
                                   where ourKinks.KinkListKind == KinkListKind.Yes
                                   join theirKinks in model.ProfileData.Kinks on ourKinks.Id equals theirKinks.Id
                                   select ourKinks).Count();

                // we weight the ratio of our favorites and yes
                var favoritePoints = 0.75*GetMatchRatio(applicableFavorites, ourTotalFavorite);
                var yesPoints = 0.25*GetMatchRatio(applicableYes, ourTotalYes);

                var subTotal = favoritePoints + yesPoints;

                // this subtracts points if we're missing a lot of interests
                var percent = (numberOfOurInterests > numberOfTheirInterests
                    ? numberOfTheirInterests/(double)numberOfOurInterests
                    : numberOfOurInterests/(double)numberOfTheirInterests);

                // this returns a number between 0 and 1, but gets close to 1 quickly
                // this makes really large disparities hurt
                var multiplier = Math.Max((1+Math.Log10(percent)), 0);

                subTotal *= multiplier;

                return subTotal > 0.5 
                    ? Math.Round((GetMatchRatio(subTotal, 1)* 100), 2) 
                    : Math.Round(subTotal*100, 2);
            }
        }

        public bool IsRoleMismatch
        {
            get
            {
                if (model.ProfileData == null || model.ProfileData.Kinks == null)
                    return false;

                var ours = ChatModel.CurrentCharacterData;
                var theirs = model.ProfileData;

                if (ours.DomSubRole == null || theirs.DomSubRole == null || ours.Position == null || theirs.Position == null)
                    return false;

                if (ours.DomSubRole.ContainsOrdinal("dominant") && theirs.DomSubRole.Contains("dominant"))
                    return true;

                if (ours.DomSubRole.ContainsOrdinal("submissive") && theirs.DomSubRole.Contains("submissive"))
                    return true;

                if (theirs.Position.ContainsOrdinal("top") && ours.Position.Contains("top"))
                    return true;

                if (ours.Position.ContainsOrdinal("bottom") && theirs.Position.Contains("bottom"))
                    return true;

                return false;
            }
        }

        public bool IsOrientationMismatch
        {
            get
            {
                if (model.ProfileData == null || model.ProfileData.Kinks == null)
                    return false;

                var ours = ChatModel.CurrentCharacterData;
                var theirs = model.ProfileData;

                var gender = ours.AdditionalTags.FirstOrDefault(x => x.Label.ContainsOrdinal("gender"));
                if (gender == null)
                    return false;

                return theirs.Kinks
                    .Where(x => x.KinkListKind == KinkListKind.No)
                    .Any(x => x.Name.StartsWith(gender.Value, StringComparison.OrdinalIgnoreCase));
            }
        }

        #endregion

        #region Methods

        private double GetMatchRatio(double one, double two)
        {
            return 1 + Math.Log10((one/two)-0.1) + 0.05;
        }

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

                if (model.ShouldViewProfile)
                {
                    IsViewingChat = IsViewingProfile = model.ShouldViewProfile;
                    model.ShouldViewProfile = false;
                }

                if (IsViewingProfile) return;
                if (model.ShouldViewNotes || (ApplicationSettings.OpenOfflineChatsInNoteView && ConversationWith.Status == StatusType.Offline))
                    IsViewingChat = model.ShouldViewNotes = false;
            }

            if (e.PropertyName == "ProfileData")
            {
                OnPropertyChanged("KinksInCommon");
                OnPropertyChanged("OurTroubleKinks");
                OnPropertyChanged("TheirTroubleKinks");
                AllKinks = new ListCollectionView(model.ProfileData.Kinks);
                AllKinks.GroupDescriptions.Add(new PropertyGroupDescription("KinkListKind"));
                AllKinks.SortDescriptions.Add(new SortDescription("KinkListKind", ListSortDirection.Ascending));
                AllKinks.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                OnPropertyChanged("AllKinks");
                OnPropertyChanged("MatchPercent");
                OnPropertyChanged("IsRoleMismatch");
                OnPropertyChanged("IsOrientationMismatch");
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

            LastMessage = Message;
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

            noteService.SendNoteAsync(Message, ConversationWith.Name, NoteSubject);
            isInNoteCoolDown = true;
            noteCooldownTimer.Enabled = true;
            noteCooldownUpdateTick.Enabled = true;

            noteTimeLeft = DateTime.Now.AddMilliseconds(noteCooldownTimer.Interval);
            OnPropertyChanged("NoteTimeLeft");
            OnPropertyChanged("CanShowNoteTimeLeft");
            OnPropertyChanged("CanPost");
            CanShowSubject = false;

            LastMessage = Message;
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

        protected override void StartLinkInDefaultBrowser(object linkToOpen)
        {
            if (ApplicationSettings.OpenProfilesInClient)
            {
                var target = (string) linkToOpen;
                if (target.ToLower().StartsWith(ConversationWith.Name.ToLower()))
                {
                    if (target.EndsWith("/notes"))
                        IsViewingChat = IsViewingProfile = false;
                    else
                        IsViewingChat = IsViewingProfile = true;

                    return;
                }
            }

            base.StartLinkInDefaultBrowser(linkToOpen);
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