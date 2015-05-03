#region Copyright

// <copyright file="MessageDelimiterColorConverter.cs">
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
    using System.Windows.Media;
    using Models;

    #endregion

    public sealed class MessageDelimiterColorConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            if (!(value is IMessage))
                return new SolidColorBrush(Colors.Transparent);

            var message = (IMessage) value;

            if (message.IsLastViewed)
                return
                    Application.Current.FindResource(message.Type == MessageType.Ad
                        ? "ContrastBrush"
                        : "BrightBackgroundBrush");

            if (message.IsOfInterest)
                return Application.Current.FindResource("HighlightBrush");

            if (message.Type != MessageType.Normal)
                return Application.Current.FindResource("BrightBackgroundBrush");

            return new SolidColorBrush(Colors.Transparent);
        }
    }
}