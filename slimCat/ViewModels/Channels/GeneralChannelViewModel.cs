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
        #region Fields

        private readonly GenderSettingsModel genderSettings;
        private readonly FilteredMessageCollection messageManager;

        private readonly GenericSearchSettingsModel searchSettings;
        private readonly IList<string> thisDingTerms = new List<string>();

        private Timer adFlood = new Timer(602000);

        private string adMessage = string.Empty;

        private bool autoPostAds;

        private bool hasNewAds;

        private bool hasNewMessages;

        private bool isDisplayingChat;

        private bool isInCoolDownAd;

        private bool isInCoolDownMessage;

        private bool isSearching;

        private Timer messageFlood = new Timer(500);

        private RelayCommand @switch;

        private RelayCommand switchSearch;

        private DateTimeOffset timeLeftAd;

        private Timer update = new Timer(1000);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GeneralChannelViewModel" /> class.
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

                messageFlood.Elapsed += (s, e) =>
                    {
                        isInCoolDownMessage = false;
                        messageFlood.Enabled = false;
                        OnPropertyChanged("CanPost");
                    };

                adFlood.Elapsed += (s, e) =>
                    {
                        isInCoolDownAd = false;
                        adFlood.Enabled = false;
                        OnPropertyChanged("CanPost");
                        OnPropertyChanged("CannotPost");
                        OnPropertyChanged("ShouldShowAutoPost");
                        if (autoPostAds)
                            SendAutoAd();
                    };

                update.Elapsed += (s, e) =>
                    {
                        if (!Model.IsSelected)
                            return;

                        if (CannotPost)
                            OnPropertyChanged("TimeLeft");

                        OnPropertyChanged("StatusString");
                    };

                ChannelManagementViewModel.PropertyChanged += (s, e) => OnPropertyChanged("ChannelManagementViewModel");

                update.Enabled = true;

                var newSettings = SettingsDaemon.GetChannelSettings(
                    cm.CurrentCharacter.Name, Model.Title, Model.Id, Model.Type);
                Model.Settings = newSettings;

                ChannelSettings.Updated += (s, e) =>
                    {
                        OnPropertyChanged("ChannelSettings");
                        OnPropertyChanged("HasNotifyTerms");
                        if (!ChannelSettings.IsChangingSettings)
                        {
                            SettingsDaemon.UpdateSettingsFile(
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

        /// <summary>
        ///     Gets or sets a value indicating whether auto post.
        /// </summary>
        public bool AutoPost
        {
            get { return autoPostAds; }

            set
            {
                autoPostAds = value;
                OnPropertyChanged("AutoPost");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether can post.
        /// </summary>
        public bool CanPost
        {
            get
            {
                return (IsDisplayingChat && !isInCoolDownMessage)
                       || (IsDisplayingAds && !isInCoolDownAd);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether can switch.
        /// </summary>
        public bool CanSwitch
        {
            get
            {
                if (IsDisplayingChat && ShouldDisplayAds)
                    return true;

                return !IsDisplayingChat && ShouldDisplayChat;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether cannot post.
        /// </summary>
        public bool CannotPost
        {
            get { return !CanPost; }
        }

        /// <summary>
        ///     Gets the channel management view model.
        /// </summary>
        public ChannelManagementViewModel ChannelManagementViewModel { get; private set; }

        /// <summary>
        ///     Gets the chat content string.
        /// </summary>
        public string ChatContentString
        {
            get { return IsDisplayingChat ? "Chat" : "Ads"; }
        }

        /// <summary>
        ///     Gets the current messages.
        /// </summary>
        public ObservableCollection<IViewableObject> CurrentMessages
        {
            get { return messageManager.Collection; }
        }

        /// <summary>
        ///     Gets the gender settings.
        /// </summary>
        public GenderSettingsModel GenderSettings
        {
            get { return genderSettings; }
        }

        /// <summary>
        ///     Used for channel settings to display settings related to notify terms
        /// </summary>
        public bool HasNotifyTerms
        {
            get { return !string.IsNullOrEmpty(ChannelSettings.NotifyTerms); }
        }

        /// <summary>
        ///     Gets a value indicating whether is chatting.
        /// </summary>
        public bool IsChatting
        {
            get { return !IsSearching; }
        }

        /// <summary>
        ///     Gets a value indicating whether is displaying Ads.
        /// </summary>
        public bool IsDisplayingAds
        {
            get { return !IsDisplayingChat; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is displaying chat.
        /// </summary>
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
            get { return (Model.Mode == ChannelMode.Both || Model.Mode == ChannelMode.Ads) && Model.Type != ChannelType.PrivateMessage; }
        }

        public bool CanDisplayChat
        {
            get { return Model.Mode == ChannelMode.Both || Model.Mode == ChannelMode.Chat; }
        }

        /// <summary>
        ///     Gets a value indicating whether is not searching.
        /// </summary>
        public bool IsNotSearching
        {
            get { return !IsSearching; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is searching.
        /// </summary>
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

        /// <summary>
        ///     Gets the motd.
        /// </summary>
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
                if (IsDisplayingAds && CanDisplayAds)
                    hasNewMessages = value;
                else if (!IsDisplayingAds && CanDisplayChat)
                    hasNewAds = value;

                OnPropertyChanged("OtherTabHasMessages");
                OnPropertyChanged("StatusString");
            }
        }

        /// <summary>
        ///     Gets the search settings.
        /// </summary>
        public GenericSearchSettingsModel SearchSettings
        {
            get { return searchSettings; }
        }

        /// <summary>
        ///     Gets a value indicating whether should display Ads.
        /// </summary>
        public bool ShouldDisplayAds
        {
            get { return (Model.Mode == ChannelMode.Both) || (Model.Mode == ChannelMode.Ads); }
        }

        /// <summary>
        ///     Gets a value indicating whether should display chat.
        /// </summary>
        public bool ShouldDisplayChat
        {
            get { return (Model.Mode == ChannelMode.Both) || (Model.Mode == ChannelMode.Chat); }
        }

        /// <summary>
        ///     Gets a value indicating whether should show auto post.
        /// </summary>
        public bool ShouldShowAutoPost
        {
            get
            {
                if (!isInCoolDownAd)
                    return IsDisplayingAds;

                return IsDisplayingAds && !string.IsNullOrEmpty(Message);
            }
        }

        /// <summary>
        ///     This is used for the channel settings, if it should show settings like 'notify when this character is mentioned'
        /// </summary>
        public bool ShowAllSettings
        {
            get { return true; }
        }

        /// <summary>
        ///     Gets the status string.
        /// </summary>
        public string StatusString
        {
            get
            {
                if (IsDisplayingAds && AutoPost && isInCoolDownAd)
                    return "Auto post Ads enabled";

                if (!string.IsNullOrEmpty(Message))
                {
                    return string.Format(
                        "{0} / {1} characters", Message.Length, IsDisplayingChat ? "4,096" : "50,000");
                }

                if (hasNewAds && IsDisplayingChat)
                    return "This channel has new ad(s).";

                if (hasNewMessages && IsDisplayingAds)
                    return "This channel has new message(s).";

                return string.Empty;
            }
        }

        /// <summary>
        ///     Gets the switch command.
        /// </summary>
        public ICommand SwitchCommand
        {
            get
            {
                return @switch
                       ?? (@switch = new RelayCommand(param => IsDisplayingChat = !IsDisplayingChat));
            }
        }

        /// <summary>
        ///     Gets the switch search command.
        /// </summary>
        public ICommand SwitchSearchCommand
        {
            get
            {
                return switchSearch ?? (switchSearch = new RelayCommand(
                    delegate { IsSearching = !IsSearching; }));
            }
        }

        /// <summary>
        ///     Gets the time left.
        /// </summary>
        public string TimeLeft
        {
            get { return HelperConverter.DateTimeInFutureToRough(timeLeftAd) + "left"; }
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

        /// <summary>
        ///     The send auto ad.
        /// </summary>
        public void SendAutoAd()
        {
            var messageToSend = IsDisplayingChat ? adMessage : Message;
            if (messageToSend == null)
                UpdateError("There is no ad to auto-post!");

            var toSend =
                CommandDefinitions.CreateCommand(
                    CommandDefinitions.ClientSendChannelAd, new List<string> {messageToSend}, Model.Id)
                    .ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(toSend);
            timeLeftAd = DateTimeOffset.Now.AddMinutes(10).AddSeconds(2);

            isInCoolDownAd = true;
            adFlood.Start();
            OnPropertyChanged("CanPost");
            OnPropertyChanged("CannotPost");
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

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="isManaged">
        ///     The is Managed.
        /// </param>
        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                update.Dispose();
                update = null;

                adFlood.Dispose();
                adFlood = null;

                messageFlood.Dispose();
                messageFlood = null;

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

        /// <summary>
        ///     The send message.
        /// </summary>
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

            var toSend =
                CommandDefinitions.CreateCommand(command, new List<string> {Message}, Model.Id)
                    .ToDictionary();

            Events.GetEvent<UserCommandEvent>().Publish(toSend);

            if (!autoPostAds || IsDisplayingChat)
                Message = null;

            if (IsDisplayingChat)
            {
                isInCoolDownMessage = true;
                OnPropertyChanged("CanPost");

                messageFlood.Enabled = true;
            }
            else
            {
                timeLeftAd = DateTime.Now.AddMinutes(10).AddSeconds(2);

                isInCoolDownAd = true;
                OnPropertyChanged("CanPost");
                OnPropertyChanged("CannotPost");
                OnPropertyChanged("ShouldShowAutoPost");

                adFlood.Enabled = true;
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