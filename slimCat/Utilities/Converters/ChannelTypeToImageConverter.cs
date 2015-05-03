#region Copyright

// <copyright file="ChannelTypeToImageConverter.cs">
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
    using System.Windows.Media.Imaging;
    using Models;

    #endregion

    /// <summary>
    ///     Converts a channel type enum into a channel type image representation.
    /// </summary>
    public sealed class ChannelTypeToImageConverter : OneWayConverter
    {
        public override object Convert(object value, object parameter)
        {
            var args = (ChannelType) value;
            var uri = new Uri("pack://application:,,,/icons/chat.png");
            switch (args)
            {
                case ChannelType.PrivateMessage:
                    uri = new Uri("pack://application:,,,/icons/chat.png");
                    break;
                case ChannelType.InviteOnly:
                    uri = new Uri("pack://application:,,,/icons/private_closed.png");
                    break;
                case ChannelType.Private:
                    uri = new Uri("pack://application:,,,/icons/private_open.png");
                    break;
                case ChannelType.Public:
                    uri = new Uri("pack://application:,,,/icons/public.png");
                    break;
            }

            return new BitmapImage(uri);
        }
    }
}