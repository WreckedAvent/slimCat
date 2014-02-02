#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelbarGlobalListView.xaml.cs">
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
    ///     The actual view for the tab on the right-hand side labeled 'global'
    /// </summary>
    public partial class GlobalTabView
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalTabView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public GlobalTabView(GlobalTabViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        #endregion
    }
}