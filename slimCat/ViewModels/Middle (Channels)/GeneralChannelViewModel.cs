// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralChannelViewModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
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
//   The general channel view model. This manages bindings for general channels, e.g anything that isn't a PM or the 'home' tab.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using Services;

    using slimCat;

    using Views;

    /// <summary>
    ///     The general channel view model. This manages bindings for general channels, e.g anything that isn't a PM or the 'home' tab.
    /// </summary>
    public class GeneralChannelViewModel : ChannelViewModelBase, IDisposable
    {
        #region Fields

        private readonly ChannelManagementViewModel _channManVM;

        private readonly ObservableCollection<IMessage> _currentMessages = new ObservableCollection<IMessage>();

        private readonly IList<string> _thisDingTerms = new List<string>();

        // this is a combination of all relevant ding terms 
        private Timer _adFlood = new Timer(602000);

        private string _adMessage = string.Empty;

        private bool _autoPostAds;

        private GenderSettingsModel _genderSettings = new GenderSettingsModel();

        private bool _hasNewAds;

        private bool _hasNewMessages;

        private bool _isDisplayingChat;

        private bool _isInCoolDownAd;

        private bool _isInCoolDownMessage;

        private bool _isSearching;

        private Timer _messageFlood = new Timer(500);

        private GenericSearchSettingsModel _searchSettings = new GenericSearchSettingsModel();

        private RelayCommand _switch;

        private RelayCommand _switchSearch;

        private DateTimeOffset _timeLeftAd;

        private Timer _update = new Timer(1000);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralChannelViewModel"/> class.
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
        public GeneralChannelViewModel(
            string name, IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                this.Model = this.CM.CurrentChannels.FirstOrDefault(chan => chan.ID == name)
                             ?? this.CM.AllChannels.First(chan => chan.ID == name);
                this.Model.ThrowIfNull("this.Model");

                string safeName = HelperConverter.EscapeSpaces(name);

                this._container.RegisterType<object, GeneralChannelView>(safeName, new InjectionConstructor(this));

                this._isDisplayingChat = this.ShouldDisplayChat;

                this._channManVM = new ChannelManagementViewModel(this._events, this.Model as GeneralChannelModel);

                // instance our management vm
                this.Model.Messages.CollectionChanged += this.OnMessagesChanged;
                this.Model.Ads.CollectionChanged += this.OnAdsChanged;
                this.Model.PropertyChanged += this.OnModelPropertyChanged;

                this._genderSettings.Updated += (s, e) =>
                    {
                        this.OnPropertyChanged("CurrentMessages");
                        this.OnPropertyChanged("GenderSettings");
                    };

                this._searchSettings.Updated += (s, e) =>
                    {
                        this.OnPropertyChanged("SearchSettings");
                        this.OnPropertyChanged("CurrentMessages");
                    };

                this._messageFlood.Elapsed += (s, e) =>
                    {
                        this._isInCoolDownMessage = false;
                        this._messageFlood.Enabled = false;
                        this.OnPropertyChanged("CanPost");
                    };

                this._adFlood.Elapsed += (s, e) =>
                    {
                        this._isInCoolDownAd = false;
                        this._adFlood.Enabled = false;
                        this.OnPropertyChanged("CanPost");
                        this.OnPropertyChanged("CannotPost");
                        this.OnPropertyChanged("ShouldShowAutoPost");
                        if (this._autoPostAds)
                        {
                            this.SendAutoAd();
                        }
                    };

                this._update.Elapsed += (s, e) =>
                    {
                        if (!this.Model.IsSelected)
                        {
                            return;
                        }

                        if (this.CannotPost)
                        {
                            this.OnPropertyChanged("TimeLeft");
                        }

                        this.OnPropertyChanged("StatusString");
                    };

                this._channManVM.PropertyChanged += (s, e) => this.OnPropertyChanged("ChannelManagementViewModel");

                this._update.Enabled = true;

                var newSettings = SettingsDaemon.GetChannelSettings(
                    cm.SelectedCharacter.Name, this.Model.Title, this.Model.ID, this.Model.Type);
                this.Model.Settings = newSettings;

                this.ChannelSettings.Updated += (s, e) =>
                    {
                        this.OnPropertyChanged("ChannelSettings");
                        this.OnPropertyChanged("HasNotifyTerms");
                        if (!this.ChannelSettings.IsChangingSettings)
                        {
                            SettingsDaemon.UpdateSettingsFile(
                                this.ChannelSettings, cm.SelectedCharacter.Name, this.Model.Title, this.Model.ID);
                        }
                    };

                this.PropertyChanged += this.OnPropertyChanged;

                this._events.GetEvent<NewUpdateEvent>().Subscribe(this.UpdateChat);
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
            get
            {
                return this._autoPostAds;
            }

            set
            {
                this._autoPostAds = value;
                this.OnPropertyChanged("AutoPost");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether can post.
        /// </summary>
        public bool CanPost
        {
            get
            {
                return (this.IsDisplayingChat && !this._isInCoolDownMessage)
                       || (this.IsDisplayingAds && !this._isInCoolDownAd);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether can switch.
        /// </summary>
        public bool CanSwitch
        {
            get
            {
                if (this.IsDisplayingChat && this.ShouldDisplayAds)
                {
                    return true;
                }

                return !this.IsDisplayingChat && this.ShouldDisplayChat;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether cannot post.
        /// </summary>
        public bool CannotPost
        {
            get
            {
                return !this.CanPost;
            }
        }

        /// <summary>
        ///     Gets the channel management view model.
        /// </summary>
        public ChannelManagementViewModel ChannelManagementViewModel
        {
            get
            {
                return this._channManVM;
            }
        }

        /// <summary>
        ///     Gets the chat content string.
        /// </summary>
        public string ChatContentString
        {
            get
            {
                return this.IsDisplayingChat ? "Chat" : "Ads";
            }
        }

        /// <summary>
        ///     Gets the current messages.
        /// </summary>
        public ObservableCollection<IMessage> CurrentMessages
        {
            get
            {
                return this._currentMessages;
            }
        }

        /// <summary>
        ///     Gets the gender settings.
        /// </summary>
        public GenderSettingsModel GenderSettings
        {
            get
            {
                return this._genderSettings;
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
        ///     Gets a value indicating whether is chatting.
        /// </summary>
        public bool IsChatting
        {
            get
            {
                return !this.IsSearching;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether is displaying ads.
        /// </summary>
        public bool IsDisplayingAds
        {
            get
            {
                return !this.IsDisplayingChat;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is displaying chat.
        /// </summary>
        public bool IsDisplayingChat
        {
            get
            {
                return this._isDisplayingChat;
            }

            set
            {
                if (this._isDisplayingChat == value)
                {
                    return;
                }

                this._isDisplayingChat = value;

                string temp = this.Message;
                this.Message = this._adMessage;
                this._adMessage = temp;

                this.OnPropertyChanged("IsDisplayingChat");
                this.OnPropertyChanged("IsDisplayingAds");
                this.OnPropertyChanged("ChatContentString");
                this.OnPropertyChanged("CurrentMessages");
                this.OnPropertyChanged("MessageMax");
                this.OnPropertyChanged("CanPost");
                this.OnPropertyChanged("CannotPost");
                this.OnPropertyChanged("ShouldShowAutoPost");
                this.OnPropertyChanged("SwitchChannelTypeString");

                if (value)
                {
                    this._hasNewMessages = false;
                }
                else
                {
                    this._hasNewAds = false;
                }

                this.OnPropertyChanged("OtherTabHasMessages");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether is not searching.
        /// </summary>
        public bool IsNotSearching
        {
            get
            {
                return !this.IsSearching;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is searching.
        /// </summary>
        public bool IsSearching
        {
            get
            {
                return this._isSearching;
            }

            set
            {
                if (this._isSearching == value)
                {
                    return;
                }

                this._isSearching = value;
                this.OnPropertyChanged("IsSearching");
                this.OnPropertyChanged("SearchSwitchMessageString");
                this.OnPropertyChanged("IsChatting");
                this.OnPropertyChanged("IsNotSearching");
            }
        }

        /// <summary>
        ///     Gets the motd.
        /// </summary>
        public string MOTD
        {
            get
            {
                return ((GeneralChannelModel)this.Model).MOTD;
            }
        }

        /// <summary>
        ///     if we're displaying the channel's messages, if there's a new ad (or vice-versa)
        /// </summary>
        public bool OtherTabHasMessages
        {
            get
            {
                if (this.IsDisplayingChat)
                {
                    return this._hasNewAds;
                }
                else
                {
                    return this._hasNewMessages;
                }
            }
        }

        /// <summary>
        ///     Gets the search settings.
        /// </summary>
        public GenericSearchSettingsModel SearchSettings
        {
            get
            {
                return this._searchSettings;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether should display ads.
        /// </summary>
        public bool ShouldDisplayAds
        {
            get
            {
                return (this.Model.Mode == ChannelMode.both) || (this.Model.Mode == ChannelMode.ads);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether should display chat.
        /// </summary>
        public bool ShouldDisplayChat
        {
            get
            {
                return (this.Model.Mode == ChannelMode.both) || (this.Model.Mode == ChannelMode.chat);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether should show auto post.
        /// </summary>
        public bool ShouldShowAutoPost
        {
            get
            {
                if (!this._isInCoolDownAd)
                {
                    return this.IsDisplayingAds;
                }

                return this.IsDisplayingAds && !string.IsNullOrEmpty(this.Message);
            }
        }

        /// <summary>
        ///     This is used for the channel settings, if it should show settings like 'notify when this character is mentioned'
        /// </summary>
        public bool ShowAllSettings
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        ///     Gets the status string.
        /// </summary>
        public string StatusString
        {
            get
            {
                if (this.IsDisplayingAds && this.AutoPost && this._isInCoolDownAd)
                {
                    return "Auto post ads enabled";
                }

                if (!string.IsNullOrEmpty(this.Message))
                {
                    return string.Format(
                        "{0} / {1} characters", this.Message.Length, this.IsDisplayingChat ? "4,096" : "50,000");
                }

                if (this._hasNewAds && this.IsDisplayingChat)
                {
                    return "This channel has new ad(s).";
                }

                if (this._hasNewMessages && this.IsDisplayingAds)
                {
                    return "This channel has new message(s).";
                }

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
                return this._switch
                       ?? (this._switch = new RelayCommand(param => this.IsDisplayingChat = !this.IsDisplayingChat));
            }
        }

        /// <summary>
        ///     Gets the switch search command.
        /// </summary>
        public ICommand SwitchSearchCommand
        {
            get
            {
                return this._switchSearch ?? (this._switchSearch = new RelayCommand(
                                                                       param =>
                                                                           {
                                                                               this.OnPropertyChanged("CurrentMessages");
                                                                               this.IsSearching = !this.IsSearching;
                                                                           }));
            }
        }

        /// <summary>
        ///     Gets the time left.
        /// </summary>
        public string TimeLeft
        {
            get
            {
                return HelperConverter.DateTimeInFutureToRough(this._timeLeftAd) + "left";
            }
        }

        #endregion

        #region Properties

        private IEnumerable<string> thisDingTerms
        {
            get
            {
                const int CharacterNameOffset = 2; // how many ding terms we have to offset for the character's name
                int count = this._thisDingTerms.Count;
                int shouldBe = ApplicationSettings.GlobalNotifyTermsList.Count()
                               + this.Model.Settings.EnumerableTerms.Count() + CharacterNameOffset;

                if (count != shouldBe)
                {
                    this._thisDingTerms.Clear();

                    foreach (string term in ApplicationSettings.GlobalNotifyTermsList)
                    {
                        this._thisDingTerms.Add(term);
                    }

                    foreach (string term in this.Model.Settings.EnumerableTerms)
                    {
                        this._thisDingTerms.Add(term);
                    }

                    this._thisDingTerms.Add(this._cm.SelectedCharacter.Name);
                    this._thisDingTerms.Add(this._cm.SelectedCharacter.Name + "'s");
                }

                return this._thisDingTerms.Distinct().Where(term => !string.IsNullOrWhiteSpace(term));
            }
        }

        #endregion

        #region Public Methods and Operators
        /// <summary>
        ///     The send auto ad.
        /// </summary>
        public void SendAutoAd()
        {
            var messageToSend = this.IsDisplayingChat ? this._adMessage : this.Message;
            if (messageToSend == null)
            {
                this.UpdateError("There is no ad to auto-post!");
            }

            IDictionary<string, object> toSend =
                CommandDefinitions.CreateCommand(
                    CommandDefinitions.ClientSendChannelAd, new List<string> { messageToSend }, this.Model.ID)
                                  .toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(toSend);
            this._timeLeftAd = DateTimeOffset.Now.AddMinutes(10).AddSeconds(2);

            this._isInCoolDownAd = true;
            this._adFlood.Start();
            this.OnPropertyChanged("CanPost");
            this.OnPropertyChanged("CannotPost");
        }

        #endregion

        #region Methods

        internal override void InvertButton(object arguments)
        {
            var args = arguments as string;
            if (args == null)
            {
                return;
            }

            if (args.Equals("Messages"))
            {
                this.ChannelSettings.MessageNotifyOnlyForInteresting =
                    !this.ChannelSettings.MessageNotifyOnlyForInteresting;
            }

            if (args.Equals("PromoteDemote"))
            {
                this.ChannelSettings.PromoteDemoteNotifyOnlyForInteresting =
                    !this.ChannelSettings.PromoteDemoteNotifyOnlyForInteresting;
            }

            if (args.Equals("JoinLeave"))
            {
                this.ChannelSettings.JoinLeaveNotifyOnlyForInteresting =
                    !this.ChannelSettings.JoinLeaveNotifyOnlyForInteresting;
            }

            this.OnPropertyChanged("ChannelSettings");
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="isManaged">
        /// The is Managed.
        /// </param>
        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this._update.Dispose();
                this._update = null;

                this._adFlood.Dispose();
                this._adFlood = null;

                this._messageFlood.Dispose();
                this._messageFlood = null;

                this._searchSettings = null;
                this._genderSettings = null;

                this._events.GetEvent<NewUpdateEvent>().Unsubscribe(this.UpdateChat);
                this.Model.Messages.CollectionChanged -= this.OnMessagesChanged;
                this.Model.Ads.CollectionChanged -= this.OnAdsChanged;
                this.PropertyChanged -= this.OnPropertyChanged;

                (this.Model as GeneralChannelModel).MOTD = null;
                (this.Model as GeneralChannelModel).Moderators.Clear();
            }

            base.Dispose(isManaged);
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
            switch (e.PropertyName)
            {
                case "MOTD":
                    this.OnPropertyChanged("MOTD");
                    break;
                case "Type":
                    this.OnPropertyChanged("ChannelTypeString"); // fixes laggy room type change
                    break;
                case "Moderators":
                    this.OnPropertyChanged("HasPermissions"); // fixes laggy permissions
                    break;
                case "Mode":
                    if (this.Model.Mode == ChannelMode.ads && this.IsDisplayingChat)
                    {
                        this.IsDisplayingChat = false;
                    }

                    if (this.Model.Mode == ChannelMode.chat && this.IsDisplayingAds)
                    {
                        this.IsDisplayingChat = true;
                    }

                    this.OnPropertyChanged("CanSwitch");
                    break;
            }
        }

        /// <summary>
        ///     The send message.
        /// </summary>
        protected override void SendMessage()
        {
            if (this.IsSearching)
            {
                return;
            }

            // if we're not searching, treat this input normal
            if ((this.IsDisplayingChat && this.Message.Length > 4096)
                || (this.IsDisplayingAds && this.Message.Length > 50000))
            {
                this.UpdateError("You expect me to post all of that?! How about you post less, huh?");
                return;
            }

            if ((this._isInCoolDownAd && this.IsDisplayingAds) || (this._isInCoolDownMessage && this.IsDisplayingChat))
            {
                this.UpdateError("Cool your engines. Wait a little before you post again.");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.Message))
            {
                this.UpdateError("I'm sure you didn't mean to do that.");
                return;
            }

            var command = this.IsDisplayingChat
                                 ? CommandDefinitions.ClientSendChannelMessage
                                 : CommandDefinitions.ClientSendChannelAd;

            var toSend =
                CommandDefinitions.CreateCommand(command, new List<string> { this.Message }, this.Model.ID)
                                  .toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(toSend);

            if (!this._autoPostAds || this.IsDisplayingChat)
            {
                this.Message = null;
            }

            if (this.IsDisplayingChat)
            {
                this._isInCoolDownMessage = true;
                this.OnPropertyChanged("CanPost");

                this._messageFlood.Enabled = true;
            }
            else
            {
                this._timeLeftAd = DateTime.Now.AddMinutes(10).AddSeconds(2);

                this._isInCoolDownAd = true;
                this.OnPropertyChanged("CanPost");
                this.OnPropertyChanged("CannotPost");
                this.OnPropertyChanged("ShouldShowAutoPost");

                this._adFlood.Enabled = true;
            }
        }

        private bool MeetsFilter(IMessage message)
        {
            return message.MeetsFilters(
                this.GenderSettings, this.SearchSettings, this.CM, this.CM.SelectedChannel as GeneralChannelModel);
        }

        private void OnAdsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.IsDisplayingAds)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            var items = e.NewItems.Cast<IMessage>();
                            foreach (var item in items.Where(this.MeetsFilter))
                            {
                                this._currentMessages.Add(item);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        this._currentMessages.Clear();
                        break;
                }
            }

            if (this.IsDisplayingChat)
            {
                this._hasNewAds = this.Model.Ads.Where(this.MeetsFilter).Any();
                this.OnPropertyChanged("OtherTabHasMessages");
            }

            this.OnPropertyChanged("StatusString");
        }

        private void OnMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.IsDisplayingChat)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            var items = e.NewItems.Cast<IMessage>();
                            foreach (var item in items.Where(this.MeetsFilter))
                            {
                                this._currentMessages.Add(item);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        this._currentMessages.Clear();
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        this._currentMessages.RemoveAt(0);
                        break;
                }
            }
            else if (this.IsDisplayingAds)
            {
                this._hasNewMessages = this.Model.Messages.Count > 0;
                this.OnPropertyChanged("OtherTabHasMessages");
            }

            this.OnPropertyChanged("StatusString");
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Message":
                    this.OnPropertyChanged("StatusString"); // keep the counter updated
                    break;
                case "SearchSettings":
                    {
                        this._currentMessages.Clear();
                        ObservableCollection<IMessage> collection = this.IsDisplayingChat
                                                                        ? this.Model.Messages
                                                                        : this.Model.Ads;
                        foreach (IMessage message in collection.Where(this.MeetsFilter))
                        {
                            this._currentMessages.Add(message);
                        }
                    }

                    break;
                case "IsDisplayingChat":
                    {
                        this._currentMessages.Clear();
                        IEnumerable<IMessage> collection = this.IsDisplayingChat
                                                               ? this.Model.Messages.Where(this.MeetsFilter)
                                                               : this.Model.Ads.Where(this.MeetsFilter);
                        foreach (IMessage item in collection)
                        {
                            this._currentMessages.Add(item);
                        }
                    }

                    break;
            }
        }

        private void UpdateChat(NotificationModel Update)
        {
            var update = Update as CharacterUpdateModel;
            if (update == null)
            {
                return;
            }

            if (update.Arguments is CharacterUpdateModel.ListChangedEventArgs)
            {
                this.OnPropertyChanged("CurrentMessages");
            }
        }

        #endregion
    }
}