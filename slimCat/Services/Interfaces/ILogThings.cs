#region Copyright

// <copyright file="ILogThings.cs">
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
    ///     Represents a logging mechanism.
    /// </summary>
    public interface ILogThings
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Returns the last few messages from a given channel
        /// </summary>
        IEnumerable<string> GetLogs(string title, string id);

        /// <summary>
        ///     Logs a given message in a given channel
        /// </summary>
        void LogMessage(string title, string id, IMessage message);

        /// <summary>
        ///     Opens the log in the default text editor
        /// </summary>
        void OpenLog(bool isFolder, string title = null, string id = null);

        #endregion
    }
}