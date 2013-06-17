/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace lib
{
    public class TextBlockHelper
    {
        public static string GetInlineList(TextBlock element)
        {
            if (element != null)
                return element.GetValue(ArticleContentProperty) as string;
            return string.Empty;
        }

        public static void SetInlineList(TextBlock element, string value)
        {
            if (element != null)
                element.SetValue(ArticleContentProperty, value);
        }

        public static readonly DependencyProperty ArticleContentProperty =
            DependencyProperty.RegisterAttached(
                "InlineList",
                typeof(List<Inline>),
                typeof(TextBlockHelper),
                new PropertyMetadata(null, OnInlineListPropertyChanged));

        private static void OnInlineListPropertyChanged(DependencyObject obj,
            DependencyPropertyChangedEventArgs e)
        {
            var tb = obj as TextBlock;
            if (tb != null)
            {
                // clear previous inlines
                tb.Inlines.Clear();

                // add new inlines
                var inlines = e.NewValue as List<Inline>;
                if (inlines != null)
                {
                    inlines.ForEach(inl => tb.Inlines.Add((inl)));
                }
            }
        }
    }
}
