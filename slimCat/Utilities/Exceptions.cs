#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Exceptions.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    // ReSharper disable once RedundantUsingDirective
    using System;
    using System.IO;
    using System.Windows.Forms;
    using System.Windows.Threading;

    #endregion

    internal static class Exceptions
    {
        #region Constants

        /// <summary>
        ///     The default message.
        /// </summary>
        private const string DefaultMessage = "Uh-oh! Something bad happened." +
                                              "\n\nPlease submit the stacktrace.log file found in your slimCat installation folder to slimCat on F-list for debugging purposes.";

        private const string DefaultDebugMessage = "Uh-oh! Something bad happened." +
                                                   "\n\nPlease submit the trace.log file found in your slimCat installation folder to slimCat on F-list for debugging purposes.";

        #endregion

        #region Public Methods and Operators

#if DEBUG
    /// <summary>
    ///     Writes the given exception to a log file in a uniform way.
    /// </summary>
    /// <param name="ex">
    ///     Exception to be traced
    /// </param>
    /// <param name="message">
    ///     The message.
    /// </param>
        public static void HandleException(Exception ex, string message = DefaultDebugMessage)
        {
            Logging.LogHeader("Exception at " + DateTime.UtcNow);

            Logging.LogLine("Version: " + Constants.FriendlyName);
            Logging.Log(ex.ToString());

            Logging.LogHeader("End exception");

            ShowErrorBox(message, "An error has occured!");
        }
#endif

#if !DEBUG
        /// <summary>
        ///     Writes the given exception to a log file in a uniform way.
        /// </summary>
        /// <param name="ex">
        ///     Exception to be traced
        /// </param>
        /// <param name="message">
        ///     The message.
        /// </param>
        public static void HandleException(Exception ex, string message = DefaultMessage)
        {
            try
            {
                using (var file = new StreamWriter(@"Stacktrace.log", true))
                {
                    file.WriteLine();
                    file.WriteLine("====================================");
                    file.WriteLine("BEGIN EXCEPTION REPORT");
                    file.WriteLine(DateTime.UtcNow);
                    file.WriteLine("====================================");
                    file.WriteLine();
                    file.WriteLine("Version: {0}", Constants.FriendlyName);
                    file.WriteLine("Exception: {0}", ex.Message);
                    file.WriteLine("Occured at: {0}", ex.Source);
                    file.WriteLine();
                    file.Write("Immediate stack trace: {0}", ex.TargetSite);
                    file.WriteLine(ex.StackTrace);

                    if (ex.InnerException != null)
                        file.WriteLine("Inner Exception: {0}", ex.InnerException);

                    file.WriteLine();

                    file.WriteLine("====================================");
                    file.WriteLine("END EXCEPTION REPORT");
                    file.WriteLine("====================================");
                    file.Flush();

                    ShowErrorBox(message, "An error has occured!");
                }
            }
            catch (IOException)
            {
            }
        }
#endif

        public static void ShowErrorBox(string message, string title)
        {
            const int topMostOption = 0x40000;
            const int getsForegroundOption = 0x010000;
            const MessageBoxOptions options = (MessageBoxOptions) (topMostOption | getsForegroundOption);

            MessageBox.Show(
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1,
                options);
        }

        /// <summary>
        ///     Handles an exception by logging it entirely and displaying an error modal.
        /// </summary>
        public static void HandleException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            HandleException(ex);
        }

        #endregion
    }
}