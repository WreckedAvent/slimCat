#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationModels.cs">
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
    #region Usings

    using System.Windows.Documents;

    #endregion

    /// <summary>
    ///     The notification model.
    /// </summary>
    public abstract class NotificationModel : MessageBase, IViewableObject
    {
        public abstract Block View { get; }
    }
}