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
    }
}
