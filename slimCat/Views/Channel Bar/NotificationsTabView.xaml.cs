#region Copyright

// <copyright file="NotificationsTabView.xaml.cs">
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

namespace slimCat.Views
{
    #region Usings

    using System.Windows;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for ChannelbarNotificationsTabView.xaml
    /// </summary>
    public partial class NotificationsTabView
    {
        private readonly NotificationsTabViewModel vm;

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="NotificationsTabView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public NotificationsTabView(NotificationsTabViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            this.vm = vm;
        }

        #endregion

        private void OnUnload(object sender, RoutedEventArgs e)
        {
            PopupAnchor.MessageSource = null;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            PopupAnchor.MessageSource = vm.CurrentNotifications;
        }
    }
}