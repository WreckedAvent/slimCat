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

namespace Slimcat.Utilities
{
    /// <summary>
    ///     The constants.
    /// </summary>
    public static class Constants
    {
        #region Constants

        public const string ClientId = "slimCat";

        public const string ClientName = "Ocelot";

        public const string ClientVer = "rc3.06";

        #endregion

        #region Static Fields

        public static readonly string FriendlyName = ClientId + ' ' + ClientName + ' ' + ClientVer;

        #endregion

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

        public static class Arguments
        {
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
            public const string ThisCharacter = "_thisCharacter";
        }
    }
}