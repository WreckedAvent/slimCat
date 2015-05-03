#region Copyright

// <copyright file="ImageWidthConverter.cs">
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
    public class ImageWidthConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            var imageWidthString = value as string;
            try
            {
                return int.Parse(imageWidthString);
            }
            catch
            {
            }

            return 150;
        }
    }
}