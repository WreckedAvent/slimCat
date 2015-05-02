#region Copyright

// <copyright file="IAutomationService.cs">
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
    ///     This manages automatic actions in slimCat, like ad deduplication and automatic status changing.
    /// </summary>
    public interface IAutomateThings
    {
        /// <summary>
        ///     Resets the status timers, causing things like auto-away to reset.
        /// </summary>
        void ResetStatusTimers();

        /// <summary>
        ///     Determines whether the specified name and message is a duplicate ad.
        /// </summary>
        bool IsDuplicateAd(string name, string message);

        /// <summary>
        ///     Acknowledgment that a user did an action. Used to reset some things and otherwise be aware of how active a user is.
        /// </summary>
        void UserDidAction();
    }
}