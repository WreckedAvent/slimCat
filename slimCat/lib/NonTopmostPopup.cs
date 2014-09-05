#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NonTopmostPopup.cs">
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
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Interop;

    #endregion

    public class NonTopmostPopup : Popup
    {
        protected override void OnOpened(EventArgs e)
        {
            var hwnd = ((HwndSource) PresentationSource.FromVisual(Child)).Handle;
            RECT rect;

            if (GetWindowRect(hwnd, out rect))
            {
                SetWindowPos(hwnd, -2, rect.Left, rect.Top, (int) Width, (int) Height, 0);
            }
        }

        #region P/Invoke imports & definitions

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32", EntryPoint = "SetWindowPos")]
        private static extern int SetWindowPos(IntPtr hWnd, int hwndInsertAfter, int x, int y, int cx, int cy,
            int wFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion
    }
}