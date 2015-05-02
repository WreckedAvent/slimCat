#region Copyright

// <copyright file="Properties.cs">
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

namespace slimCat.Views
{
    #region Usings

    using System.Windows;

    #endregion

    internal class Properties
    {
        public static readonly DependencyProperty NeedsAttentionProperty = DependencyProperty.RegisterAttached(
            "NeedsAttention", typeof (bool), typeof (Properties), new PropertyMetadata(false));

        public static void SetNeedsAttention(DependencyObject element, bool value)
        {
            element.SetValue(NeedsAttentionProperty, value);
        }

        public static bool GetNeedsAttention(DependencyObject element)
        {
            return (bool) element.GetValue(NeedsAttentionProperty);
        }
    }
}