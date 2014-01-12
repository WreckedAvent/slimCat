#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ToastNotificationsView.xaml.cs">
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

namespace Slimcat.Views
{
    #region Usings

    using System;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for NotificationsView.xaml
    /// </summary>
    public partial class NotificationsView
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="NotificationsView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public NotificationsView(ToastNotificationsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The on content changed.
        /// </summary>
        public void OnContentChanged()
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(
                    () =>
                        {
                            var workingArea = Screen.PrimaryScreen.WorkingArea;
                            var presentationSource = PresentationSource.FromVisual(this);
                            if (presentationSource == null
                                || presentationSource.CompositionTarget == null)
                                return;

                            var transform = presentationSource.CompositionTarget.TransformFromDevice;
                            var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

                            Left = corner.X - ActualWidth;
                            Top = corner.Y - ActualHeight;
                        }));
        }

        /// <summary>
        ///     The on hide command.
        /// </summary>
        public void OnHideCommand()
        {
            var fadeOut = FindResource("FadeOutAnimation") as Storyboard;
            if (fadeOut == null)
            {
                Hide();
                return;
            }

            fadeOut = fadeOut.Clone();
            fadeOut.Completed += (s, e) => Hide();

            fadeOut.Begin(this);
        }

        /// <summary>
        ///     The on show command.
        /// </summary>
        public void OnShowCommand()
        {
            Show();
            var fadeIn = FindResource("FadeInAnimation") as Storyboard;

            if (fadeIn != null)
                fadeIn.Begin(this);
        }

        #endregion
    }
}