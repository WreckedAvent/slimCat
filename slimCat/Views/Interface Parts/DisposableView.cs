using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Views
{
    /// <summary>
    /// Declares a view which needs to be disposed
    /// </summary>
    public abstract class DisposableView : UserControl, IDisposable
    {
        public abstract void Dispose();
        internal abstract void Dispose(bool IsManaged);
    }
}
