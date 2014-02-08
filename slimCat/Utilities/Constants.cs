#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs">
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

namespace slimCat.Utilities
{
    /// <summary>
    ///     The constants.
    /// </summary>
    public static class Constants
    {
        #region Constants

        public const string ClientId = "slimCat";

        public const string ClientName = "Ocelot";

        public const string ClientVer = "rc3.08";

        public const string ServerHost = "wss://chat.f-list.net:9799/";

        #endregion

        #region Static Fields

        public static readonly string FriendlyName = ClientId + ' ' + ClientName + ' ' + ClientVer;

        #endregion

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
            public const string MultipleCharacters = "characters";
            public const string Action = "action";
            public const string Type = "type";
            public const string Title = "title";
            public const string Mode = "mode";
            public const string Sender = "sender";
            public const string Report = "report";
            public const string LogId = "logid";
            public const string CallId = "callid";
            public const string Status = "status";
            public const string StatusMessage = "statusmsg";
            public const string ThisCharacter = "_thisCharacter";

            public const string ActionReport = "report";
            public const string ActionDelete = "delete";
            public const string ActionConfirm = "confirm";
            public const string ActionAdd = "add";
            public const string ActionNotify = "notify";
        }

        /// <remarks>
        ///     Some of these will be the same as server commands;
        ///     there's no contract for them to be the same command code, though.
        /// </remarks>
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
            public const int Timeout = 39;
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
            ///     The url for the root api.
            /// </summary>
            public const string Api = Domain + @"/json/api/";

            /// <summary>
            ///     The url for f-list.
            /// </summary>
            public const string Domain = @"https://www.f-list.net";

            /// <summary>
            ///     The url for the ticket get script.
            /// </summary>
            public const string GetTicket = Domain + @"/json/getApiTicket.php";

            /// <summary>
            ///     The url for the login script.
            /// </summary>
            public const string Login = Domain + @"/action/script_login.php";

            /// <summary>
            ///     The url for the get log script.
            /// </summary>
            public const string ReadLog = Domain + @"/fchat/getLog.php?log=";

            /// <summary>
            ///     The url for the view note script.
            /// </summary>
            public const string ViewNote = Domain + @"/view_note.php?note_id=";

            /// <summary>
            ///     The url for the upload log script.
            /// </summary>
            public const string UploadLog = Domain + @"/fchat/submitLog.php";
        }
    }
}