namespace Slimcat.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

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
        ///     A list of all bookmarked characters
        /// </summary>
        IList<string> Bookmarks { get; }

        /// <summary>
        ///     Gets or sets the client uptime.
        /// </summary>
        DateTimeOffset ClientUptime { get; set; }

        /// <summary>
        ///     A colleciton of all opened channels
        /// </summary>
        ObservableCollection<GeneralChannelModel> CurrentChannels { get; }

        /// <summary>
        ///     A collection of all opened PMs
        /// </summary>
        ObservableCollection<PMChannelModel> CurrentPMs { get; }

        /// <summary>
        ///     Gets the friends.
        /// </summary>
        IList<string> Friends { get; }

        /// <summary>
        ///     Gets the ignored.
        /// </summary>
        IList<string> Ignored { get; }

        /// <summary>
        ///     Gets the interested.
        /// </summary>
        IList<string> Interested { get; }

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
        ///     A list of all global moderators
        /// </summary>
        IList<string> Mods { get; }

        /// <summary>
        ///     Gets the not interested.
        /// </summary>
        IList<string> NotInterested { get; }

        /// <summary>
        ///     A collection of all of our notifications
        /// </summary>
        ObservableCollection<NotificationModel> Notifications { get; }

        /// <summary>
        ///     A list of all online characters who are bookmarked
        /// </summary>
        IEnumerable<ICharacter> OnlineBookmarks { get; }

        /// <summary>
        ///     A list of all online characters
        /// </summary>
        IEnumerable<ICharacter> OnlineCharacters { get; }

        /// <summary>
        ///     A list of all online characters who are friends
        /// </summary>
        IEnumerable<ICharacter> OnlineFriends { get; }

        /// <summary>
        ///     A list of all online global moderators
        /// </summary>
        IEnumerable<ICharacter> OnlineGlobalMods { get; }

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
        /// The add character.
        /// </summary>
        /// <param name="character">
        /// The character.
        /// </param>
        void AddCharacter(ICharacter character);

        /// <summary>
        /// Returns the ICharacter value of a given string, if online
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="ICharacter"/>.
        /// </returns>
        ICharacter FindCharacter(string name);

        /// <summary>
        /// The is of interest.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        bool IsOfInterest(string name);

        /// <summary>
        /// Checks if a given user is online
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        bool IsOnline(string name);

        /// <summary>
        /// The remove character.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        void RemoveCharacter(string name);

        /// <summary>
        /// Toggle our interest in a character
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        void ToggleInterestedMark(string name);

        /// <summary>
        /// Toggle our disinterested in a character
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        void ToggleNotInterestedMark(string name);

        #endregion

        void FriendsChanged();
    }
}