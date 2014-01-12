#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DisposableView.cs">
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
    using System.Windows.Controls;

    #endregion

    /// <summary>
    ///     Declares a view which needs to be disposed
    /// </summary>
    public abstract class DisposableView : UserControl, IDisposable
    {
        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Methods

        protected abstract void Dispose(bool isManaged);

        #endregion
    }
}