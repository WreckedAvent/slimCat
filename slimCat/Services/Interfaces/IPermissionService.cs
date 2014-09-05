#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPermissionService.cs">
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
    public interface IPermissionService
    {
        /// <summary>
        ///     Determines whether the specified character is a moderator or greater.
        /// </summary>
        /// <param name="name">The character's name to check.</param>
        bool IsModerator(string name);

        /// <summary>
        ///     Determines whether the specified character is an admin.
        /// </summary>
        /// <param name="name">The character's name to check.</param>
        bool IsAdmin(string name);

        /// <summary>
        ///     Determines whether the specified character is a channel moderator.
        /// </summary>
        /// <param name="name">The character's name to check.</param>
        bool IsChannelModerator(string name);
    }
}