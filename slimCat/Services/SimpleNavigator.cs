#region Copyright

// <copyright file="SimpleNavigator.cs">
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

    #endregion

    public class SimpleNavigator : ICanNavigate
    {
        public SimpleNavigator(Action<IChatState> navigateAction)
        {
            Navigate = navigateAction;
        }

        public Action<IChatState> Navigate { get; set; }

        void ICanNavigate.Navigate(IChatState chatState)
        {
            Navigate(chatState);
        }
    }
}