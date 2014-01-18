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

        public const string ClientVer = "rc3.04";

        #endregion

        #region Static Fields

        public static readonly string FriendlyName = ClientId + ' ' + ClientName + ' ' + ClientVer;

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