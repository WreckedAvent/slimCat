#region Copyright

// <copyright file="NameplateMessageColorConverter.cs">
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
    using Services;

    #endregion

    /// <summary>
    ///     Converts a character's interested level to a nameplate color for a message. Accounts for message being of interest.
    /// </summary>
    public sealed class NameplateMessageColorConverter : GenderColorConverterBase
    {
        public NameplateMessageColorConverter(IGetPermissions permissions, ICharacterManager characters)
            : base(permissions, characters)
        {
        }

        public NameplateMessageColorConverter()
            : base(null, null)
        {
        }

        public override object Convert(object value, object parameter)
        {
            if (!(value is IMessage))
                return Application.Current.FindResource("ForegroundBrush");

            var message = (IMessage) value;
            return GetBrush(message.Poster);
        }
    }
}