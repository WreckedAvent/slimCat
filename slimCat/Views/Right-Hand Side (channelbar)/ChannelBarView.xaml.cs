// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelBarView.xaml.cs" company="Justin Kadrovach">
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
//   Interaction logic for ChannelBarView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Views
{
    using System.Windows.Controls;

    using ViewModels;

    /// <summary>
    ///     Interaction logic for ChannelBarView.xaml
    /// </summary>
    public partial class ChannelbarView : UserControl
    {
        #region Fields

        private readonly ChannelbarViewModel _vm;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelbarView"/> class.
        /// </summary>
        /// <param name="vm">
        /// The vm.
        /// </param>
        public ChannelbarView(ChannelbarViewModel vm)
        {
            this.InitializeComponent();
            this._vm = vm;

            this.DataContext = this._vm;

            this._vm.OnJumpToNotifications += (s, e) =>
                {
                    if (this.NotificationButton.IsChecked == false)
                    {
                        this.NotificationButton.IsChecked = true;
                    }
                };
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the vm.
        /// </summary>
        public ChannelbarViewModel VM
        {
            get
            {
                return this._vm;
            }
        }

        #endregion
    }
}