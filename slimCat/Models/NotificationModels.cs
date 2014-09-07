#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationModels.cs">
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
    #region Usings

    using System;
    using System.Windows.Documents;
    using Microsoft.Practices.Prism.Events;
    using Services;
    using ViewModels;

    #endregion

    /// <summary>
    ///     The notification model.
    /// </summary>
    public abstract class NotificationModel : MessageBase, IViewableObject, ICanNavigate, IDisplayToast
    {
        public abstract void Navigate(IChatState chatState);

        public abstract Block View { get; }

        public abstract void DisplayNewToast(IChatState chatState, IManageToasts toastsManager);
    }

    public interface ICanNavigate
    {
        void Navigate(IChatState chatState);
    }

    public interface IDisplayToast
    {
        void DisplayNewToast(IChatState chatState, IManageToasts toastsManager);
    }

    public interface IManageToasts
    {
        Action FlashWindow { get; }

        Action PlaySound { get; }

        Action<NotificationModel> AddNotification { get; }

        ToastNotificationsViewModel Toast { get; }

        Action ShowToast { get; }
    }
}