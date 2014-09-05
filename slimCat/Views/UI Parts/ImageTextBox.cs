#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageTextBox.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Views
{
    #region Usings

    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Brush = System.Drawing.Brush;

    #endregion

    public class ImageTextBox : TextBox
    {
        public static DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                "LabelText",
                typeof (string),
                typeof (ImageTextBox));

        public static DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                "IconSource",
                typeof (ImageSource),
                typeof (ImageTextBox));

        public static DependencyProperty LabelTextColorProperty =
            DependencyProperty.Register(
                "LabelTextColor",
                typeof (Brush),
                typeof (ImageTextBox));

        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "HasText",
                typeof (bool),
                typeof (ImageTextBox),
                new PropertyMetadata());

        public static DependencyProperty HasTextProperty = HasTextPropertyKey.DependencyProperty;

        static ImageTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof (ImageTextBox),
                new FrameworkPropertyMetadata(typeof (ImageTextBox)));
        }


        public string LabelText
        {
            get { return (string) GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public Brush LabelTextColor
        {
            get { return (Brush) GetValue(LabelTextColorProperty); }
            set { SetValue(LabelTextColorProperty, value); }
        }

        public bool HasText
        {
            get { return (bool) GetValue(HasTextProperty); }
            private set { SetValue(HasTextPropertyKey, value); }
        }

        public ImageSource IconSource
        {
            get { return (ImageSource) GetValue(IconSourceProperty); }
            set { SetValue(IconSourceProperty, value); }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            HasText = Text.Length != 0;
        }
    }
}