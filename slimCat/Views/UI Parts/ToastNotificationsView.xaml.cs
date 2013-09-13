// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ToastNotificationsView.xaml.cs" company="Justin Kadrovach">
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
//   Interaction logic for NotificationsView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Views
{
    using System;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;

    using ViewModels;

    using Point = System.Windows.Point;

    /// <summary>
    ///     Interaction logic for NotificationsView.xaml
    /// </summary>
    public partial class NotificationsView : Window
    {
        #region Fields

        private readonly ToastNotificationsViewModel _vm;

        #endregion

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

            this._vm = vm;
            this.DataContext = this._vm;
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
                            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                            Matrix transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                            Point corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom));

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
            fadeIn.Begin(this);
        }

        #endregion
    }
}