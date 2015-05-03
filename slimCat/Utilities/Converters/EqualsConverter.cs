#region Copyright

// <copyright file="EqualsConverter.cs">
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
    using System.Globalization;
    using System.Windows.Data;

    #endregion

    public class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type type, object parameter, CultureInfo cultureInfo)
            => value.Equals(parameter);

        public object ConvertBack(object value, Type type, object parameter, CultureInfo cultureInfo)
            => value.Equals(true) ? parameter : Binding.DoNothing;
    }
}