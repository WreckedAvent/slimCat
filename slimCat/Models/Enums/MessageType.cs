#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageType.cs">
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

namespace slimCat.Models
{
    /// <summary>
    ///     Used to represent possible types of message sent to the client
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        ///     Represents an ad
        /// </summary>
        Ad,

        /// <summary>
        ///     Represents a normal message
        /// </summary>
        Normal,

        /// <summary>
        ///     Represents a dice roll
        /// </summary>
        Roll
    }
}