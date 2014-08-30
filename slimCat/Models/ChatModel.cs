#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChatModel.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Collections.ObjectModel;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Contains most chat data which spans channels. Channel-wide UI binds to this.
    /// </summary>
    public class ChatModel : SysProp, IChatModel
    {
        #region Fields

        private readonly ObservableCollection<GeneralChannelModel> channels =
            new ObservableCollection<GeneralChannelModel>();

        private readonly ObservableCollection<NotificationModel> notifications =
            new ObservableCollection<NotificationModel>();

        private readonly ObservableCollection<GeneralChannelModel> ourChannels =
            new ObservableCollection<GeneralChannelModel>();

        private readonly ObservableCollection<PmChannelModel> pms = new ObservableCollection<PmChannelModel>();

        private IAccount account;
        private ChannelModel currentChannel;

        private ICharacter currentCharacter;

        private bool isAuthenticated;
        private ProfileData currentCharacterData;

        #endregion

        #region Public Events

        /// <summary>
        ///     The selected channel changed.
        /// </summary>
        public event EventHandler SelectedChannelChanged;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the all channels.
        /// </summary>
        public ObservableCollection<GeneralChannelModel> AllChannels
        {
            get { return channels; }
        }

        /// <summary>
        ///     Gets or sets the client uptime.
        /// </summary>
        public DateTimeOffset ClientUptime { get; set; }

        /// <summary>
        ///     Gets the current channels.
        /// </summary>
        public ObservableCollection<GeneralChannelModel> CurrentChannels
        {
            get { return ourChannels; }
        }

        /// <summary>
        ///     Gets the current private messages.
        /// </summary>
        public ObservableCollection<PmChannelModel> CurrentPms
        {
            get { return pms; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get { return isAuthenticated; }

            set
            {
                isAuthenticated = value;
                OnPropertyChanged("IsAuthenticated");
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is global moderator.
        /// </summary>
        public bool IsGlobalModerator { get; set; }

        /// <summary>
        ///     Gets or sets the last message received.
        /// </summary>
        public DateTimeOffset LastMessageReceived { get; set; }

        /// <summary>
        ///     Gets the notifications.
        /// </summary>
        public ObservableCollection<NotificationModel> Notifications
        {
            get { return notifications; }
        }

        /// <summary>
        ///     Gets or sets the our account.
        /// </summary>
        public IAccount CurrentAccount
        {
            get { return account; }

            set
            {
                account = value;
                OnPropertyChanged("OurAccount");
            }
        }

        /// <summary>
        ///     Gets or sets the current channel.
        /// </summary>
        public ChannelModel CurrentChannel
        {
            get { return currentChannel; }

            set
            {
                if (currentChannel == value || value == null)
                    return;

                currentChannel = value;

                if (SelectedChannelChanged != null)
                    SelectedChannelChanged(this, new EventArgs());

                OnPropertyChanged("CurrentChannel");
            }
        }

        /// <summary>
        ///     Gets or sets the current character.
        /// </summary>
        public ICharacter CurrentCharacter
        {
            get { return currentCharacter; }

            set
            {
                currentCharacter = value;
                OnPropertyChanged("CurrentCharacter");
            }
        }

        public ProfileData CurrentCharacterData
        {
            get { return currentCharacterData; }
            set 
            { 
                currentCharacterData = value;
                OnPropertyChanged("CurrentCharacterData"); 
            }
        }

        /// <summary>
        ///     Gets or sets the server up time.
        /// </summary>
        public DateTimeOffset ServerUpTime { get; set; }

        public bool AutoReplyEnabled { get; set; }
        public string AutoReplyMessage { get; set; }

        #endregion

        #region Public Methods and Operators

        public ChannelModel FindChannel(string id, string title = null)
        {
            var channel = AllChannels.FirstByIdOrNull(id);

            return channel ?? new GeneralChannelModel(id, ChannelType.InviteOnly) {Title = title};
        }

        #endregion
    }
}