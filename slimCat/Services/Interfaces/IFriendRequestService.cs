#region Copyright

// <copyright file="IFriendRequestService.cs">
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
    ///     Gets our incoming/outgoing friend requests.
    /// </summary>
    public interface IFriendRequestService
    {
        /// <summary>
        ///     Updates the pending friend requests.
        /// </summary>
        void UpdatePendingRequests();

        /// <summary>
        ///     Updates the outgoing friend requests.
        /// </summary>
        void UpdateOutgoingRequests();

        /// <summary>
        ///     Attempts to get the request with the returned id for the specified character name.
        /// </summary>
        int? GetRequestForCharacter(string character);
    }
}