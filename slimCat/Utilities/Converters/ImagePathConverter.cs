#region Copyright

// <copyright file="ImagePathConverter.cs">
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

    using System;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    #endregion

    public class ImagePathConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            if (value == null) return null;

            var uri = new Uri(value.ToString(), UriKind.RelativeOrAbsolute);
            var imageBrush = new ImageBrush {ImageSource = new BitmapImage(uri)};
            return imageBrush;
        }
    }
}