namespace slimCat.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Brush = System.Drawing.Brush;

    public class ImageTextBox : TextBox
    {
        static ImageTextBox()
        {

            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ImageTextBox),
                new FrameworkPropertyMetadata(typeof(ImageTextBox)));
        }

        public static DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                "LabelText",
                typeof (string),
                typeof (ImageTextBox));

        public static DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                "IconSource",
                typeof(ImageSource),
                typeof(ImageTextBox));

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


        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            HasText = Text.Length != 0;
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
            set { SetValue(IconSourceProperty, value);}
        }
    }
}
