#region Copyright

// <copyright file="GeneralChannelViewModel.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
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

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows;
    using System.Windows.Input;
    using Libraries;
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

        #region Constructors and Destructor

        public GeneralChannelViewModel(string name, IChatState chatState)
            : base(chatState)
        {
            try
            {
                Model = ChatModel.CurrentChannels.FirstOrDefault(chan => chan.Id == name)
                        ?? ChatModel.AllChannels.First(chan => chan.Id == name);
                Model.ThrowIfNull("this.Model");

                var safeName = StringExtensions.EscapeSpaces(name);

                Container.RegisterType<object, GeneralChannelView>(safeName, new InjectionConstructor(this));

                isDisplayingChat = ShouldDisplayChat;

                ChannelManagementViewModel = new ChannelManagementViewModel(Events, (GeneralChannelModel) Model);

                // instance our management vm
                Model.Messages.CollectionChanged += OnMessagesChanged;
                Model.Ads.CollectionChanged += OnAdsChanged;
                Model.PropertyChanged += OnModelPropertyChanged;

                SearchSettings = new GenericSearchSettingsModel();
                GenderSettings = new GenderSettingsModel();

                messageManager =
                    new FilteredMessageCollection(
                        isDisplayingChat
                            ? Model.Messages
                            : Model.Ads,
                        MeetsFilter,
                        ConstantFilter,
                        IsDisplayingAds);

                GenderSettings.Updated += (s, e) => OnPropertyChanged("GenderSettings");

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
                    ChatModel.CurrentCharacter.Name, Model.Title, Model.Id, Model.Type);
                Model.Settings = newSettings;

                ChannelSettings.Updated += (s, e) =>
                {
                    OnPropertyChanged("ChannelSettings");
                    OnPropertyChanged("HasNotifyTerms");
                    if (!ChannelSettings.IsChangingSettings)
                    {
                        SettingsService.UpdateSettingsFile(
                            ChannelSettings, ChatModel.CurrentCharacter.Name, Model.Title, Model.Id);
                    }
                };

                PropertyChanged += OnPropertyChanged;

                updateSearch = DeferredAction.Create(() => messageManager.IsFiltering = isSearching);

                Events.GetEvent<NewUpdateEvent>().Subscribe(UpdateChat);

                LoggingSection = "general chan vm";

                if (!string.IsNullOrWhiteSpace(ChannelSettings.LastMessage))
                {
                    Message = ChannelSettings.LastMessage;
                    ChannelSettings.LastMessage = null;
                }
                SearchSettings.ShowOffline = true;

                Application.Current.Dispatcher.Invoke(
                    () => Application.Current.MainWindow.Deactivated += SetLastMessageMark);
            }
            catch (Exception ex)
            {
                ex.Source = "General Channel ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Properties

        public bool ShowChannelDescription
        {
            get { return showChannelDescription; }
            set
            {
                showChannelDescription = value;
                OnPropertyChanged();
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
                {
                    Events.SendUserCommand(CommandDefinitions.ClientSendChannelAd, new[] {messageToSend}, Model.Id);
                    Log("sending auto-ad");
                }
            }
            else
            {
                Events.SendUserCommand(CommandDefinitions.ClientSendChannelAd, new[] {messageToSend}, Model.Id);
                Log("sending auto-ad");
            }

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

        #region Fields

        private readonly FilteredMessageCollection messageManager;

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

        private bool showChannelDescription;

        private RelayCommand @switch;

        private RelayCommand switchSearch;

        private DateTimeOffset timeLeftAd;

        private Timer updateTimer = new Timer(1000);

        private readonly DeferredAction updateSearch;

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

                OnPropertyChanged();
                OnPropertyChanged("CanShowAutoTimeLeft");
            }
        }

        public bool CanPost
        {
            get
            {
                var canPostChat = isDisplayingChat &&
                                  (!isInCoolDownMessage || !ApplicationSettings.AllowTextboxDisable);
                var canPostAd = IsDisplayingAds && !isInCoolDownAd;

                return canPostChat || canPostAd;
            }
        }

        public bool CanShowAutoTimeLeft => IsDisplayingAds && CannotPost && AutoPost;

        public bool CanSwitch
        {
            get
            {
                if (IsDisplayingChat && ShouldDisplayAds)
                    return true;

                return !IsDisplayingChat && ShouldDisplayChat;
            }
        }

        public bool CannotPost => !CanPost;

        public ChannelManagementViewModel ChannelManagementViewModel { get; }

        public string ChatContentString => IsDisplayingChat ? "Chat" : "Ads";

        public ObservableCollection<IViewableObject> CurrentMessages => messageManager.Collection;

        public GenderSettingsModel GenderSettings { get; }

        public bool HasNotifyTerms => !string.IsNullOrEmpty(ChannelSettings.NotifyTerms);

        public bool IsChatting => !IsSearching;

        public bool IsDisplayingAds => !IsDisplayingChat;

        public bool IsDisplayingChat
        {
            get { return isDisplayingChat; }

            set
            {
                if (isDisplayingChat == value)
                    return;

                Log("now showing " + (isDisplayingChat ? "chat" : "ads"));

                isDisplayingChat = value;

                var temp = Message;
                Message = adMessage;
                adMessage = temp;

                messageManager.OriginalCollection = value ? Model.Messages : Model.Ads;

                OnPropertyChanged();
                OnPropertyChanged("IsDisplayingAds");
                OnPropertyChanged("ChatContentString");
                OnPropertyChanged("MessageMax");
                OnPropertyChanged("CanPost");
                OnPropertyChanged("CannotPost");
                OnPropertyChanged("ShouldShowAutoPost");
                OnPropertyChanged("SwitchChannelTypeString");
                OnPropertyChanged("EntryTextBoxLabel");
                OnPropertyChanged("EntryTextBoxIcon");
                OtherTabHasMessages = false;
            }
        }

        public bool CanDisplayAds
            =>
                (Model.Mode == ChannelMode.Both || Model.Mode == ChannelMode.Ads) &&
                Model.Type != ChannelType.PrivateMessage;

        public bool CanDisplayChat => Model.Mode == ChannelMode.Both || Model.Mode == ChannelMode.Chat;

        public bool IsNotSearching => !IsSearching;

        public bool IsSearching
        {
            get { return isSearching; }

            set
            {
                if (isSearching == value)
                    return;

                Log("now " + (isSearching ? "searching" : "chatting"));

                isSearching = value;
                OnPropertyChanged();
                OnPropertyChanged("SearchSwitchMessageString");
                OnPropertyChanged("IsChatting");
                OnPropertyChanged("IsNotSearching");
            }
        }

        public string Description => ((GeneralChannelModel) Model).Description;

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

                OnPropertyChanged();
                OnPropertyChanged("StatusString");
            }
        }

        public GenericSearchSettingsModel SearchSettings { get; }

        public bool ShouldDisplayAds => (Model.Mode == ChannelMode.Both) || (Model.Mode == ChannelMode.Ads);

        public bool ShouldDisplayChat => (Model.Mode == ChannelMode.Both) || (Model.Mode == ChannelMode.Chat);

        public bool ShouldShowAutoPost
        {
            get
            {
                if (!isInCoolDownAd)
                    return IsDisplayingAds;

                return IsDisplayingAds && !string.IsNullOrEmpty(Message);
            }
        }

        public bool ShowAllSettings => true;

        public string StatusString
        {
            get
            {
                if (IsDisplayingAds && AutoPost && isInCoolDownAd)
                    return "Auto posting:";

                if (!string.IsNullOrEmpty(Message))
                {
                    return $"{Message.Length} / {(IsDisplayingChat ? "4,096" : "50,000")} characters";
                }

                if (OtherTabHasMessages && IsDisplayingChat)
                    return "There are new ad(s).";

                if (OtherTabHasMessages && IsDisplayingAds)
                    return "There are new message(s).";

                return string.Empty;
            }
        }

        public ICommand SwitchCommand
            => @switch ?? (@switch = new RelayCommand(_ => IsDisplayingChat = !IsDisplayingChat));

        public ICommand SwitchSearchCommand
            => switchSearch ?? (switchSearch = new RelayCommand(_ => IsSearching = !IsSearching));

        /// <summary>
        ///     Gets the time left before the next ad can be posted.
        /// </summary>
        public string TimeLeft => timeLeftAd.DateTimeInFutureToRough() + "until next";

        public string AutoTimeLeft => autoTimeLeft.DateTimeInFutureToRough() + "until disabled";

        public override string EntryTextBoxIcon => isDisplayingChat
            ? "pack://application:,,,/icons/send_chat.png"
            : "pack://application:,,,/icons/send_ad.png";

        public override string EntryTextBoxLabel => isDisplayingChat
            ? "Chat here ..."
            : "Write a pretty ad here ...";

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

        private void SetLastMessageMark(object s = null, EventArgs e = null)
        {
            if (!Model.IsSelected) return;
            if (isDisplayingChat && Model.Messages.Any())
            {
                Model.Messages.Each(x => x.IsLastViewed = false);
                Model.Messages.Last().IsLastViewed = true;
            }
            else if (IsDisplayingAds && Model.Ads.Any())
            {
                Model.Ads.Each(x => x.IsLastViewed = false);
                Model.Ads.Last().IsLastViewed = true;
            }
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
                Application.Current.Dispatcher.Invoke(
                    () => Application.Current.MainWindow.Deactivated -= SetLastMessageMark);
            }

            base.Dispose(isManaged);
        }

        protected override void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Description":
                    OnPropertyChanged("Description");
                    ShowChannelDescription = ((GeneralChannelModel) Model).ShowChannelDescription;
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
                case "IsSelected":
                    if (Model.IsSelected)
                    {
                        if (isDisplayingChat && Model.Messages.Any())
                            Model.Messages.Last().IsLastViewed = false;
                        else if (IsDisplayingAds && Model.Ads.Any())
                            Model.Ads.Last().IsLastViewed = false;

                        var chanModel = (GeneralChannelModel) Model;
                        if (!chanModel.ShowChannelDescription) break;

                        ShowChannelDescription = false;
                        chanModel.LastChannelDescription = chanModel.Description.GetHashCode();

                        SettingsService.UpdateSettingsFile(
                            ChannelSettings, ChatModel.CurrentCharacter.Name, Model.Title, Model.Id);
                        break;
                    }

                    if (isDisplayingChat && Model.Messages.Any())
                    {
                        Model.Messages.Each(x => x.IsLastViewed = false);
                        Model.Messages.Last().IsLastViewed = true;
                    }
                    else if (IsDisplayingAds && Model.Ads.Any())
                    {
                        Model.Ads.Each(x => x.IsLastViewed = false);
                        Model.Ads.Last().IsLastViewed = true;
                    }
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
                return;

            var command = IsDisplayingChat
                ? CommandDefinitions.ClientSendChannelMessage
                : CommandDefinitions.ClientSendChannelAd;

            Events.SendUserCommand(command, new[] {Message}, Model.Id);

            if (!autoPostAds || IsDisplayingChat)
            {
                LastMessage = Message;
                Message = null;
            }

            if (IsDisplayingChat)
            {
                isInCoolDownMessage = true;
                OnPropertyChanged("CanPost");

                messageFloodTimer.Enabled = true;
                Log("sending message");
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
                Log("sending ad");
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
                return !CharacterManager.IsOnList(message.Poster.Name, ListKind.NotInterested, false);
            return true;
        }

        private void OnAdsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsDisplayingChat) return;

            OtherTabHasMessages =
                e.NewItems != null
                && e.NewItems.Count > 0
                &&
                e.NewItems.OfType<IMessage>()
                    .Any(x => !CharacterManager.IsOnList(x.Poster.Name, ListKind.NotInterested, false));
            ((GeneralChannelModel) Model).AdsContainsInteresting = OtherTabHasMessages;

            if (Model.Ads.All(x => x.IsHistoryMessage)) OtherTabHasMessages = false;
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
                        updateSearch.Defer(Constants.SearchDebounce);
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

            var args = updateModel?.Arguments as CharacterListChangedEventArgs;
            if (args == null)
                return;

            messageManager.RebuildItems();
        }

        #endregion
    }
}