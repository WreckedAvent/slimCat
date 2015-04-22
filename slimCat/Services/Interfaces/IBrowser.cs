#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IBrowser.cs">
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
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    ///     Represents an endpoint for bi-directional HTTP requests.
    /// </summary>
    public interface IBrowser
    {
        /// <summary>
        ///     Gets the response from the host.
        /// </summary>
        /// <param name="host">The host of the endpoint.</param>
        /// <param name="arguments">The arguments to serialize and send.</param>
        /// <param name="useCookies">if set to <c>true</c> then cookies will be saved/used.</param>
        /// <returns>
        ///     The full response from the endpoint serialized to a string.
        /// </returns>
        string GetResponse(string host, IDictionary<string, object> arguments,
            bool useCookies = false);

        string GetResponse(string host, bool useCookies = false);

        Task<string> GetResponseAsync(string host, bool useCookies = false);
    }
}