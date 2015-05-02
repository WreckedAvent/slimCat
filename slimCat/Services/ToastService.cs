#region Copyright

// <copyright file="ToastService.cs">
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
    #region Usings

    using System;
    using Models;
    using ViewModels;

    #endregion

    public class ToastService : IManageToasts
    {
        public Action FlashWindow { get; set; }
        public Action PlaySound { get; set; }
        public Action<NotificationModel> AddNotification { get; set; }
        public ToastNotificationsViewModel Toast { get; set; }
        public Action ShowToast { get; set; }
    }
}