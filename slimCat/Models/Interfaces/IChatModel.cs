#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IChatModel.cs">
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

namespace Slimcat.Models
{
    #region Usings

    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    #endregion

    /// <summary>
    ///     The ChatModel interface.
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
        ///     A collection of ALL channels, public or private
        /// </summary>
        ObservableCollection<GeneralChannelModel> AllChannels { get; }

        /// <summary>
        ///     Gets or sets the client uptime.
        /// </summary>
        DateTimeOffset ClientUptime { get; set; }

        /// <summary>
        ///     A colleciton of all opened channels
        /// </summary>
        ObservableCollection<GeneralChannelModel> CurrentChannels { get; }

        /// <summary>
        ///     A collection of all opened Pms
        /// </summary>
        ObservableCollection<PmChannelModel> CurrentPms { get; }

        /// <summary>
        ///     If we're actively connected and authenticated through F-Chat
        /// </summary>
        bool IsAuthenticated { get; set; }

        /// <summary>
        ///     Whether or not the current user has permissions to act like a moderator
        /// </summary>
        bool IsGlobalModerator { get; set; }

        /// <summary>
        ///     Gets or sets the last message received.
        /// </summary>
        DateTimeOffset LastMessageReceived { get; set; }

        /// <summary>
        ///     A collection of all of our notifications
        /// </summary>
        ObservableCollection<NotificationModel> Notifications { get; }

        /// <summary>
        ///     Information relating to the currently selected account
        /// </summary>
        IAccount CurrentAccount { get; set; }

        /// <summary>
        ///     The Channel we have selected as the 'active' one
        /// </summary>
        ChannelModel CurrentChannel { get; set; }

        /// <summary>
        ///     The Character we've chosen to enter chat with
        /// </summary>
        ICharacter CurrentCharacter { get; set; }

        /// <summary>
        ///     Gets or sets the server up time.
        /// </summary>
        DateTimeOffset ServerUpTime { get; set; }

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