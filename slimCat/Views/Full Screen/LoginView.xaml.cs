#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginView.xaml.cs">
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

    using System;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="LoginView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public LoginView(LoginViewModel vm)
        {
            try
            {
                InitializeComponent();
                DataContext = vm;
            }
            catch (Exception ex)
            {
                ex.Source = "Login View, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion
    }
}