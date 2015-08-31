#region Copyright

// <copyright file="Program.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
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

#endregion


namespace Bootstrapper
{
    using System.Runtime.InteropServices;

    // found at http://stackoverflow.com/questions/6374673/unblock-file-from-within-net-4-c-sharp
    // other useful links: 
    // http://mikehadlow.blogspot.com/2011/07/detecting-and-changing-files-internet.html
    // https://msdn.microsoft.com/en-us/library/ms537029(v=vs.85).aspx   
     
    /// <summary>
    /// Used to unblock the extra zone information windows adds with an internet download.
    /// </summary>
    public static class FileUnblocker
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string name);

        public static bool Unblock(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }
    }
}
