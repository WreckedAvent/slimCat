#region Copyright

// <copyright file="NameplateColorConverter.cs">
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
    ///     Converts a character's interested level to a nameplate color.
    /// </summary>
    public sealed class NameplateColorConverter : GenderColorConverterBase
    {
        public NameplateColorConverter(IGetPermissions permissions, ICharacterManager characters)
            : base(permissions, characters)
        {
        }

        public NameplateColorConverter()
            : base(null, null)
        {
        }

        public override object Convert(object value, object parameter)
        {
            if (!(value is ICharacter))
                return Application.Current.FindResource("ForegroundBrush");

            var character = (ICharacter) value;
            return GetBrush(character);
        }
    }
}