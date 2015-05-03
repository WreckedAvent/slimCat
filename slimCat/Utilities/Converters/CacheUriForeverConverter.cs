#region Copyright

// <copyright file="CacheUriForeverConverter.cs">
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

    #endregion

    public class CacheUriForeverConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            if (value == null) return null;

            var character = (Uri) value;
            var bi = new BitmapImage();
            bi.BeginInit();

            bi.UriSource = character;
            bi.CacheOption = BitmapCacheOption.OnDemand;
            bi.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);
            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

            bi.EndInit();

            return bi;
        }
    }
}