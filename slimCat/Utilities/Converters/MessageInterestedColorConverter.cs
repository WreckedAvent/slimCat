#region Copyright

// <copyright file="MessageInterestedColorConverter.cs">
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

    /// <summary>
    ///     Converts a message's of interest / not state to an appropriate foreground
    /// </summary>
    public sealed class MessageInterestedColorConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            if (!(value is IMessage))
                return Application.Current.FindResource("BackgroundBrush");

            var message = (IMessage) value;

            return Application.Current.FindResource(message.IsOfInterest ? "HighlightBrush" : "BackgroundBrush");
        }
    }
}