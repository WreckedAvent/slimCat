#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FlashWindowHelper.cs">
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

namespace Slimcat.Libraries
{
    #region Usings

    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;

    #endregion

    /// <summary>
    ///     The native methods.
    /// </summary>
    public static class NativeMethods
    {
        #region Window Flashing API Stuff

        private const uint FlashwStop = 0; // Stop flashing. The system restores the window to its original state.

        private const uint FlashwCaption = 1; // Flash the window caption.

        private const uint FlashwTray = 2; // Flash the taskbar button.

        private const uint FlashwAll = 3; // Flash both the window caption and taskbar button.

        private const uint FlashwTimer = 4; // Flash continuously, until the FLASHW_STOP flag is set.

        private const uint FlashwTimernofg = 12; // Flash continuously until the window comes to the foreground.

        #endregion

        /// <summary>
        ///     The flash window.
        /// </summary>
        /// <param name="win">
        ///     The win.
        /// </param>
        /// <param name="count">
        ///     The count.
        /// </param>
        public static void FlashWindow(this Window win, uint count = 5)
        {
            // Don't flash if the window is active
            if (win.IsActive)
                return;

            var h = new WindowInteropHelper(win);

            var info = new Flashwinfo
                {
                    hwnd = h.Handle,
                    dwFlags = FlashwAll | FlashwTimernofg,
                    uCount = count,
                    dwTimeout = 0
                };

            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
            FlashWindowEx(ref info);
        }

        /// <summary>
        ///     The stop flashing window.
        /// </summary>
        /// <param name="win">
        ///     The win.
        /// </param>
        public static void StopFlashingWindow(this Window win)
        {
            var h = new WindowInteropHelper(win);

            var info = new Flashwinfo {hwnd = h.Handle};
            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
            info.dwFlags = FlashwStop;
            info.uCount = uint.MaxValue;
            info.dwTimeout = 0;

            FlashWindowEx(ref info);
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref Flashwinfo pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct Flashwinfo
        {
            /// <summary>
            ///     The cb size.
            /// </summary>
            public uint cbSize; // The size of the structure in bytes.

            /// <summary>
            ///     The hwnd.
            /// </summary>
            public IntPtr hwnd; // A Handle to the Window to be Flashed. The window can be either opened or minimized.

            /// <summary>
            ///     The dw flags.
            /// </summary>
            public uint dwFlags; // The Flash Status.

            /// <summary>
            ///     The u count.
            /// </summary>
            public uint uCount; // number of times to flash the window

            /// <summary>
            ///     The dw timeout.
            /// </summary>
            public uint dwTimeout;

            // The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
        }
    }
}