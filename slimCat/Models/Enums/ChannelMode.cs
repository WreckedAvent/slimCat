#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelMode.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Models
{
    /// <summary>
    ///     Represents possible channel modes
    /// </summary>
    public enum ChannelMode
    {
        /// <summary>
        ///     Channels that only allow ads.
        /// </summary>
        Ads,

        /// <summary>
        ///     Channels that only allow chatting.
        /// </summary>
        Chat,

        /// <summary>
        ///     Channels that allow ads and chatting.
        /// </summary>
        Both,
    }
}