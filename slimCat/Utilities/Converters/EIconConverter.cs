#region Copyright

// <copyright file="CharacterAvatarConverter.cs">
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
    using System.Net.Cache;
    using System.Windows.Media.Imaging;
    using slimCat.Models;

    #endregion

    public class EIconConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            if (value == null) return null;

            var character = (string)value;
            return ApplicationSettings.AnimateIcons
                ? new Uri(Constants.UrlConstants.EIcon + character.ToLower() + ".gif", UriKind.Absolute)
                : new Uri(Constants.UrlConstants.EIcon + character.ToLower() + ".png", UriKind.Absolute);
        }
    }
}