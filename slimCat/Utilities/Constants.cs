// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Justin Kadrovach">
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
//   The constants.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Utilities
{
    /// <summary>
    ///     The constants.
    /// </summary>
    public static class Constants
    {
        #region Constants

        public const string ClientID = "slimCat";

        public const string ClientName = "Ocelot";

        public const string ClientVer = "rc3.01";

        #endregion

        #region Static Fields

        public static readonly string FriendlyName = ClientID + ' ' + ClientName + ' ' + ClientVer;

        #endregion

        /// <summary>
        ///     The url constants.
        /// </summary>
        public static class UrlConstants
        {
            #region Constants

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

            #endregion
        }
    }
}