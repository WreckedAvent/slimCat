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

namespace System
{
    /// <summary>
    ///     The constants.
    /// </summary>
    public static class Constants
    {
        #region Constants

        /// <summary>
        ///     The clien t_ id.
        /// </summary>
        public const string CLIENT_ID = "slimCat";

        /// <summary>
        ///     The clien t_ name.
        /// </summary>
        public const string CLIENT_NAME = "Ocelot";

        /// <summary>
        ///     The clien t_ ver.
        /// </summary>
        public const string CLIENT_VER = "rc2.00";

        #endregion

        #region Static Fields

        /// <summary>
        ///     The friendl y_ name.
        /// </summary>
        public static string FRIENDLY_NAME = CLIENT_ID + ' ' + CLIENT_NAME + ' ' + CLIENT_VER;

        #endregion

        /// <summary>
        ///     The url constants.
        /// </summary>
        public static class UrlConstants
        {
            #region Constants

            /// <summary>
            ///     The api.
            /// </summary>
            public const string API = DOMAIN + @"/json/api/";

            /// <summary>
            ///     The domain.
            /// </summary>
            public const string DOMAIN = @"http://www.f-list.net";

            /// <summary>
            ///     The ge t_ ticket.
            /// </summary>
            public const string GET_TICKET = DOMAIN + @"/json/getApiTicket.php";

            // not-so-api
            /// <summary>
            ///     The login.
            /// </summary>
            public const string LOGIN = DOMAIN + @"/action/script_login.php";

            /// <summary>
            ///     The rea d_ log.
            /// </summary>
            public const string READ_LOG = DOMAIN + @"/fchat/getLog.php?log=";

            /// <summary>
            ///     The rea d_ note.
            /// </summary>
            public const string READ_NOTE = DOMAIN + @"/view_note.php?note_id=";

            /// <summary>
            ///     The uploa d_ log.
            /// </summary>
            public const string UPLOAD_LOG = DOMAIN + @"/fchat/submitLog.php";

            #endregion

            // api
        }
    }
}