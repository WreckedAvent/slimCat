#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PMChannelView.xaml.cs">
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

namespace Slimcat.Views
{
    #region Usings

    using System;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for PmChannelView.xaml
    /// </summary>
    public partial class PmChannelView
    {
        #region Fields

        private PmChannelViewModel vm;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PmChannelView" /> class.
        /// </summary>
        /// <param name="vm">
        ///     The vm.
        /// </param>
        public PmChannelView(PmChannelViewModel vm)
        {
            try
            {
                InitializeComponent();
                this.vm = vm.ThrowIfNull("vm");

                DataContext = this.vm;

                this.vm.StatusChanged += OnStatusChanged;
            }
            catch (Exception ex)
            {
                ex.Source = "PmChannel View, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            vm.StatusChanged -= OnStatusChanged;
            DataContext = null;
            vm = null;
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(
                (Action) delegate
                    {
                        if (!CharacterStatusDisplayer.IsExpanded)
                            CharacterStatusDisplayer.IsExpanded = true;
                    });
        }

        #endregion
    }
}