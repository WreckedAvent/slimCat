// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SnapToBottomManager.cs" company="Justin Kadrovach">
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
//   The snap to bottom manager.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace System
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    ///     The snap to bottom manager.
    /// </summary>
    public class SnapToBottomManager
    {
        #region Fields

        private readonly DependencyObject _messages;

        private ScrollViewer _toManage;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapToBottomManager"/> class.
        /// </summary>
        /// <param name="messages">
        /// The messages.
        /// </param>
        public SnapToBottomManager(DependencyObject messages)
        {
            this._messages = messages;
            this._toManage = messages as ScrollViewer;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The auto down scroll.
        /// </summary>
        /// <param name="keepAtCurrent">
        /// The keep at current.
        /// </param>
        /// <param name="forceDown">
        /// The force down.
        /// </param>
        public void AutoDownScroll(bool keepAtCurrent, bool forceDown = false)
        {
            if (this._toManage == null)
            {
                this._toManage = this._messages as ScrollViewer;
            }

            if (this._toManage != null)
            {
                if (forceDown)
                {
                    this._toManage.ScrollToBottom();
                }
                else if (this.ShouldAutoScroll())
                {
                    this._toManage.ScrollToBottom();
                }
            }
        }

        #endregion

        #region Methods

        public static ScrollViewer FindChild(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is ScrollViewer)
                {
                    return child as ScrollViewer;
                }

                DependencyObject grandchild = FindChild(child);

                if (grandchild != null)
                {
                    return grandchild as ScrollViewer;
                }
            }

            return default(ScrollViewer);
        }

        private bool ShouldAutoScroll()
        {
            return this._toManage.ScrollableHeight - this._toManage.VerticalOffset <= 25;
        }

        #endregion
    }
}