#region Copyright

// <copyright file="GenderColorConverter.cs">
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

    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using Models;
    using Services;

    #endregion

    /// <summary>
    ///     Converts gender string into gender color.
    /// </summary>
    public sealed class GenderColorConverter : GenderColorConverterBase
    {
        public GenderColorConverter(IGetPermissions permissions, ICharacterManager characters)
            : base(permissions, characters)
        {
        }

        public GenderColorConverter()
            : base(null, null)
        {
        }

        public override object Convert(object value, object parameter)
        {
            if (!(value is ICharacter))
                return Application.Current.FindResource("ForegroundBrush");

            var character = (ICharacter) value;
            var gender = character.Gender;

            var brightColor = (Color) TryGet("Foreground", false);
            var baseColor = GetColor(character);

            var stops = new List<GradientStop>
            {
                new GradientStop(baseColor, 0.0),
                new GradientStop(baseColor, 0.5),
                new GradientStop(brightColor, 0.5),
                new GradientStop(brightColor, 1.0)
            };

            switch (gender)
            {
                case Gender.HermF:
                    return new LinearGradientBrush(new GradientStopCollection(stops), 0);
                case Gender.HermM:
                    return new LinearGradientBrush(new GradientStopCollection(stops));

                case Gender.Cuntboy:
                case Gender.Shemale:
                    return TryGet("Foreground", true);
                default:
                    return new SolidColorBrush(baseColor);
            }
        }
    }
}