#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeferredAction.cs">
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

namespace slimCat.Libraries
{
    #region Usings

    using System;
    using System.Threading;
    using System.Windows;

    #endregion

    /// <summary>
    ///     Represents a timer which performs an action on the UI thread when time elapses.  Rescheduling is supported.
    /// </summary>
    public class DeferredAction : IDisposable
    {
        private Timer timer;

        private DeferredAction(Action action)
        {
            timer = new Timer(delegate { Application.Current.Dispatcher.Invoke(action); });
        }

        /// <summary>
        ///     Creates a new DeferredAction.
        /// </summary>
        /// <param name="action">
        ///     The action that will be deferred.  It is not performed until after <see cref="Defer" /> is called.
        /// </param>
        public static DeferredAction Create(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return new DeferredAction(action);
        }

        /// <summary>
        ///     Defers performing the action until after time elapses.  Repeated calls will reschedule the action
        ///     if it has not already been performed.
        /// </summary>
        /// <param name="delay">
        ///     The amount of time to wait before performing the action.
        /// </param>
        public void Defer(TimeSpan delay)
        {
            // Fire action when time elapses (with no subsequent calls).
            timer.Change(delay, TimeSpan.FromMilliseconds(-1));
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (timer == null) return;
            timer.Dispose();
            timer = null;
        }

        #endregion
    }
}