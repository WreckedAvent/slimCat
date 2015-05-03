#region Copyright

// <copyright file="CategoryConverter.cs">
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

namespace slimCat.ViewModels
{
    #region Usings

    using System.Globalization;
    using Utilities;

    #endregion

    internal class CategoryConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            var toReturn = CultureInfo.CurrentCulture.TextInfo.ToTitleCase((string) value);

            return toReturn.Equals("Furryprefs") ? "Furry Preference" : toReturn;
        }
    }
}