#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextBlockHelper.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Libraries
{
    #region Usings

    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;

    #endregion

    [ExcludeFromCodeCoverage]
    public class TextBlockHelper
    {
        #region Static Fields

        public static readonly DependencyProperty ArticleContentProperty =
            DependencyProperty.RegisterAttached(
                "InlineList",
                typeof (List<Inline>),
                typeof (TextBlockHelper),
                new PropertyMetadata(null, OnInlineListPropertyChanged));

        #endregion

        #region Public Methods and Operators

        public static List<Inline> GetInlineList(TextBlock element)
        {
            if (element != null)
                return element.GetValue(ArticleContentProperty) as List<Inline>;

            return null;
        }

        public static void SetInlineList(TextBlock element, List<Inline> value)
        {
            if (element != null)
                element.SetValue(ArticleContentProperty, value);
        }

        #endregion

        #region Methods

        private static void OnInlineListPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var tb = obj as TextBlock;
            if (tb == null)
                return;

            tb.Inlines.Clear();

            // add new inlines
            var inlines = e.NewValue as List<Inline>;
            if (inlines != null)
                inlines.ForEach(inl => tb.Inlines.Add(inl));
        }

        #endregion
    }

    /// <summary>
    ///     The span helper.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SpanHelper
    {
        #region Static Fields

        /// <summary>
        ///     The inline source property.
        /// </summary>
        public static readonly DependencyProperty InlineSourceProperty =
            DependencyProperty.RegisterAttached(
                "InlineSource",
                typeof (IEnumerable<Inline>),
                typeof (SpanHelper),
                new UIPropertyMetadata(null, OnInlineChanged));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The get inline source.
        /// </summary>
        /// <param name="obj">
        ///     The obj.
        /// </param>
        /// <returns>
        ///     The <see cref="IEnumerable{T}" />.
        /// </returns>
        public static IEnumerable<Inline> GetInlineSource(DependencyObject obj)
        {
            return (IEnumerable<Inline>) obj.GetValue(InlineSourceProperty);
        }

        /// <summary>
        ///     The set inline source.
        /// </summary>
        /// <param name="obj">
        ///     The obj.
        /// </param>
        /// <param name="value">
        ///     The value.
        /// </param>
        public static void SetInlineSource(DependencyObject obj, IEnumerable<Inline> value)
        {
            obj.SetValue(InlineSourceProperty, value);
        }

        #endregion

        #region Methods

        private static void OnInlineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var span = d as Span;
            var inlines = GetInlineSource(span);
            if (inlines == null || span == null)
                return;

            foreach (var inline in inlines)
                span.Inlines.Add(inline);
        }

        #endregion
    }
}