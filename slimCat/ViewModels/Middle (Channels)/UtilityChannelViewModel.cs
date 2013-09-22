// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UtilityChannelViewModel.cs" company="Justin Kadrovach">
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
//   Used for a few channels which are not treated normally and cannot receive/send messages.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Timers;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Services;
    using Slimcat.Utilities;
    using Slimcat.Views;

    /// <summary>
    ///     Used for a few channels which are not treated normally and cannot receive/send messages.
    /// </summary>
    public class UtilityChannelViewModel : ChannelViewModelBase
    {
        #region Constants

        private const string NewVersionLink = "https://dl.dropbox.com/u/29984849/slimCat/latest.csv";

        #endregion

        #region Fields

        private readonly Timer updateTimer = new Timer(1000); // every second

        private readonly StringBuilder connectDotDot;

        private readonly CacheCount minuteOnlineCount;

        private StringBuilder flavorText;

        private bool inStagger;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UtilityChannelViewModel"/> class.
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
        public UtilityChannelViewModel(
            string name, IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            try
            {
                this.Model = this.Container.Resolve<GeneralChannelModel>(name);
                this.ConnectTime = 0;
                this.flavorText = new StringBuilder("Connecting");
                this.connectDotDot = new StringBuilder();

                this.Container.RegisterType<object, UtilityChannelView>(this.Model.Id, new InjectionConstructor(this));
                this.minuteOnlineCount = new CacheCount(this.OnlineCountPrime, 15, 1000 * 15);

                this.updateTimer.Enabled = true;
                this.updateTimer.Elapsed += (s, e) =>
                    {
                        this.OnPropertyChanged("RoughServerUpTime");
                        this.OnPropertyChanged("RoughClientUpTime");
                        this.OnPropertyChanged("LastMessageReceived");
                        this.OnPropertyChanged("IsConnecting");
                    };

                this.updateTimer.Elapsed += this.UpdateConnectText;

                this.Events.GetEvent<NewUpdateEvent>().Subscribe(
                    param =>
                        {
                            if (param is CharacterUpdateModel)
                            {
                                var temp = param as CharacterUpdateModel;
                                if (temp.Arguments is CharacterUpdateModel.LoginStateChangedEventArgs)
                                {
                                    this.OnPropertyChanged("OnlineCount");
                                    this.OnPropertyChanged("OnlineFriendsCount");
                                    this.OnPropertyChanged("OnlineBookmarksCount");
                                    this.OnPropertyChanged("OnlineCountChange");
                                }
                            }
                        });

                this.Events.GetEvent<LoginAuthenticatedEvent>().Subscribe(this.LoggedInEvent);
                this.Events.GetEvent<LoginFailedEvent>().Subscribe(this.LoginFailedEvent);
                this.Events.GetEvent<ReconnectingEvent>().Subscribe(this.LoginReconnectingEvent);

                SettingsDaemon.ReadApplicationSettingsFromXml(cm.CurrentCharacter.Name);

                try
                {
                    string[] args;
                    using (var client = new WebClient())
                    {
                        using (var stream = client.OpenRead("https://dl.dropbox.com/u/29984849/slimCat/latest.csv"))
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                args = reader.ReadToEnd().Split(',');
                            }
                        }
                    }

                    if (args[0] == Constants.FriendlyName)
                    {
                        return;
                    }

                    this.HasNewUpdate = true;

                    this.UpdateName = args[0];
                    this.UpdateLink = args[1];
                    this.UpdateBuildTime = args[2];

                    this.OnPropertyChanged("HasNewUpdate");
                    this.OnPropertyChanged("UpdateName");
                    this.OnPropertyChanged("UpdateLink");
                    this.OnPropertyChanged("UpdateBuildTime");
                }
                catch
                {
                }
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
        ///     Gets or sets a value indicating whether allow logging.
        /// </summary>
        public bool AllowLogging
        {
            get
            {
                return ApplicationSettings.AllowLogging;
            }

            set
            {
                ApplicationSettings.AllowLogging = value;
                SettingsDaemon.SaveApplicationSettingsToXml(this.ChatModel.CurrentCharacter.Name);
            }
        }

        /// <summary>
        ///     Gets or sets the back log max.
        /// </summary>
        public int BackLogMax
        {
            get
            {
                return ApplicationSettings.BackLogMax;
            }

            set
            {
                if (value < 25000 || value > 10)
                {
                    ApplicationSettings.BackLogMax = value;
                }

                SettingsDaemon.SaveApplicationSettingsToXml(this.ChatModel.CurrentCharacter.Name);
            }
        }

        /// <summary>
        ///     Gets the client id string.
        /// </summary>
        public string ClientIDString
        {
            get
            {
                return string.Format("{0} {1} ({2})", Constants.ClientID, Constants.ClientName, Constants.ClientVer);
            }
        }

        /// <summary>
        ///     Gets the connect flavor text.
        /// </summary>
        public string ConnectFlavorText
        {
            get
            {
                return this.flavorText + this.connectDotDot.ToString()
                       + (!this.inStagger ? "\nRequest sent " + this.ConnectTime + " seconds ago" : string.Empty);
            }
        }

        /// <summary>
        ///     Gets or sets the connect time.
        /// </summary>
        public int ConnectTime { get; set; }

        /// <summary>
        ///     Gets or sets the global notify terms.
        /// </summary>
        public string GlobalNotifyTerms
        {
            get
            {
                return ApplicationSettings.GlobalNotifyTerms;
            }

            set
            {
                ApplicationSettings.GlobalNotifyTerms = value;
                SettingsDaemon.SaveApplicationSettingsToXml(this.ChatModel.CurrentCharacter.Name);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether has new update.
        /// </summary>
        public bool HasNewUpdate { get; set; }

        /// <summary>
        ///     Gets a value indicating whether is connecting.
        /// </summary>
        public bool IsConnecting
        {
            get
            {
                return !this.ChatModel.IsAuthenticated;
            }
        }

        /// <summary>
        ///     Gets the last message received.
        /// </summary>
        public string LastMessageReceived
        {
            get
            {
                return HelperConverter.DateTimeToRough(this.ChatModel.LastMessageReceived, true, false);
            }
        }

        /// <summary>
        ///     Gets the online bookmarks count.
        /// </summary>
        public int OnlineBookmarksCount
        {
            get
            {
                return this.ChatModel.OnlineBookmarks == null ? 0 : this.ChatModel.OnlineBookmarks.Count();
            }
        }

        /// <summary>
        ///     Gets the online count.
        /// </summary>
        public int OnlineCount
        {
            get
            {
                return this.ChatModel.OnlineCharacters.Count();
            }
        }

        /// <summary>
        ///     Gets the online count change.
        /// </summary>
        public string OnlineCountChange
        {
            get
            {
                return this.minuteOnlineCount.GetDisplayString();
            }
        }

        /// <summary>
        ///     Gets the online friends count.
        /// </summary>
        public int OnlineFriendsCount
        {
            get
            {
                return this.ChatModel.OnlineFriends == null ? 0 : this.ChatModel.OnlineFriends.Count();
            }
        }

        /// <summary>
        ///     Gets the rough client up time.
        /// </summary>
        public string RoughClientUpTime
        {
            get
            {
                return HelperConverter.DateTimeToRough(this.ChatModel.ClientUptime, true, false);
            }
        }

        /// <summary>
        ///     Gets the rough server up time.
        /// </summary>
        public string RoughServerUpTime
        {
            get
            {
                return HelperConverter.DateTimeToRough(this.ChatModel.ServerUpTime, true, false);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether show notifications.
        /// </summary>
        public bool ShowNotifications
        {
            get
            {
                return ApplicationSettings.ShowNotificationsGlobal;
            }

            set
            {
                ApplicationSettings.ShowNotificationsGlobal = value;
                SettingsDaemon.SaveApplicationSettingsToXml(this.ChatModel.CurrentCharacter.Name);
            }
        }

        /// <summary>
        ///     Gets or sets the update build time.
        /// </summary>
        public string UpdateBuildTime { get; set; }

        /// <summary>
        ///     Gets or sets the update link.
        /// </summary>
        public string UpdateLink { get; set; }

        /// <summary>
        ///     Gets or sets the update name.
        /// </summary>
        public string UpdateName { get; set; }

        /// <summary>
        ///     Gets or sets the volume.
        /// </summary>
        public double Volume
        {
            get
            {
                return ApplicationSettings.Volume;
            }

            set
            {
                ApplicationSettings.Volume = value;
                SettingsDaemon.SaveApplicationSettingsToXml(this.ChatModel.CurrentCharacter.Name);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The logged in event.
        /// </summary>
        /// <param name="payload">
        /// The payload.
        /// </param>
        public void LoggedInEvent(bool? payload)
        {
            this.updateTimer.Elapsed -= this.UpdateConnectText;
            this.OnPropertyChanged("IsConnecting");
        }

        /// <summary>
        /// The login failed event.
        /// </summary>
        /// <param name="error">
        /// The error.
        /// </param>
        public void LoginFailedEvent(string error)
        {
            if (this.ChatModel.IsAuthenticated)
            {
                this.updateTimer.Elapsed += this.UpdateConnectText;
                this.ChatModel.IsAuthenticated = false;
            }

            this.inStagger = true;
            this.flavorText = new StringBuilder(error);

            this.flavorText.Append("\nStaggering connection");
            this.ConnectTime = 0;

            this.OnPropertyChanged("IsConnecting");
        }

        /// <summary>
        /// The login reconnecting event.
        /// </summary>
        /// <param name="payload">
        /// The payload.
        /// </param>
        public void LoginReconnectingEvent(string payload)
        {
            this.inStagger = false;
            if (this.ChatModel.IsAuthenticated)
            {
                this.updateTimer.Elapsed += this.UpdateConnectText;
                this.ChatModel.IsAuthenticated = false;
            }

            this.flavorText = new StringBuilder("Attempting reconnect");
            this.ConnectTime = 0;
            this.OnPropertyChanged("IsConnecting");
        }

        /// <summary>
        ///     The online count prime.
        /// </summary>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        public int OnlineCountPrime()
        {
            return this.ChatModel.OnlineCharacters.Count();
        }

        /// <summary>
        /// The update connect text.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public void UpdateConnectText(object sender, EventArgs e)
        {
            if (!this.ChatModel.IsAuthenticated)
            {
                this.ConnectTime++;

                if (this.connectDotDot.Length >= 3)
                {
                    this.connectDotDot.Clear();
                }

                this.connectDotDot.Append('.');

                this.OnPropertyChanged("ConnectFlavorText");
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="isManaged">
        /// The is managed dispose.
        /// </param>
        protected override void Dispose(bool isManaged)
        {
            this.Dispose();

            if (!isManaged)
            {
                return;
            }

            this.updateTimer.Dispose();
            this.minuteOnlineCount.Dispose();
            this.Model = null;
        }

        /// <summary>
        ///     The send message.
        /// </summary>
        protected override void SendMessage()
        {
            this.UpdateError("Cannot send messages to this channel!");
        }

        #endregion
    }
}