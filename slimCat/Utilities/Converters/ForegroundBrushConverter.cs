#region Copyright

// <copyright file="ForegroundBrushConverter.cs">
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
    using System.Windows.Media;
    using Models;

    #endregion

    public sealed class ForegroundBrushConverter : OneWayConverter
    {
        private readonly IChatModel cm;
        private readonly IThemeLocator locator;

        public ForegroundBrushConverter(IChatModel cm, IThemeLocator locator)
        {
            this.cm = cm;
            this.locator = locator;
        }

        public ForegroundBrushConverter()
        {
        }

        public override object Convert(object value, object parameter)
        {
            Brush defaultBrush;
            if (locator != null) defaultBrush = locator.Find<Brush>("ForegroundBrush");
            else defaultBrush = (Brush) Application.Current.FindResource("ForegroundBrush");

            if (value == null || cm == null) return defaultBrush;

            var message = value as IMessage; // this is the beef of the message

            if (message == null) return defaultBrush;

            if (cm.CurrentCharacter.NameEquals(message.Poster.Name) && locator != null)
                return locator.Find<Brush>("SelfBrush");

            return defaultBrush;
        }
    }
}