#region Copyright

// <copyright file="BoolColorConverter.cs">
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

    #endregion

    /// <summary>
    ///     If true, return a bright color
    /// </summary>
    public class BoolColorConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            var v = (bool) value;
            if (v)
                return Application.Current.FindResource("HighlightBrush") as SolidColorBrush;

            return Application.Current.FindResource("BrightBackgroundBrush") as SolidColorBrush;
        }
    }
}