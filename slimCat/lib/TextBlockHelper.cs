// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextBlockHelper.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The text block helper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Libraries
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;

    /// <summary>
    ///     The text block helper.
    /// </summary>
    public class TextBlockHelper
    {
        #region Static Fields

        /// <summary>
        ///     The article content property.
        /// </summary>
        public static readonly DependencyProperty ArticleContentProperty =
            DependencyProperty.RegisterAttached(
                "InlineList", 
                typeof(List<Inline>), 
                typeof(TextBlockHelper), 
                new PropertyMetadata(null, OnInlineListPropertyChanged));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get inline list.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetInlineList(TextBlock element)
        {
            if (element != null)
            {
                return element.GetValue(ArticleContentProperty) as string;
            }

            return string.Empty;
        }

        /// <summary>
        /// The set inline list.
        /// </summary>
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void SetInlineList(TextBlock element, string value)
        {
            if (element != null)
            {
                element.SetValue(ArticleContentProperty, value);
            }
        }

        #endregion

        #region Methods

        private static void OnInlineListPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var tb = obj as TextBlock;
            if (tb == null)
            {
                return;
            }

            tb.Inlines.Clear();

            // add new inlines
            var inlines = e.NewValue as List<Inline>;
            if (inlines != null)
            {
                inlines.ForEach(inl => tb.Inlines.Add(inl));
            }
        }

        #endregion
    }

    /// <summary>
    ///     The span helper.
    /// </summary>
    public class SpanHelper
    {
        #region Static Fields

        /// <summary>
        ///     The inline source property.
        /// </summary>
        public static readonly DependencyProperty InlineSourceProperty =
            DependencyProperty.RegisterAttached(
                "InlineSource", 
                typeof(IEnumerable<Inline>), 
                typeof(SpanHelper), 
                new UIPropertyMetadata(null, OnInlineChanged));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get inline source.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{T}"/>.
        /// </returns>
        public static IEnumerable<Inline> GetInlineSource(DependencyObject obj)
        {
            return (IEnumerable<Inline>)obj.GetValue(InlineSourceProperty);
        }

        /// <summary>
        /// The set inline source.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <param name="value">
        /// The value.
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
            if (inlines == null)
            {
                return;
            }

            foreach (var inline in inlines)
            {
                span.Inlines.Add(inline);
            }
        }

        #endregion
    }
}