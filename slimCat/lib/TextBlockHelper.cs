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
