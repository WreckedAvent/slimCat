#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralChannelViewModel.cs">
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

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Services;
    using Utilities;
    using Views;

    #endregion

    /// <summary>
    ///     The general channel view model. This manages bindings for general channels, e.g anything that isn't a Pm or the
    ///     'home' tab.
    /// </summary>
    public class GeneralChannelViewModel : ChannelViewModelBase
    {
        #region Constants

        private const int MaxAutoPosts = 6;

        #endregion

        #region Fields

        private readonly GenderSettingsModel genderSettings;

        private readonly FilteredMessageCollection messageManager;

        private readonly GenericSearchSettingsModel searchSettings;

        private readonly IList<string> thisDingTerms = new List<string>();

        private Timer adFloodTimer = new Timer(602000);

        private string adMessage = string.Empty;

        private bool autoPostAds;

        private int autoPostCount;

        private DateTimeOffset autoTimeLeft;

        private bool hasNewAds;

        private bool hasNewMessages;

        private bool isDisplayingChat;

        private bool isInCoolDownAd;

        private bool isInCoolDownMessage;

        private bool isSearching;

        private Timer messageFloodTimer = new Timer(500);

        private RelayCommand @switch;

        private RelayCommand switchSearch;

        private DateTimeOffset timeLeftAd;

        private Timer updateTimer = new Timer(1000);

        #endregion

        #region Constructors and Destructor

        public GeneralChannelViewModel(
            string name, IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm,
            ICharacterManager manager)
            : base(contain, regman, events, cm, manager)
        {
            try
            {
                Model = ChatModel.CurrentChannels.FirstOrDefault(chan => chan.Id == name)
                        ?? ChatModel.AllChannels.First(chan => chan.Id == name);
                Model.ThrowIfNull("this.Model");

                var safeName = HelperConverter.EscapeSpaces(name);

                Container.RegisterType<object, GeneralChannelView>(safeName, new InjectionConstructor(this));

                isDisplayingChat = ShouldDisplayChat;

                ChannelManagementViewModel = new ChannelManagementViewModel(Events, Model as GeneralChannelModel);

                // instance our management vm
                Model.Messages.CollectionChanged += OnMessagesChanged;
                Model.Ads.CollectionChanged += OnAdsChanged;
                Model.PropertyChanged += OnModelPropertyChanged;

                searchSettings = new GenericSearchSettingsModel();
                genderSettings = new GenderSettingsModel();

                messageManager =
                    new FilteredMessageCollection(
                        isDisplayingChat
                            ? Model.Messages
                            : Model.Ads,
                        MeetsFilter,
                        ConstantFilter,
                        IsDisplayingAds);

                genderSettings.Updated += (s, e) => OnPropertyChanged("GenderSettings");

                SearchSettings.Updated += (s, e) => OnPropertyChanged("SearchSettings");

                messageFloodTimer.Elapsed += (s, e) =>
                    {
                        isInCoolDownMessage = false;
                        messageFloodTimer.Enabled = false;
                        OnPropertyChanged("CanPost");
                    };

                adFloodTimer.Elapsed += (s, e) =>
                    {
                        isInCoolDownAd = false;
                        adFloodTimer.Enabled = false;
                        OnPropertyChanged("CanPost");
                        OnPropertyChanged("CannotPost");
                        OnPropertyChanged("ShouldShowAutoPost");
                        if (autoPostAds)
                            SendAutoAd();
                        else
                            autoPostCount = 0;
                    };

                updateTimer.Elapsed += (s, e) =>
                    {
                        if (!Model.IsSelected)
                            return;

                        if (CannotPost)
                        {
                            OnPropertyChanged("TimeLeft");
                            OnPropertyChanged("AutoTimeLeft");
                        }

                        OnPropertyChanged("StatusString");
                    };

                ChannelManagementViewModel.PropertyChanged += (s, e) => OnPropertyChanged("ChannelManagementViewModel");

                updateTimer.Enabled = true;

                var newSettings = SettingsService.GetChannelSettings(
                    cm.CurrentCharacter.Name, Model.Title, Model.Id, Model.Type);
                Model.Settings = newSettings;

                ChannelSettings.Updated += (s, e) =>
                    {
                        OnPropertyChanged("ChannelSettings");
                        OnPropertyChanged("HasNotifyTerms");
                        if (!ChannelSettings.IsChangingSettings)
                        {
                            SettingsService.UpdateSettingsFile(
                                ChannelSettings, cm.CurrentCharacter.Name, Model.Title, Model.Id);
                        }
                    };

                PropertyChanged += OnPropertyChanged;

                Events.GetEvent<NewUpdateEvent>().Subscribe(UpdateChat);
            }
            catch (Exception ex)
            {
                ex.Source = "General Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Properties

        public bool AutoPost
        {
            get { return autoPostAds; }

            set
            {
                autoPostAds = value;

                // set the interval time to however many minutes in miliseconds
                // plus two seconds in miliseconds
                if (!isInCoolDownAd)
                    adFloodTimer.Interval = Model.Settings.AutopostTime*60*1000 + 2000;

                OnPropertyChanged("AutoPost");
                OnPropertyChanged("CanShowAutoTimeLeft");
            }
        }

        public bool CanPost
        {
            get
            {
                return (IsDisplayingChat && !isInCoolDownMessage)
                       || (IsDisplayingAds && !isInCoolDownAd);
            }
        }

        public bool CanShowAutoTimeLeft
        {
            get { return IsDisplayingAds && CannotPost && AutoPost; }
        }

        public bool CanSwitch
        {
            get
            {
                if (IsDisplayingChat && ShouldDisplayAds)
                    return true;

                return !IsDisplayingChat && ShouldDisplayChat;
            }
        }

        public bool CannotPost
        {
            get { return !CanPost; }
        }

        public ChannelManagementViewModel ChannelManagementViewModel { get; private set; }


        public string ChatContentString
        {
            get { return IsDisplayingChat ? "Chat" : "Ads"; }
        }

        public ObservableCollection<IViewableObject> CurrentMessages
        {
            get { return messageManager.Collection; }
        }

        public GenderSettingsModel GenderSettings
        {
            get { return genderSettings; }
        }

        public bool HasNotifyTerms
        {
            get { return !string.IsNullOrEmpty(ChannelSettings.NotifyTerms); }
        }


        public bool IsChatting
        {
            get { return !IsSearching; }
        }

        public bool IsDisplayingAds
        {
            get { return !IsDisplayingChat; }
        }

        public bool IsDisplayingChat
        {
            get { return isDisplayingChat; }

            set
            {
                if (isDisplayingChat == value)
                    return;

                isDisplayingChat = value;

                var temp = Message;
                Message = adMessage;
                adMessage = temp;

                messageManager.OriginalCollection = value ? Model.Messages : Model.Ads;

                OnPropertyChanged("IsDisplayingChat");
                OnPropertyChanged("IsDisplayingAds");
                OnPropertyChanged("ChatContentString");
                OnPropertyChanged("MessageMax");
                OnPropertyChanged("CanPost");
                OnPropertyChanged("CannotPost");
                OnPropertyChanged("ShouldShowAutoPost");
                OnPropertyChanged("SwitchChannelTypeString");
                OtherTabHasMessages = false;
            }
        }

        public bool CanDisplayAds
        {
            get
            {
                return (Model.Mode == ChannelMode.Both || Model.Mode == ChannelMode.Ads) &&
                       Model.Type != ChannelType.PrivateMessage;
            }
        }

        public bool CanDisplayChat
        {
            get { return Model.Mode == ChannelMode.Both || Model.Mode == ChannelMode.Chat; }
        }

        public bool IsNotSearching
        {
            get { return !IsSearching; }
        }

        public bool IsSearching
        {
            get { return isSearching; }

            set
            {
                if (isSearching == value)
                    return;

                isSearching = value;
                OnPropertyChanged("IsSearching");
                OnPropertyChanged("SearchSwitchMessageString");
                OnPropertyChanged("IsChatting");
                OnPropertyChanged("IsNotSearching");
            }
        }

        public string Description
        {
            get { return ((GeneralChannelModel) Model).Description; }
        }

        /// <summary>
        ///     if we're displaying the channel's messages, if there's a new ad (or vice-versa)
        /// </summary>
        public bool OtherTabHasMessages
        {
            get { return IsDisplayingChat ? hasNewAds : hasNewMessages; }

            set
            {
                if (IsDisplayingAds && CanDisplayChat)
                    hasNewMessages = value;
                else if (!IsDisplayingAds && CanDisplayAds)
                    hasNewAds = value;

                OnPropertyChanged("OtherTabHasMessages");
                OnPropertyChanged("StatusString");
            }
        }

        public GenericSearchSettingsModel SearchSettings
        {
            get { return searchSettings; }
        }

        public bool ShouldDisplayAds
        {
            get { return (Model.Mode == ChannelMode.Both) || (Model.Mode == ChannelMode.Ads); }
        }

        public bool ShouldDisplayChat
        {
            get { return (Model.Mode == ChannelMode.Both) || (Model.Mode == ChannelMode.Chat); }
        }

        public bool ShouldShowAutoPost
        {
            get
            {
                if (!isInCoolDownAd)
                    return IsDisplayingAds;

                return IsDisplayingAds && !string.IsNullOrEmpty(Message);
            }
        }

        public bool ShowAllSettings
        {
            get { return true; }
        }

        public string StatusString
        {
            get
            {
                if (IsDisplayingAds && AutoPost && isInCoolDownAd)
                    return "Auto posting:";

                if (!string.IsNullOrEmpty(Message))
                {
                    return string.Format(
                        "{0} / {1} characters", Message.Length, IsDisplayingChat ? "4,096" : "50,000");
                }

                if (OtherTabHasMessages && IsDisplayingChat)
                    return "There are new ad(s).";

                if (OtherTabHasMessages && IsDisplayingAds)
                    return "There are new message(s).";

                return string.Empty;
            }
        }

        public ICommand SwitchCommand
        {
            get
            {
                return @switch
                       ?? (@switch = new RelayCommand(param => IsDisplayingChat = !IsDisplayingChat));
            }
        }

        public ICommand SwitchSearchCommand
        {
            get
            {
                return switchSearch ?? (switchSearch = new RelayCommand(
                    delegate { IsSearching = !IsSearching; }));
            }
        }

        /// <summary>
        ///     Gets the time left before the next ad can be posted.
        /// </summary>
        public string TimeLeft
        {
            get { return HelperConverter.DateTimeInFutureToRough(timeLeftAd) + "until next"; }
        }

        public string AutoTimeLeft
        {
            get { return HelperConverter.DateTimeInFutureToRough(autoTimeLeft) + "until disabled"; }
        }

        #endregion

        #region Properties

        private IEnumerable<string> ThisDingTerms
        {
            get
            {
                const int characterNameOffset = 2; // how many ding terms we have to offset for the character's name
                var count = thisDingTerms.Count;
                var shouldBe = ApplicationSettings.GlobalNotifyTermsList.Count()
                               + Model.Settings.EnumerableTerms.Count() + characterNameOffset;

                if (count != shouldBe)
                {
                    thisDingTerms.Clear();

                    foreach (var term in ApplicationSettings.GlobalNotifyTermsList)
                        thisDingTerms.Add(term);

                    foreach (var term in Model.Settings.EnumerableTerms)
                        thisDingTerms.Add(term);

                    thisDingTerms.Add(ChatModel.CurrentCharacter.Name);
                    thisDingTerms.Add(ChatModel.CurrentCharacter.Name + "'s");
                }

                return thisDingTerms.Distinct().Where(term => !string.IsNullOrWhiteSpace(term));
            }
        }

        #endregion

        #region Public Methods and Operators

        public void SendAutoAd()
        {
            var messageToSend = IsDisplayingChat ? adMessage : Message;
            if (messageToSend == null)
            {
                UpdateError("There is no ad to auto-post!");
                AutoPost = false;
                isInCoolDownAd = false;
                return;
            }

            var last = Model.Ads.Last();
            if (last != null)
            {
                if (!last.Poster.NameEquals(ChatModel.CurrentCharacter.Name))
                    Events.SendUserCommand(CommandDefinitions.ClientSendChannelAd, new[] {messageToSend}, Model.Id);
            }
            else
                Events.SendUserCommand(CommandDefinitions.ClientSendChannelAd, new[] {messageToSend}, Model.Id);

            adFloodTimer.Interval = Model.Settings.AutopostTime*60*1000 + 2000;
            timeLeftAd = DateTimeOffset.Now.AddMilliseconds(adFloodTimer.Interval);

            isInCoolDownAd = true;
            adFloodTimer.Start();

            OnPropertyChanged("CanPost");
            OnPropertyChanged("CannotPost");

            autoPostCount++;

            if (autoPostCount < MaxAutoPosts) return;

            autoTimeLeft = DateTime.Now.AddMilliseconds(adFloodTimer.Interval*(MaxAutoPosts - autoPostCount));
            AutoPost = false;
            OnPropertyChanged("CanShowAutoTimeLeft");
            autoPostCount = 0;
        }

        #endregion

        #region Methods

        protected override void InvertButton(object arguments)
        {
            var args = arguments as string;
            if (args == null)
                return;

            if (args.Equals("Messages"))
            {
                ChannelSettings.MessageNotifyOnlyForInteresting =
                    !ChannelSettings.MessageNotifyOnlyForInteresting;
            }

            if (args.Equals("PromoteDemote"))
            {
                ChannelSettings.PromoteDemoteNotifyOnlyForInteresting =
                    !ChannelSettings.PromoteDemoteNotifyOnlyForInteresting;
            }

            if (args.Equals("JoinLeave"))
            {
                ChannelSettings.JoinLeaveNotifyOnlyForInteresting =
                    !ChannelSettings.JoinLeaveNotifyOnlyForInteresting;
            }

            OnPropertyChanged("ChannelSettings");
        }

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                updateTimer.Dispose();
                updateTimer = null;

                adFloodTimer.Dispose();
                adFloodTimer = null;

                messageFloodTimer.Dispose();
                messageFloodTimer = null;

                Events.GetEvent<NewUpdateEvent>().Unsubscribe(UpdateChat);
                Model.Messages.CollectionChanged -= OnMessagesChanged;
                Model.Ads.CollectionChanged -= OnAdsChanged;
                PropertyChanged -= OnPropertyChanged;

                var model = (GeneralChannelModel) Model;
                model.Description = null;
                model.CharacterManager.Dispose();
            }

            base.Dispose(isManaged);
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Description":
                    OnPropertyChanged("Description");
                    break;
                case "Type":
                    OnPropertyChanged("ChannelTypeString"); // fixes laggy room type change
                    break;
                case "Moderators":
                    OnPropertyChanged("HasPermissions"); // fixes laggy permissions
                    break;
                case "Mode":
                    if (Model.Mode == ChannelMode.Ads && IsDisplayingChat)
                        IsDisplayingChat = false;

                    if (Model.Mode == ChannelMode.Chat && IsDisplayingAds)
                        IsDisplayingChat = true;

                    OnPropertyChanged("CanSwitch");
                    break;
            }
        }

        protected override void SendMessage()
        {
            if (IsSearching)
                return;

            // if we're not searching, treat this input normal
            if ((IsDisplayingChat && Message.Length > 4096)
                || (IsDisplayingAds && Message.Length > 50000))
            {
                UpdateError("You expect me to post all of that?! How about you post less, huh?");
                return;
            }

            if ((isInCoolDownAd && IsDisplayingAds) || (isInCoolDownMessage && IsDisplayingChat))
            {
                UpdateError("Cool your engines. Wait a little before you post again.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Message))
            {
                UpdateError("I'm sure you didn't mean to do that.");
                return;
            }

            var command = IsDisplayingChat
                ? CommandDefinitions.ClientSendChannelMessage
                : CommandDefinitions.ClientSendChannelAd;

            Events.SendUserCommand(command, new[] {Message}, Model.Id);

            if (!autoPostAds || IsDisplayingChat)
                Message = null;

            if (IsDisplayingChat)
            {
                isInCoolDownMessage = true;
                OnPropertyChanged("CanPost");

                messageFloodTimer.Enabled = true;
            }
            else
            {
                timeLeftAd = DateTime.Now.AddMilliseconds(adFloodTimer.Interval);
                autoTimeLeft = DateTime.Now.AddMilliseconds(adFloodTimer.Interval*MaxAutoPosts);

                isInCoolDownAd = true;
                OnPropertyChanged("CanPost");
                OnPropertyChanged("CannotPost");
                OnPropertyChanged("ShouldShowAutoPost");
                OnPropertyChanged("CanShowAutoTimeLeft");

                adFloodTimer.Start();
            }
        }

        private bool MeetsFilter(IMessage message)
        {
            return message.MeetsFilters(
                GenderSettings, SearchSettings, CharacterManager, ChatModel.CurrentChannel as GeneralChannelModel);
        }

        private bool ConstantFilter(IMessage message)
        {
            if (message.Type == MessageType.Ad)
                return !CharacterManager.IsOnList(message.Poster.Name, ListKind.NotInterested);
            return true;
        }

        private void OnAdsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsDisplayingChat)
                OtherTabHasMessages = e.NewItems != null && e.NewItems.Count > 0;
        }

        private void OnMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsDisplayingAds)
            {
                OtherTabHasMessages =
                    e.Action != NotifyCollectionChangedAction.Reset
                    &&
                    e.Action != NotifyCollectionChangedAction.Remove
                    &&
                    e.NewItems
                        .Cast<IMessage>()
                        .Where(m => m != null)
                        .Where(MeetsFilter)
                        .Any();
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Message":
                    OnPropertyChanged("StatusString"); // keep the counter updated
                    break;
                case "SearchSettings":
                {
                    if (!SearchSettings.IsChangingSettings)
                        messageManager.IsFiltering = isSearching;
                    break;
                }
                case "IsSearching":
                {
                    messageManager.IsFiltering = isSearching;
                    break;
                }
            }
        }

        private void UpdateChat(NotificationModel newUpdate)
        {
            var updateModel = newUpdate as CharacterUpdateModel;
            if (updateModel == null)
                return;

            var args = updateModel.Arguments as CharacterUpdateModel.ListChangedEventArgs;
            if (args == null)
                return;

            messageManager.RebuildItems();
        }

        #endregion
    }
}