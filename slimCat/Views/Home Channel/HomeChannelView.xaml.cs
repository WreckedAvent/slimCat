#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HomeChannelView.xaml.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Views
{
    #region Usings

    using System.Windows;
    using System.Windows.Input;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for HomeChannelView.xaml
    /// </summary>
    public partial class HomeChannelView
    {
        #region Fields

        private ViewModelBase vm;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="UtilityChannelView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public HomeChannelView(HomeChannelViewModel vm)
        {
            InitializeComponent();
            this.vm = vm;

            DataContext = this.vm;
        }

        #endregion

        #region Methods

        override protected void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            DataContext = null;
            vm = null;
        }

        private void OnEntryBoxResizeRequested(object sender, MouseButtonEventArgs e)
        {
            EntryBoxRowDefinition.Height = new GridLength();
        }

        #endregion

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            StaticFunctions.TryOpenRightClickMenuCommand<DisposableView>(sender, 1);
        }
    }
}