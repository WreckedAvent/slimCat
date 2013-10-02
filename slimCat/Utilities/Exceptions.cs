// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Exceptions.cs" company="Justin Kadrovach">
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
//   Defines the Exceptions type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Utilities
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using System.Windows.Threading;

    using Application = System.Windows.Application;
    using MessageBox = System.Windows.Forms.MessageBox;

    internal static class Exceptions
    {
        #region Constants

        /// <summary>
        ///     The defaul t_ message.
        /// </summary>
        private const string DefaultMessage = "Uh-oh! Something bad happened."
            + "\nPlease submit stacktrace.log found in your slimCat installation folder for debugging purposes.";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Writes the given exception to a log file in a uniform way.
        /// </summary>
        /// <param name="ex">
        /// Exception to be traced
        /// </param>
        /// <param name="message">
        /// The message.
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
                    file.WriteLine("Exception: {0}", ex.Message);
                    file.WriteLine("Occured at: {0}", ex.Source);
                    file.WriteLine();
                    file.Write("Immediate stack trace: {0}", ex.TargetSite);
                    file.WriteLine(ex.StackTrace);

                    if (ex.InnerException != null)
                    {
                        file.WriteLine("Inner Exception: {0}", ex.InnerException);
                    }

                    file.WriteLine();

                    file.WriteLine("====================================");
                    file.WriteLine("END EXCEPTION REPORT");
                    file.WriteLine("====================================");
                    file.Flush();

                    var dis = Application.Current.Dispatcher;

                    MessageBox.Show(message, "An error has occured!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (IOException)
            {
            }
        }

        /// <summary>
        /// The handle exception.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public static void HandleException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            HandleException(ex);
        }

        #endregion
    }
}