#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SysProp.cs">
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

namespace slimCat.ViewModels
{
    #region Usings

    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Threading;

    #endregion

    /// <summary>
    ///     This is a master class which provides access to the Dispatcher and NotifyPropertyChanged
    /// </summary>
    public abstract class SysProp : DispatcherObject, IDisposable, INotifyPropertyChanged
    {
        #region Public Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Methods and Operators

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Methods

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var e = new PropertyChangedEventArgs(propertyName);
            PropertyChanged?.Invoke(this, e);
        }


        protected virtual void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            PropertyChanged = null;
        }

        #endregion
    }
}