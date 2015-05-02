#region Copyright

// <copyright file="IPermissionService.cs">
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
    ///     This is used to determine the permissions of a given character.
    /// </summary>
    public interface IGetPermissions
    {
        /// <summary>
        ///     Determines whether the specified character is a moderator or greater.
        /// </summary>
        bool IsModerator(string name);

        /// <summary>
        ///     Determines whether the specified character is an admin.
        /// </summary>
        bool IsAdmin(string name);

        /// <summary>
        ///     Determines whether the specified character is a channel moderator.
        /// </summary>
        bool IsChannelModerator(string name);
    }
}