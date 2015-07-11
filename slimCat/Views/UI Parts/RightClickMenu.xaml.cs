#region Copyright

// <copyright file="RightClickMenu.xaml.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
// 
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>

#endregion

using System.Windows;
using System.Windows.Controls;

namespace slimCat.Views
{
    /// <summary>
    ///     Interaction logic for RightClickMenu.xaml
    /// </summary>
    public partial class RightClickMenu
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="RightClickMenu" /> class.
        /// </summary>
        public RightClickMenu()
        {
            InitializeComponent();
        }

        #endregion

        private void VisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToTop();
            }
        }
    }
}