#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneralChannelView.xaml.cs">
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

    using System;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for GeneralChannelView.xaml
    /// </summary>
    public partial class GeneralChannelView
    {
        #region Fields

        private GeneralChannelViewModel vm;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GeneralChannelView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public GeneralChannelView(GeneralChannelViewModel vm)
        {
            try
            {
                InitializeComponent();
                this.vm = vm.ThrowIfNull("vm");

                DataContext = this.vm;
            }
            catch (Exception ex)
            {
                ex.Source = "General Channel View, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            vm = null;
            DataContext = null;
        }

        #endregion
    }
}