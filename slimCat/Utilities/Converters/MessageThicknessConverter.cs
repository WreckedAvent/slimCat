#region Copyright

// <copyright file="MessageThicknessConverter.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System.Windows;
    using Models;

    #endregion

    public sealed class MessageThicknessConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            const int top = 0;
            const int left = 0;

            var bottom = 0;
            var right = 0;

            var message = value as IMessage;
            if (message == null) return new Thickness(left, top, right, bottom);

            if (message.IsLastViewed) bottom = 2;

            else if (message.Type == MessageType.Ad) bottom = 1;

            else if (message.IsOfInterest)
            {
                right = 8;
                bottom = 2;
            }

            return new Thickness(left, top, right, bottom);
        }
    }
}