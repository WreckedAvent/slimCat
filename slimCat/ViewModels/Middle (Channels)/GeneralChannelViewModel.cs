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

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat;
    using Slimcat.Libraries;
    using Slimcat.Models;
    using Slimcat.Services;
    using Slimcat.Utilities;
    using Slimcat.Views;

    /// <summary>
    ///     The general channel view model. This manages bindings for general channels, e.g anything that isn't a PM or the 'home' tab.
    /// </summary>
    public class GeneralChannelViewModel : ChannelViewModelBase
    {
        #region Fields
        private readonly IList<string> thisDingTerms = new List<string>();

        private Timer adFlood = new Timer(602000);

        private string adMessage = string.Empty;

        private bool autoPostAds;

        private GenderSettingsModel genderSettings = new GenderSettingsModel();

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

        private FilteredCollection<IMessage, IViewableObject> messageManager;

        private GenericSearchSettingsModel searchSettings = new GenericSearchSettingsModel();
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
                this.Model = this.ChatModel.CurrentChannels.FirstOrDefault(chan => chan.Id == name)
                             ?? this.ChatModel.AllChannels.First(chan => chan.Id == name);
                this.Model.ThrowIfNull("this.Model");

                var safeName = HelperConverter.EscapeSpaces(name);

                this.Container.RegisterType<object, GeneralChannelView>(safeName, new InjectionConstructor(this));

                this.isDisplayingChat = this.ShouldDisplayChat;

                this.ChannelManagementViewModel = new ChannelManagementViewModel(this.Events, this.Model as GeneralChannelModel);

                // instance our management vm
                this.Model.Messages.CollectionChanged += this.OnMessagesChanged;
                this.Model.Ads.CollectionChanged += this.OnAdsChanged;
                this.Model.PropertyChanged += this.OnModelPropertyChanged;

                this.messageManager = 
                    new FilteredCollection<IMessage, IViewableObject>(
                        this.isDisplayingChat 
                            ? this.Model.Messages 
                            : this.Model.Ads, 
                        this.MeetsFilter, 
                        this.IsDisplayingAds);

                this.genderSettings.Updated += (s, e) =>
                    {
                        this.OnPropertyChanged("GenderSettings");
                    };

                this.SearchSettings.Updated += (s, e) =>
                    {
                        this.OnPropertyChanged("SearchSettings");
                    };

                this.messageFlood.Elapsed += (s, e) =>
                    {
                        this.isInCoolDownMessage = false;
                        this.messageFlood.Enabled = false;
                        this.OnPropertyChanged("CanPost");
                    };

                this.adFlood.Elapsed += (s, e) =>
                    {
                        this.isInCoolDownAd = false;
                        this.adFlood.Enabled = false;
                        this.OnPropertyChanged("CanPost");
                        this.OnPropertyChanged("CannotPost");
                        this.OnPropertyChanged("ShouldShowAutoPost");
                        if (this.autoPostAds)
                        {
                            this.SendAutoAd();
                        }
                    };

                this.update.Elapsed += (s, e) =>
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

                this.ChannelManagementViewModel.PropertyChanged += (s, e) => this.OnPropertyChanged("ChannelManagementViewModel");

                this.update.Enabled = true;

                var newSettings = SettingsDaemon.GetChannelSettings(
                    cm.CurrentCharacter.Name, this.Model.Title, this.Model.Id, this.Model.Type);
                this.Model.Settings = newSettings;

                this.ChannelSettings.Updated += (s, e) =>
                    {
                        this.OnPropertyChanged("ChannelSettings");
                        this.OnPropertyChanged("HasNotifyTerms");
                        if (!this.ChannelSettings.IsChangingSettings)
                        {
                            SettingsDaemon.UpdateSettingsFile(
                                this.ChannelSettings, cm.CurrentCharacter.Name, this.Model.Title, this.Model.Id);
                        }
                    };

                this.PropertyChanged += this.OnPropertyChanged;

                this.Events.GetEvent<NewUpdateEvent>().Subscribe(this.UpdateChat);
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
                return this.autoPostAds;
            }

            set
            {
                this.autoPostAds = value;
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
                return (this.IsDisplayingChat && !this.isInCoolDownMessage)
                       || (this.IsDisplayingAds && !this.isInCoolDownAd);
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
        public ChannelManagementViewModel ChannelManagementViewModel { get; private set; }

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
        public ObservableCollection<IViewableObject> CurrentMessages
        {
            get
            {
                return this.messageManager.Collection;
            }
        }

        /// <summary>
        ///     Gets the gender settings.
        /// </summary>
        public GenderSettingsModel GenderSettings
        {
            get
            {
                return this.genderSettings;
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
        ///     Gets a value indicating whether is displaying Ads.
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
                return this.isDisplayingChat;
            }

            set
            {
                if (this.isDisplayingChat == value)
                {
                    return;
                }

                this.isDisplayingChat = value;

                var temp = this.Message;
                this.Message = this.adMessage;
                this.adMessage = temp;

                this.messageManager.OriginalCollection = value ? this.Model.Messages : this.Model.Ads;

                this.OnPropertyChanged("IsDisplayingChat");
                this.OnPropertyChanged("IsDisplayingAds");
                this.OnPropertyChanged("ChatContentString");
                this.OnPropertyChanged("MessageMax");
                this.OnPropertyChanged("CanPost");
                this.OnPropertyChanged("CannotPost");
                this.OnPropertyChanged("ShouldShowAutoPost");
                this.OnPropertyChanged("SwitchChannelTypeString");
                this.OtherTabHasMessages = false;
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
                return this.isSearching;
            }

            set
            {
                if (this.isSearching == value)
                {
                    return;
                }

                this.isSearching = value;
                this.OnPropertyChanged("IsSearching");
                this.OnPropertyChanged("SearchSwitchMessageString");
                this.OnPropertyChanged("IsChatting");
                this.OnPropertyChanged("IsNotSearching");
            }
        }

        /// <summary>
        ///     Gets the motd.
        /// </summary>
        public string Description
        {
            get
            {
                return ((GeneralChannelModel)this.Model).Description;
            }
        }

        /// <summary>
        ///     if we're displaying the channel's messages, if there's a new ad (or vice-versa)
        /// </summary>
        public bool OtherTabHasMessages
        {
            get
            {
                return this.IsDisplayingChat ? this.hasNewAds : this.hasNewMessages;
            }

            set
            {
                if (this.IsDisplayingAds)
                {
                    this.hasNewMessages = value;
                }
                else
                {
                    this.hasNewAds = value;
                }

                this.OnPropertyChanged("OtherTabHasMessages");
                this.OnPropertyChanged("StatusString");
            }
        }

        /// <summary>
        ///     Gets the search settings.
        /// </summary>
        public GenericSearchSettingsModel SearchSettings
        {
            get
            {
                return this.searchSettings;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether should display Ads.
        /// </summary>
        public bool ShouldDisplayAds
        {
            get
            {
                return (this.Model.Mode == ChannelMode.Both) || (this.Model.Mode == ChannelMode.Ads);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether should display chat.
        /// </summary>
        public bool ShouldDisplayChat
        {
            get
            {
                return (this.Model.Mode == ChannelMode.Both) || (this.Model.Mode == ChannelMode.Chat);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether should show auto post.
        /// </summary>
        public bool ShouldShowAutoPost
        {
            get
            {
                if (!this.isInCoolDownAd)
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
                if (this.IsDisplayingAds && this.AutoPost && this.isInCoolDownAd)
                {
                    return "Auto post Ads enabled";
                }

                if (!string.IsNullOrEmpty(this.Message))
                {
                    return string.Format(
                        "{0} / {1} characters", this.Message.Length, this.IsDisplayingChat ? "4,096" : "50,000");
                }

                if (this.hasNewAds && this.IsDisplayingChat)
                {
                    return "This channel has new ad(s).";
                }

                if (this.hasNewMessages && this.IsDisplayingAds)
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
                return this.@switch
                       ?? (this.@switch = new RelayCommand(param => this.IsDisplayingChat = !this.IsDisplayingChat));
            }
        }

        /// <summary>
        ///     Gets the switch search command.
        /// </summary>
        public ICommand SwitchSearchCommand
        {
            get
            {
                return this.switchSearch ?? (this.switchSearch = new RelayCommand(
                                                                     delegate
                                                                         {
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
                return HelperConverter.DateTimeInFutureToRough(this.timeLeftAd) + "left";
            }
        }

        #endregion

        #region Properties

        private IEnumerable<string> ThisDingTerms
        {
            get
            {
                const int CharacterNameOffset = 2; // how many ding terms we have to offset for the character's name
                var count = this.thisDingTerms.Count;
                var shouldBe = ApplicationSettings.GlobalNotifyTermsList.Count()
                               + this.Model.Settings.EnumerableTerms.Count() + CharacterNameOffset;

                if (count != shouldBe)
                {
                    this.thisDingTerms.Clear();

                    foreach (var term in ApplicationSettings.GlobalNotifyTermsList)
                    {
                        this.thisDingTerms.Add(term);
                    }

                    foreach (var term in this.Model.Settings.EnumerableTerms)
                    {
                        this.thisDingTerms.Add(term);
                    }

                    this.thisDingTerms.Add(this.ChatModel.CurrentCharacter.Name);
                    this.thisDingTerms.Add(this.ChatModel.CurrentCharacter.Name + "'s");
                }

                return this.thisDingTerms.Distinct().Where(term => !string.IsNullOrWhiteSpace(term));
            }
        }

        #endregion

        #region Public Methods and Operators
        /// <summary>
        ///     The send auto ad.
        /// </summary>
        public void SendAutoAd()
        {
            var messageToSend = this.IsDisplayingChat ? this.adMessage : this.Message;
            if (messageToSend == null)
            {
                this.UpdateError("There is no ad to auto-post!");
            }

            var toSend =
                CommandDefinitions.CreateCommand(
                    CommandDefinitions.ClientSendChannelAd, new List<string> { messageToSend }, this.Model.Id)
                                  .ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(toSend);
            this.timeLeftAd = DateTimeOffset.Now.AddMinutes(10).AddSeconds(2);

            this.isInCoolDownAd = true;
            this.adFlood.Start();
            this.OnPropertyChanged("CanPost");
            this.OnPropertyChanged("CannotPost");
        }

        #endregion

        #region Methods

        protected override void InvertButton(object arguments)
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
                this.update.Dispose();
                this.update = null;

                this.adFlood.Dispose();
                this.adFlood = null;

                this.messageFlood.Dispose();
                this.messageFlood = null;

                this.searchSettings = null;
                this.genderSettings = null;

                this.Events.GetEvent<NewUpdateEvent>().Unsubscribe(this.UpdateChat);
                this.Model.Messages.CollectionChanged -= this.OnMessagesChanged;
                this.Model.Ads.CollectionChanged -= this.OnAdsChanged;
                this.PropertyChanged -= this.OnPropertyChanged;
                this.messageManager.Dispose();
                this.messageManager = null;

                (this.Model as GeneralChannelModel).Description = null;
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
                case "Description":
                    this.OnPropertyChanged("Description");
                    break;
                case "Type":
                    this.OnPropertyChanged("ChannelTypeString"); // fixes laggy room type change
                    break;
                case "Moderators":
                    this.OnPropertyChanged("HasPermissions"); // fixes laggy permissions
                    break;
                case "Mode":
                    if (this.Model.Mode == ChannelMode.Ads && this.IsDisplayingChat)
                    {
                        this.IsDisplayingChat = false;
                    }

                    if (this.Model.Mode == ChannelMode.Chat && this.IsDisplayingAds)
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

            if ((this.isInCoolDownAd && this.IsDisplayingAds) || (this.isInCoolDownMessage && this.IsDisplayingChat))
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
                CommandDefinitions.CreateCommand(command, new List<string> { this.Message }, this.Model.Id)
                                  .ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(toSend);

            if (!this.autoPostAds || this.IsDisplayingChat)
            {
                this.Message = null;
            }

            if (this.IsDisplayingChat)
            {
                this.isInCoolDownMessage = true;
                this.OnPropertyChanged("CanPost");

                this.messageFlood.Enabled = true;
            }
            else
            {
                this.timeLeftAd = DateTime.Now.AddMinutes(10).AddSeconds(2);

                this.isInCoolDownAd = true;
                this.OnPropertyChanged("CanPost");
                this.OnPropertyChanged("CannotPost");
                this.OnPropertyChanged("ShouldShowAutoPost");

                this.adFlood.Enabled = true;
            }
        }

        private bool MeetsFilter(IMessage message)
        {
            return message.MeetsFilters(
                this.GenderSettings, this.SearchSettings, this.ChatModel, this.ChatModel.CurrentChannel as GeneralChannelModel);
        }

        private void OnAdsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.IsDisplayingChat)
            {
                this.OtherTabHasMessages = e.Action != NotifyCollectionChangedAction.Reset;
            }
        }

        private void OnMessagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.IsDisplayingAds)
            {
                this.OtherTabHasMessages = e.Action != NotifyCollectionChangedAction.Reset 
                    && e.NewItems.Cast<IMessage>().Where(this.MeetsFilter).Any();
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Message":
                    this.OnPropertyChanged("StatusString"); // keep the counter updated
                    break;
                case "SearchSettings":
                case "IsDisplayingChat":
                    {
                        this.messageManager.IsFiltering = this.IsDisplayingAds || this.isSearching;
                        this.messageManager.RebuildItems();
                    }

                    break;

                case "IsSearching":
                    {
                        this.messageManager.IsFiltering = this.IsDisplayingAds || this.isSearching;
                        this.messageManager.RebuildItems();
                    }

                    break;
            }
        }

        private void UpdateChat(NotificationModel newUpdate)
        {
            var updateModel = newUpdate as CharacterUpdateModel;
            if (updateModel == null)
            {
                return;
            }

            var args = updateModel.Arguments as CharacterUpdateModel.ListChangedEventArgs;
            if (args == null)
            {
                return;
            }

            this.messageManager.RebuildItems();
        }

        #endregion
    }
}