using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ViewModels;

namespace Views
{
    /// <summary>
    /// Interaction logic for NotificationsView.xaml
    /// </summary>
    public partial class NotificationsView : Window
    {
        private ToastNotificationsViewModel _vm;

        public NotificationsView(ToastNotificationsViewModel vm)
        {
            InitializeComponent();

            _vm = vm;
            this.DataContext = _vm;
        }

        public void OnHideCommand()
        {
            Storyboard fadeOut = FindResource("FadeOutAnimation") as Storyboard;
            fadeOut = fadeOut.Clone();
            fadeOut.Completed += (s, e) => this.Hide();

            fadeOut.Begin(this);
        }

        public void OnShowCommand()
        {
            this.Show();
            Storyboard fadeIn = FindResource("FadeInAnimation") as Storyboard;
            fadeIn.Begin(this);
        }

        public void OnContentChanged()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

                this.Left = corner.X - this.ActualWidth;
                this.Top = corner.Y - this.ActualHeight;
            }));
        }
    }
}
