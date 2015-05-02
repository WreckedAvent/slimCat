#region Copyright

// <copyright file="IHandleApi.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;

    #endregion

    /// <summary>
    ///     Talks to F-list api, such as getting new tickets and uploading logs.
    /// </summary>
    public interface IHandleApi
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Uploads a log to f-list. Used in reporting.
        /// </summary>
        int UploadLog(ReportModel report, IEnumerable<IMessage> log);

        /// <summary>
        ///     Gets an F-list API ticket.
        /// </summary>
        void GetTicket(bool sendUpdate);

        #endregion
    }
}