#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IListConnection.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;

    #endregion

    /// <summary>
    ///     Represents an HTTP endpoint for F-list, mostly for API endpoints
    /// </summary>
    public interface IListConnection
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Uploads a lot to F-list.net f.e reporting a user
        /// </summary>
        /// <param name="report">
        ///     relevant data about the report
        /// </param>
        /// <param name="log">
        ///     the log to upload
        /// </param>
        /// <returns>
        ///     an int corresponding to the logid the server assigned
        /// </returns>
        int UploadLog(ReportModel report, IEnumerable<IMessage> log);

        /// <summary>
        ///     Gets an F-list API ticket
        /// </summary>
        /// <param name="sendUpdate">
        ///     The send Update.
        /// </param>
        void GetTicket(bool sendUpdate);

        #endregion
    }
}