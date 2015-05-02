#region Copyright

// <copyright file="Constants.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Globalization;

    #endregion

    /// <summary>
    ///     The constants used by slimCat.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        ///     Constants for various argument names sent to/from the server.
        ///     These are always transmitted as bare strings.
        /// </summary>
        public static class Arguments
        {
            public const string MultipleChannels = "channels";
            public const string Name = "name";
            public const string Character = "character";
            public const string Command = "command";
            public const string Channel = "channel";
            public const string Message = "message";
            public const string MultipleModerators = "ops";
            public const string MultipleUsers = "users";
            public const string Identity = "identity";
            public const string Recipient = "recipient";
            public const string MultipleCharacters = "characters";
            public const string Action = "action";
            public const string Type = "type";
            public const string Title = "title";
            public const string Mode = "mode";
            public const string Sender = "sender";
            public const string Report = "report";
            public const string LogId = "logid";
            public const string CallId = "callid";
            public const string Gender = "gender";
            public const string Status = "status";
            public const string StatusMessage = "statusmsg";
            public const string ThisCharacter = "_thisCharacter";
            public const string ActionReport = "report";
            public const string ActionDelete = "delete";
            public const string ActionConfirm = "confirm";
            public const string ActionAdd = "add";
            public const string ActionNotify = "notify";
        }

        /// <summary>
        ///     Constants for client commands we have.
        /// </summary>
        public static class ClientCommands
        {
            public const string AdminBroadcast = "BRO";
            public const string AdminPromote = "AOP";
            public const string AdminAccountWatch = "AWC"; // possibly depreciated
            public const string AdminDemote = "DOP";
            public const string AdminAlert = "SFC";
            public const string AdminReward = "RWD";
            public const string AdminBan = "ACB";
            public const string AdminKick = "KIK";
            public const string AdminUnban = "UBN";
            public const string AdminTimeout = "TMO";
            public const string ChannelCreate = "CCR";
            public const string ChannelBanList = "CBL";
            public const string ChannelDescription = "CDS";
            public const string ChannelKick = "CKU";
            public const string ChannelPromote = "COA";
            public const string ChannelDemote = "COR";
            public const string ChannelModeratorList = "COL";
            public const string ChannelOwner = "CSO";
            public const string ChannelTimeOut = "CTU";
            public const string ChannelBan = "CBU";
            public const string ChannelUnban = "CUB";
            public const string ChannelJoin = "JCH";
            public const string ChannelLeave = "LCH";
            public const string ChannelMessage = "MSG";
            public const string ChannelRoll = "RLL";
            public const string ChannelMode = "RMO";
            public const string ChannelKind = "RST";
            public const string ChannelAd = "LRP";
            public const string ChannelSetOwner = "CSO";
            public const string ChannelKill = "KIC";
            public const string UserInvite = "CIU";
            public const string UserSearch = "FKS";
            public const string UserIgnore = "IGN";
            public const string UserKinks = "KIN";
            public const string UserMessage = "PRI";
            public const string UserProfile = "PRO";
            public const string UserStatus = "STA";
            public const string UserTyping = "TPN";
            public const string PublicChannelList = "CHA";
            public const string PrivateChannelList = "ORS";
            public const string SystemChannelCreate = "CRC";
            public const string SystemAuthenticate = "IDN";
            public const string SystemPing = "PIN";
            public const string SystemReload = "RLD";
            public const string SystemUptime = "UPT";
        }

        /// <summary>
        ///     Constants for the various errors we can get back from the server.
        /// </summary>
        public static class Errors
        {
            public const int CannotRollInFrontpage = -10; // sigh
            /* -9,
             * -8,
             * -7,
             * -6 */
            public const int UnknownError = -5;
            public const int GeneralError = -2;
            public const int CommandIsNotImplemented = -3;
            public const int ConnectionTimeOut = -4;
            public const int FatalError = -1;
            public const int Successful = 0;
            public const int Syntax = 1;
            public const int NoServerSlots = 2;
            public const int NotLoggedIin = 3;
            public const int BadLoginInfo = 4;
            public const int SentMessagesTooFast = 5;
            public const int UnknownCommand = 8;
            public const int BannedFromServer = 9;
            public const int RequiresAdmin = 10;
            public const int AlreadyLoggedIn = 11;
            // 12
            public const int SentKinkRequestTooFast = 13;
            public const int MessageTooLong = 15;
            public const int CanNotPromote = 16;
            public const int CanNotDemote = 17;
            public const int NoResults = 18;
            public const int RequiresModerator = 19;
            public const int Blocked = 20;
            public const int InvalidActionSubject = 21;
            /* 22,
             * 23,
             * 24,
             * 25 */
            public const int ChannelNotFound = 26;
            // 27
            public const int AlreadyInChannel = 28;
            public const int TooManyConnections = 30;
            public const int SimultaneousLoginKick = 31;
            public const int AlreadyBannedFromServer = 32;
            public const int UnknownLoginMethod = 33;
            /* 34,
             * 35 */
            public const int InvalidRoll = 36;
            // 37
            public const int InvalidTimeout = 38;
            public const int TimedOutFromServer = 39;
            public const int KickedFromServer = 40;
            public const int AlreadyBannedFromChannel = 41;
            public const int CannotUnbanFromChannel = 42;
            // 43
            public const int RequiresInvite = 44;
            public const int RequiresChannel = 45;
            // 46
            public const int InvalidInvite = 47;
            public const int BannedFromChannel = 48;
            public const int CharacterNotFound = 49;
            public const int SentSearchRequestTooFast = 50;
            /* 51,
             * 52,
             * 53 */
            public const int SentModeratorRequestTooFast = 54;
            // 55
            public const int SentAdTooFast = 56;
            /* 57,
             * 58 */
            public const int CannotPostAds = 59;
            public const int CannotSendMessage = 60;
            public const int NoLoginSlots = 62;
            // 63
            public const int IgnoreListTooBig = 64;
            /* 65,
             * 66 */
            public const int ChannelTitleTooLong = 67;
            /* 68,
             * 69,
             * 70,
             * 71 */
            public const int SearchResultsTooBig = 72;
        }

        /// <summary>
        ///     Constants for the server commands we have.
        /// </summary>
        public static class ServerCommands
        {
            public const string AdminPromote = "AOP";
            public const string AdminBroadcast = "BRO";
            public const string AdminDemote = "DOP";
            public const string AdminReport = "SFC";
            public const string ChannelBan = "CBU";
            public const string ChannelUnban = "CUB";
            public const string ChannelDescription = "CDS";
            public const string ChannelKick = "CKU";
            public const string ChannelPromote = "COA";
            public const string ChannelDemote = "COR";
            public const string ChannelModerators = "COL";
            public const string ChannelInitialize = "ICH";
            public const string ChannelJoin = "JCH";
            public const string ChannelLeave = "LCH";
            public const string ChannelOwner = "CSO";
            public const string ChannelMessage = "MSG";
            public const string ChannelAd = "LRP";
            public const string ChannelRoll = "RLL";
            public const string ChannelMode = "RMO";
            public const string ChannelSetOwner = "CSO";
            public const string UserInvite = "CIU";
            public const string UserList = "LIS";
            public const string UserJoin = "NLN";
            public const string UserLeave = "FLN";
            public const string UserKinks = "KID";
            public const string UserProfile = "PRD";
            public const string UserMessage = "PRI";
            public const string UserStatus = "STA";
            public const string UserTyping = "TPN";
            public const string UserIgnore = "IGN";
            public const string AdminList = "ADL";
            public const string PublicChannelList = "CHA";
            public const string PrivateChannelList = "ORS";
            public const string SystemAuthenticate = "IDN";
            public const string SystemBridge = "RTB";
            public const string SystemMessage = "SYS";
            public const string SystemError = "ERR";
            public const string SystemPing = "PIN";
            public const string SystemHello = "HLO";
            public const string SystemCount = "CON";
            public const string SystemUptime = "UPT";
            public const string SystemSettings = "VAR";
            public const string SearchResult = "FKS";
        }

        /// <summary>
        ///     The url constants.
        /// </summary>
        public static class UrlConstants
        {
            /// <summary>
            ///     The url for the base of all api endpoints.
            /// </summary>
            public const string Api = Domain + @"/json/api/";

            /// <summary>
            ///     The url for f-list.
            /// </summary>
            public const string Domain = @"https://www.f-list.net";

            /// <summary>
            ///     The url used to get a new ticket.
            /// </summary>
            public const string GetTicket = Domain + @"/json/getApiTicket.php";

            /// <summary>
            ///     The url used to login.
            /// </summary>
            public const string Login = Domain + @"/action/script_login.php";

            /// <summary>
            ///     The url used to retrieve f-chat report logs.
            /// </summary>
            public const string ReadLog = Domain + @"/fchat/getLog.php?log=";

            /// <summary>
            ///     The url used to view notes.
            /// </summary>
            public const string ViewNote = Domain + @"/view_note.php?note_id=";

            /// <summary>
            ///     The url used to upload f-chat report logs.
            /// </summary>
            public const string UploadLog = Domain + @"/json/api/report-submit.php";

            /// <summary>
            ///     The url used to get a note history.
            /// </summary>
            public const string ViewHistory = Domain + @"/history.php?name=";

            /// <summary>
            ///     The url used to send notes.
            /// </summary>
            public const string SendNote = Domain + @"/json/notes-send.json";

            /// <summary>
            ///     The url used to get the search parameters for f-chat search.
            /// </summary>
            public const string SearchFields = Domain + @"/json/chat-search-getfields.json?ids=true";

            /// <summary>
            ///     The url used to retrieve profile images.
            /// </summary>
            public const string ProfileImages = Domain + @"/json/profile-images.json";

            /// <summary>
            ///     The url used to retrieve the available kinks.
            /// </summary>
            public const string KinkList = Domain + @"/json/api/kink-list.php";

            /// <summary>
            ///     The url used for getting incoming friend requests.
            /// </summary>
            public const string IncomingFriendRequests = Api + "request-list.php";

            /// <summary>
            ///     The url used for getting outgoing friend requests.
            /// </summary>
            public const string OutgoingFriendRequests = Api + "request-pending.php";

            /// <summary>
            ///     The base url for character pages.
            /// </summary>
            public const string CharacterPage = Domain + "/c/";

            /// <summary>
            ///     The base url for the static domain on f-list. Mostly for images.
            /// </summary>
            public const string StaticDomain = @"https://static.f-list.net";

            /// <summary>
            ///     The base url used for getting character avatars.
            /// </summary>
            public const string CharacterAvatar = StaticDomain + @"/images/avatar/";
        }

        #region Static Fields

        /// <summary>
        ///     The client's version.
        /// </summary>
        public static readonly string ClientVersion = Version.ToString("##.00", CultureInfo.InvariantCulture);

        /// <summary>
        ///     The usual time we debounce all searches.
        /// </summary>
        public static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);

        /// <summary>
        ///     The name displayed around and sent to the server.
        /// </summary>
        public static readonly string FriendlyName = $"{ClientId} {ClientNickname} {ClientVersion}";

        #endregion

        #region Constants

        /// <summary>
        ///     The client's identifier.
        /// </summary>
        public const string ClientId = "slimCat";

        /// <summary>
        ///     The client's name. Must be a type of cat.
        /// </summary>
        public const string ClientNickname = "Puma";

        /// <summary>
        ///     The version of the client.
        /// </summary>
        public const double Version = 5.00;

        /// <summary>
        ///     The endpoint for F-chat websocket communication.
        /// </summary>
        public const string ServerHost = "wss://chat.f-list.net:9799/";

        /// <summary>
        ///     The base url used to find updates and themes.
        /// </summary>
        public const string SlimCatResourceUrl = "https://dl.dropbox.com/u/29984849/slimCat/";

        /// <summary>
        ///     The url for checking for new updates.
        /// </summary>
        public const string NewVersionUrl = SlimCatResourceUrl + "latest%20dev.csv";

        /// <summary>
        ///     The url for the index of available themes.
        /// </summary>
        public const string ThemeIndexUrl = SlimCatResourceUrl + "themes/index.csv";

        #endregion
    }
}