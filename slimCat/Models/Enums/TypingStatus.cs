#region Copyright

// <copyright file="TypingStatus.cs">
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

namespace slimCat.Models
{
    /// <summary>
    ///     The possible states of typing
    /// </summary>
    public enum TypingStatus
    {
        /// <summary>
        ///     User is not typing and does not have anything typed.
        /// </summary>
        Clear,

        /// <summary>
        ///     User is not typing but has something typed out already.
        /// </summary>
        Paused,

        /// <summary>
        ///     User is typing actively.
        /// </summary>
        Typing
    }
}