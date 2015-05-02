#region Copyright

// <copyright file="IHandleIcons.cs">
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
    ///     Represents a service for updating our taskbar icon.
    /// </summary>
    public interface IHandleIcons
    {
        /// <summary>
        ///     Toggles the sound on/off.
        /// </summary>
        void ToggleSound();

        /// <summary>
        ///     Toggles the toasts on/off.
        /// </summary>
        void ToggleToasts();

        /// <summary>
        ///     Sets the icon notification level.
        /// </summary>
        void SetIconNotificationLevel(bool newMsg);
    }
}