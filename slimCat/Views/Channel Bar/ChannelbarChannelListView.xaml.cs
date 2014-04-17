#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelbarChannelListView.xaml.cs">
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

namespace slimCat.Views
{
    #region Usings

    using System.Windows;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for ChannelListView.xaml
    /// </summary>
    public partial class ChannelTabView
    {
        private readonly ChannelsTabViewModel vm;

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelTabView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public ChannelTabView(ChannelsTabViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            this.vm = vm;
        }

        #endregion

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            vm.UpdateChannels();
        }
    }
}