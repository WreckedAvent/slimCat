#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FullScreenHelper.cs">
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

namespace slimCat.lib
{
    #region Usings

    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Diagnostics.CodeAnalysis;

    #endregion

    [ExcludeFromCodeCoverage]
    public class FullScreenHelper
    {
        private static IntPtr desktopHandle;
        private static IntPtr shellHandle;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowRect(IntPtr hwnd, out Rect rc);

        public static bool ForegroundIsFullScreen()
        {
            desktopHandle = GetDesktopWindow();
            shellHandle = GetShellWindow();

            var runningFullScreen = false;
            Rect appBounds;

            var hWnd = GetForegroundWindow();
            if (hWnd.Equals(IntPtr.Zero)) return false;
            if (hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)) return false;

            GetWindowRect(hWnd, out appBounds);
            var screenBounds = Screen.FromHandle(hWnd).Bounds;

            if ((appBounds.Bottom - appBounds.Top) >= screenBounds.Height
                && (appBounds.Right - appBounds.Left) >= screenBounds.Width)
                runningFullScreen = true;

            return runningFullScreen;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}