namespace Slimcat.Views
{
    using System;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;

    using ViewModels;

    using Point = System.Windows.Point;

    /// <summary>
    ///     Interaction logic for NotificationsView.xaml
    /// </summary>
    public partial class NotificationsView
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsView"/> class.
        /// </summary>
        /// <param name="vm">
        /// The vm.
        /// </param>
        public NotificationsView(ToastNotificationsViewModel vm)
        {
            this.InitializeComponent();
            this.DataContext = vm;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The on content changed.
        /// </summary>
        public void OnContentChanged()
        {
            this.Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle, 
                new Action(
                    () =>
                        {
                            var workingArea = Screen.PrimaryScreen.WorkingArea;
                            var presentationSource = PresentationSource.FromVisual(this);
                            if (presentationSource == null
                                || presentationSource.CompositionTarget == null)
                            {
                                return;
                            }

                            var transform = presentationSource.CompositionTarget.TransformFromDevice;
                            var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

                            this.Left = corner.X - this.ActualWidth;
                            this.Top = corner.Y - this.ActualHeight;
                        }));
        }

        /// <summary>
        ///     The on hide command.
        /// </summary>
        public void OnHideCommand()
        {
            var fadeOut = this.FindResource("FadeOutAnimation") as Storyboard;
            if (fadeOut == null)
            {
                this.Hide();
                return;
            }

            fadeOut = fadeOut.Clone();
            fadeOut.Completed += (s, e) => this.Hide();

            fadeOut.Begin(this);
        }

        /// <summary>
        ///     The on show command.
        /// </summary>
        public void OnShowCommand()
        {
            this.Show();
            var fadeIn = this.FindResource("FadeInAnimation") as Storyboard;

            if (fadeIn != null)
            {
                fadeIn.Begin(this);
            }
        }

        #endregion
    }
}