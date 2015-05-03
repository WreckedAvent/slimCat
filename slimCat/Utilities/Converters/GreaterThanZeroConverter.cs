#region Copyright

// <copyright file="GreaterThanZeroConverter.cs">
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

    #endregion

    /// <summary>
    ///     If greater than zero, return visible.
    /// </summary>
    public class GreaterThanZeroConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            try
            {
                var parsed = System.Convert.ToInt32(value);

                return parsed > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch
            {
                return Visibility.Collapsed;
            }
        }
    }
}