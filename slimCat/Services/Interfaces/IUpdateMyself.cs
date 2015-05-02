#region Copyright

// <copyright file="IUpdateMyself.cs">
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

    using System.Threading.Tasks;
    using Models;

    #endregion

    /// <summary>
    ///     Represents a service for getting the latest version of slimCat and config.
    /// </summary>
    public interface IUpdateMyself
    {
        /// <summary>
        ///     Gets the latest slimCat config asynchronously.
        /// </summary>
        Task<LatestConfig> GetLatestAsync();

        /// <summary>
        ///     Tries to update slimCat in-place asynchronously.
        /// </summary>
        Task<bool> TryUpdateAsync();
    }
}