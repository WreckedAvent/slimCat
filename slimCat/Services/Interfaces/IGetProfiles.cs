#region Copyright

// <copyright file="IGetProfiles.cs">
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
    /// <summary>
    ///     Represents endpoints for retrieving information on profiles.
    /// </summary>
    public interface IGetProfiles
    {
        /// <summary>
        ///     Retrieves the full profile data for the selected character.
        /// </summary>
        void GetProfileDataAsync(string character);

        /// <summary>
        ///     Clears the cache for the specified character.
        /// </summary>
        void ClearCache(string character);
    }
}