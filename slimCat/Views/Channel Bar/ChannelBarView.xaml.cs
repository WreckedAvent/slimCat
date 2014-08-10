#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelBarView.xaml.cs">
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

    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for ChannelBarView.xaml
    /// </summary>
    public partial class ChannelbarView
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelbarView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public ChannelbarView(ChannelbarViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;

            vm.OnJumpToNotifications += (s, e) =>
            {
                if (NotificationButton.IsChecked == false)
                    NotificationButton.IsChecked = true;
            };
        }

        #endregion
    }
}