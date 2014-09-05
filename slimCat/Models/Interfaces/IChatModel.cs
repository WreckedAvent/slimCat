#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IChatModel.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    #endregion

    /// <summary>
    ///     Represents data relating to chat that isn't specific to characters.
    /// </summary>
    public interface IChatModel
    {
        #region Public Events

        /// <summary>
        ///     The selected channel changed.
        /// </summary>
        event EventHandler SelectedChannelChanged;

        event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets all channels, opened or not.
        /// </summary>
        ObservableCollection<GeneralChannelModel> AllChannels { get; }

        /// <summary>
        ///     Gets or sets the client uptime.
        /// </summary>
        DateTimeOffset ClientUptime { get; set; }

        /// <summary>
        ///     Gets the current opened channels.
        /// </summary>
        ObservableCollection<GeneralChannelModel> CurrentChannels { get; }

        /// <summary>
        ///     Gets the current opened PMs.
        /// </summary>
        ObservableCollection<PmChannelModel> CurrentPms { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the client is connected to the server.
        /// </summary>
        bool IsAuthenticated { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the current user is a global moderator.
        /// </summary>
        bool IsGlobalModerator { get; set; }

        /// <summary>
        ///     Gets or sets the last message received.
        /// </summary>
        DateTimeOffset LastMessageReceived { get; set; }

        /// <summary>
        ///     Gets the current notifications.
        /// </summary>
        ObservableCollection<NotificationModel> Notifications { get; }

        /// <summary>
        ///     Gets or sets the current account.
        /// </summary>
        IAccount CurrentAccount { get; set; }


        /// <summary>
        ///     Gets or sets the currently-selected channel.
        /// </summary>
        ChannelModel CurrentChannel { get; set; }


        /// <summary>
        ///     Gets or sets the currently-selected character.
        /// </summary>
        ICharacter CurrentCharacter { get; set; }

        ProfileData CurrentCharacterData { get; set; }

        /// <summary>
        ///     Gets or sets the server up time.
        /// </summary>
        DateTimeOffset ServerUpTime { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether automatic reply is enabled.
        /// </summary>
        bool AutoReplyEnabled { get; set; }

        /// <summary>
        ///     Gets or sets the automatic reply message.
        /// </summary>
        string AutoReplyMessage { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Returns the ChannelModel for a given id/title, if it exists
        /// </summary>
        /// <param name="id">ID of the channel to find</param>
        /// <param name="title">Title of the channel used to create if not existant</param>
        /// <returns></returns>
        ChannelModel FindChannel(string id, string title = null);

        #endregion
    }
}