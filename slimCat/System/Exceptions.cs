/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace System
{
    class Exceptions
    {
        public const string DEFAULT_MESSAGE =
            "Oops! Looks like the application done goofed itself."
            + "Please submit the Stacktrace.log file for inspection."
            + "\n\nApplication will now exit.";

        /// <summary>
        /// Writes the given exception to a log file in a uniform way.
        /// </summary>
        /// <param name="ex">Exception to be traced</param>
        static public void HandleException(Exception ex, string message = DEFAULT_MESSAGE)
        {
            using (StreamWriter file = new StreamWriter(@"Stacktrace.log", true))
            {
                file.WriteLine();
                file.WriteLine("====================================");
                file.WriteLine("BEGIN EXCEPTION REPORT");
                file.WriteLine(System.DateTime.UtcNow);
                file.WriteLine("====================================");
                file.WriteLine();
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

                Dispatcher dis = Application.Current.Dispatcher;

                Windows.Forms.MessageBox.Show(message, "An error has occured!",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    Windows.Forms.MessageBoxIcon.Error);

                dis.BeginInvoke((Action)delegate()
                {
                    Application.Current.Shutdown();
                });
            }
        }

        static public void HandleException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            HandleException(ex);
        }
    }
}
