#region Copyright

// <copyright file="IBrowseThings.cs">
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
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    ///     Represents a wrapper around a web browser to get pages from the internet. It also handles F-list CSRF.
    /// </summary>
    public interface IBrowseThings
    {
        /// <summary>
        ///     Synchronously POSTS to the given host with the given arguments. Can use cookies for keeping session. Returns a
        ///     serialized string.
        /// </summary>
        string GetResponse(string host, IDictionary<string, object> arguments,
            bool useCookies = false);

        /// <summary>
        ///     Synchronously GETs to the given host. Can use cookies for keeping session. Returns a serialized string.
        /// </summary>
        string GetResponse(string host, bool useCookies = false);

        /// <summary>
        ///     Asynchronously GETs to the given host. Can use cookies for keeping session. Returns a serialized string.
        /// </summary>
        Task<string> GetResponseAsync(string host, bool useCookies = false);
    }
}