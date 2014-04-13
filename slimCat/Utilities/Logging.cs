#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Logging.cs">
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

namespace slimCat.Utilities
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using SimpleJson;

    #endregion

    public static class Logging
    {
        #region Fields
        private const string LargeSeparator = "===================================";
        private static readonly TraceSwitch DefaultSwitch = new TraceSwitch("Default", string.Empty);
        private static readonly object Locker = new object();
        #endregion

        #region Methods
        /// <summary>
        ///     Logs the header with the given text.
        /// </summary>
        /// <param name="text">The text of the header.</param>
        [Conditional("DEBUG")]
        public static void LogHeader(string text)
        {
            lock (Locker)
            {
                Trace.WriteLine(LargeSeparator);
                Trace.WriteLine(text);
                Trace.WriteLine(LargeSeparator);
                Trace.Flush();
            }
        }

        /// <summary>
        ///     Logs the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="group">The group.</param>
        /// <param name="isVerbose">if set to <c>true</c> the log is treated as unnecessary verbose logging.</param>
        [Conditional("DEBUG")]
        public static void Log(string text = null, string group = null, bool isVerbose = false)
        {
            if (isVerbose && !DefaultSwitch.TraceVerbose) return;

            lock (Locker)
            {
                if (group != null) group = GetTimestamp() + " " + group;

                Trace.WriteLine(text, group);
                Trace.Flush();
            }
        }

        /// <summary>
        ///     Logs the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="group">The group.</param>
        /// <param name="isVerbose">if set to <c>true</c> the log is treated as unnecessary verbose logging.</param>
        [Conditional("DEBUG")]
        public static void LogLine(string text = null, string group = null, bool isVerbose = false)
        {
            Log(text, group, isVerbose);
            Log();
        }

        /// <summary>
        ///     Logs the object. Arrays and dictionaries are logged with their contents.
        /// </summary>
        /// <param name="obj">The object to log.</param>
        /// <param name="useComma">if set to <c>true</c>, commas are added at the end of literals.</param>
        /// <param name="isVerbose">if set to <c>true</c> the log is treated as unnecessary verbose logging.</param>
        [Conditional("DEBUG")]
        public static void LogObject(object obj, bool useComma = false, bool isVerbose = false)
        {
            lock (Locker)
            {
                if (obj == null) return;

                var dict = obj as IDictionary<string, object>;
                if (dict != null)
                {
                    if (dict.Keys.Count == 0)
                    {
                        Trace.WriteLine(useComma ? "{}," : "{}");
                        return;
                    }

                    if (dict.Keys.Count(x => x != Constants.Arguments.Command) == 1 && dict.Values.First() is string)
                    {
                        var temp = "{" + (" {0}: \"{1}\" ".FormatWith(dict.Keys.First(), dict.Values.First())) + "}";
                        Trace.WriteLine(temp + (useComma ? "," : ""));
                        return;
                    }

                    Trace.WriteLine("{");
                    Trace.Indent();

                    foreach (var pair in dict.Where(pair => pair.Key != Constants.Arguments.Command).Take(10))
                    {
                        Trace.Write("{0}: ".FormatWith(pair.Key));
                        LogObject(pair.Value, true);
                    }

                    if (dict.Keys.Count > 10)
                        Trace.WriteLine("... ");

                    Trace.Unindent();

                    Trace.WriteLine(useComma ? "}," : "}");
                    Trace.Flush();
                    return;
                }

                var arr = obj as JsonArray;
                if (arr != null)
                {
                    if (arr.Count == 0)
                    {
                        Trace.WriteLine(useComma ? "[]," : "[]");
                        return;
                    }

                    if (arr.Count == 1)
                    {
                        Trace.WriteLine("[ {0} ]".FormatWith(arr[0]) + (useComma ? "," : ""));
                        return;
                    }

                    Trace.WriteLine("[");
                    Trace.Indent();

                    foreach (var o in arr.Take(10))
                        LogObject(o, true);

                    if (arr.Count > 10)
                        Trace.WriteLine(" ... ");

                    Trace.Unindent();
                    Trace.WriteLine(useComma ? "]," : "]");
                    Trace.Flush();
                    return;
                }

                var isString = obj is string;
                var toWrite = isString ? "\"" + obj + "\"" : obj;
                Trace.WriteLine(toWrite + (useComma ? "," : ""));
                Trace.Flush();
            }
        }

        public static string GetTimestamp()
        {
            return "[{0}]".FormatWith(DateTime.Now.ToString("mm:ss.ff"));
        }
        #endregion
    }
}