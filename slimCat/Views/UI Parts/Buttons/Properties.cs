namespace slimCat.Views
{
    using System.Windows;

    class Properties
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
