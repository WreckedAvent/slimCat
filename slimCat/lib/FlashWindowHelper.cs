// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FlashWindowHelper.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The native methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Libraries
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;

    /// <summary>
    ///     The native methods.
    /// </summary>
    public static class NativeMethods
    {
        #region Window Flashing API Stuff

        private const uint FLASHW_STOP = 0; // Stop flashing. The system restores the window to its original state.

        private const uint FLASHW_CAPTION = 1; // Flash the window caption.

        private const uint FLASHW_TRAY = 2; // Flash the taskbar button.

        private const uint FLASHW_ALL = 3; // Flash both the window caption and taskbar button.

        private const uint FLASHW_TIMER = 4; // Flash continuously, until the FLASHW_STOP flag is set.

        private const uint FLASHW_TIMERNOFG = 12; // Flash continuously until the window comes to the foreground.

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        #endregion

        /// <summary>
        /// The flash window.
        /// </summary>
        /// <param name="win">
        /// The win.
        /// </param>
        /// <param name="count">
        /// The count.
        /// </param>
        public static void FlashWindow(this Window win, uint count = 5)
        {
            // Don't flash if the window is active
            if (win.IsActive)
            {
                return;
            }

            var h = new WindowInteropHelper(win);

            var info = new FLASHWINFO
                           {
                               hwnd = h.Handle, 
                               dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG, 
                               uCount = count, 
                               dwTimeout = 0
                           };

            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
            FlashWindowEx(ref info);
        }

        /// <summary>
        /// The stop flashing window.
        /// </summary>
        /// <param name="win">
        /// The win.
        /// </param>
        public static void StopFlashingWindow(this Window win)
        {
            var h = new WindowInteropHelper(win);

            var info = new FLASHWINFO();
            info.hwnd = h.Handle;
            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
            info.dwFlags = FLASHW_STOP;
            info.uCount = uint.MaxValue;
            info.dwTimeout = 0;

            FlashWindowEx(ref info);
        }
    }
}