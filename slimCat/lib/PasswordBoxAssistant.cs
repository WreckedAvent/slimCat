#region Copyright

// <copyright file="PasswordBoxAssistant.cs">
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

namespace slimCat.Libraries
{
    #region Usings

    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    #endregion

    /// <summary>
    ///     3rd party support to allow databinding for password boxes (because they don't natively)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class PasswordBoxAssistant
    {
        #region Static Fields

        /// <summary>
        ///     The bind password.
        /// </summary>
        public static readonly DependencyProperty BindPassword = DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof (bool),
            typeof (PasswordBoxAssistant),
            new PropertyMetadata(false, OnBindPasswordChanged));

        /// <summary>
        ///     The bound password.
        /// </summary>
        public static readonly DependencyProperty BoundPassword = DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof (string),
            typeof (PasswordBoxAssistant),
            new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        private static readonly DependencyProperty UpdatingPassword =
            DependencyProperty.RegisterAttached(
                "UpdatingPassword", typeof (bool), typeof (PasswordBoxAssistant), new PropertyMetadata(false));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The get bind password.
        /// </summary>
        /// <param name="dp">
        ///     The dp.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public static bool GetBindPassword(DependencyObject dp)
        {
            return (bool) dp.GetValue(BindPassword);
        }

        /// <summary>
        ///     The get bound password.
        /// </summary>
        /// <param name="dp">
        ///     The dp.
        /// </param>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public static string GetBoundPassword(DependencyObject dp)
        {
            return (string) dp.GetValue(BoundPassword);
        }

        /// <summary>
        ///     The set bind password.
        /// </summary>
        /// <param name="dp">
        ///     The dp.
        /// </param>
        /// <param name="value">
        ///     The value.
        /// </param>
        public static void SetBindPassword(DependencyObject dp, bool value)
        {
            dp.SetValue(BindPassword, value);
        }

        /// <summary>
        ///     The set bound password.
        /// </summary>
        /// <param name="dp">
        ///     The dp.
        /// </param>
        /// <param name="value">
        ///     The value.
        /// </param>
        public static void SetBoundPassword(DependencyObject dp, string value)
        {
            dp.SetValue(BoundPassword, value);
        }

        #endregion

        #region Methods

        private static bool GetUpdatingPassword(DependencyObject dp)
        {
            return (bool) dp.GetValue(UpdatingPassword);
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            var box = sender as PasswordBox;
            if (box == null)
                return;

            // set a flag to indicate that we're updating the password
            SetUpdatingPassword(box, true);

            // push the new password into the BoundPassword property
            SetBoundPassword(box, box.Password);
            SetUpdatingPassword(box, false);
        }

        private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            // when the BindPassword attached property is set on a PasswordBox,
            // start listening to its PasswordChanged event
            var box = dp as PasswordBox;

            if (box == null)
                return;

            var wasBound = (bool) e.OldValue;
            var needToBind = (bool) e.NewValue;

            if (wasBound)
                box.PasswordChanged -= HandlePasswordChanged;

            if (needToBind)
                box.PasswordChanged += HandlePasswordChanged;
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as PasswordBox;

            // only handle this event when the property is attached to a PasswordBox
            // and when the BindPassword attached property has been set to true
            if (d == null || !GetBindPassword(d) || box == null)
                return;

            // avoid recursive updating by ignoring the box's changed event
            box.PasswordChanged -= HandlePasswordChanged;

            var newPassword = (string) e.NewValue;

            if (!GetUpdatingPassword(box))
                box.Password = newPassword;

            box.PasswordChanged += HandlePasswordChanged;
        }

        private static void SetUpdatingPassword(DependencyObject dp, bool value)
        {
            dp.SetValue(UpdatingPassword, value);
        }

        #endregion
    }
}