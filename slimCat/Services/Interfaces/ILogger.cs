#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILogger.cs">
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


// TODO: This needs to become a singleton resolved by the container
namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;

    #endregion

    /// <summary>
    ///     Represents a logging mechanism.
    /// </summary>
    public interface ILogger
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Returns the last few messages from a given channel
        /// </summary>
        /// <param name="title">
        ///     The Title.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        /// <returns>
        ///     The <see cref="IEnumerable{T}" />.
        /// </returns>
        IEnumerable<string> GetLogs(string title, string id);

        /// <summary>
        ///     Logs a given message in a given channel
        /// </summary>
        /// <param name="title">
        ///     The title.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        /// <param name="message">
        ///     The message.
        /// </param>
        void LogMessage(string title, string id, IMessage message);

        /// <summary>
        ///     Opens the log in the default text editor
        /// </summary>
        /// <param name="isFolder">
        ///     The is Folder.
        /// </param>
        /// <param name="title">
        ///     The Title.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        void OpenLog(bool isFolder, string title = null, string id = null);

        #endregion
    }
}