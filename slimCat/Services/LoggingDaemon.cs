#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingDaemon.cs">
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

namespace Slimcat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Web;
    using Models;
    using Utilities;

    #endregion

    /// <summary>
    ///     The special log message kind.
    /// </summary>
    public enum SpecialLogMessageKind
    {
        /// <summary>
        ///     The header.
        /// </summary>
        Header,

        /// <summary>
        ///     The section.
        /// </summary>
        Section,

        /// <summary>
        ///     The line break.
        /// </summary>
        LineBreak
    }

    /// <summary>
    ///     The logging daemon.
    /// </summary>
    public class LoggingDaemon : ILogger
    {
        #region Fields

        private readonly string currentCharacter;
        private readonly string fullPath;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="LoggingDaemon" /> class.
        ///     Creates a new logging daemon for a given account name
        /// </summary>
        /// <param name="characterName">
        ///     The character Name.
        /// </param>
        public LoggingDaemon(string characterName)
        {
            currentCharacter = characterName;

            fullPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "slimCat", currentCharacter);

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The get logs.
        /// </summary>
        /// <param name="title">
        ///     The title.
        /// </param>
        /// <param name="id">
        ///     The id.
        /// </param>
        /// <returns>
        ///     The <see cref="IEnumerable{T}" />.
        /// </returns>
        public IEnumerable<string> GetLogs(string title, string id)
        {
            var loggingPath = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);
            IEnumerable<string> toReturn = new List<string>();
            var fileName = DateToFileName();

            if (!Directory.Exists(loggingPath))
                return new List<string>();

            var toGet = Path.Combine(loggingPath, fileName);

            if (File.Exists(toGet))
            {
                try
                {
                    var lines = File.ReadLines(Path.Combine(loggingPath, fileName));
                    var enumerable = lines as IList<string> ?? lines.ToList();

                    var toSkip = Math.Max(enumerable.Count() - 10, 0);

                    toReturn = enumerable.Skip(toSkip).ToList();
                }
                catch
                {
                    // file operations run the risk of exceptions
                    return new List<string>();
                }
            }

            return toReturn;
        }

        /// <summary>
        ///     The log message.
        /// </summary>
        /// <param name="title">
        ///     The title.
        /// </param>
        /// <param name="id">
        ///     The id.
        /// </param>
        /// <param name="message">
        ///     The message.
        /// </param>
        public void LogMessage(string title, string id, IMessage message)
        {
            using (var writer = AccessLog(title, id))
            {
                var thisMessage = HttpUtility.HtmlDecode(message.Message);
                var timestamp = message.TimeStamp;

                switch (message.Type)
                {
                    case MessageType.Normal:
                        if (!message.Message.StartsWith("/me"))
                            writer.WriteLine(timestamp + ' ' + message.Poster.Name + ": " + thisMessage);
                        else
                            writer.WriteLine(timestamp + ' ' + message.Poster.Name + thisMessage.Substring(3));

                        break;
                    case MessageType.Roll:
                        writer.WriteLine(timestamp + ' ' + thisMessage);
                        break;
                    default:
                        if (!message.Message.StartsWith("/me"))
                            writer.WriteLine("Ad at " + timestamp + ": " + thisMessage + " ~By " + message.Poster.Name);
                        else
                        {
                            writer.WriteLine(
                                "Ad at " + timestamp + ": " + message.Poster.Name + " " + thisMessage.Substring(3));
                        }

                        break;
                }
            }
        }

        /// <summary>
        ///     The open log.
        /// </summary>
        /// <param name="isFolder">
        ///     The is folder.
        /// </param>
        /// <param name="title">
        ///     The title.
        /// </param>
        /// <param name="id">
        ///     The id.
        /// </param>
        public void OpenLog(bool isFolder = false, string title = null, string id = null)
        {
            if (id == null)
                Process.Start(fullPath);
            else
            {
                var workingPath = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);

                if (!Directory.Exists(workingPath))
                {
                    Process.Start(fullPath);
                    return;
                }

                var latest = DateToFileName();

                if (!isFolder && File.Exists(Path.Combine(workingPath, latest)))
                    Process.Start(Path.Combine(workingPath, latest));
                else
                    Process.Start(workingPath);
            }
        }

        /// <summary>
        ///     The log special.
        /// </summary>
        /// <param name="title">
        ///     The title.
        /// </param>
        /// <param name="id">
        ///     The id.
        /// </param>
        /// <param name="kind">
        ///     The kind.
        /// </param>
        /// <param name="specialTitle">
        ///     The special title.
        /// </param>
        public void LogSpecial(string title, string id, SpecialLogMessageKind kind, string specialTitle)
        {
            using (var writer = AccessLog(title, id))
            {
                switch (kind)
                {
                    case SpecialLogMessageKind.LineBreak:
                        writer.WriteLine();
                        break;
                    case SpecialLogMessageKind.Header:
                    {
                        var head = string.Empty;
                        while (head.Length < specialTitle.Length + 4)
                        {
                            head += "=";
                        }

                        writer.WriteLine();
                        writer.WriteLine(head);
                        writer.WriteLine("= " + specialTitle + " =");
                        writer.WriteLine(head);
                        writer.WriteLine();

                        break;
                    }

                    case SpecialLogMessageKind.Section:
                    {
                        var head = string.Empty;
                        while (head.Length < specialTitle.Length + 4)
                        {
                            head += "-";
                        }

                        writer.WriteLine();
                        writer.WriteLine(head);
                        writer.WriteLine("- " + specialTitle + " -");
                        writer.WriteLine(head);
                        writer.WriteLine();

                        break;
                    }
                }
            }
        }

        #endregion

        #region Methods

        private static string DateToFileName()
        {
            var time = DateTimeOffset.Now.Date;

            var month = time.Month;
            var year = time.Year;
            var day = time.Day;

            return month + "-" + day + "-" + year + ".txt";
        }

        /// <summary>
        ///     Provides a streamwriter, given certain paramters
        /// </summary>
        /// <param name="title">
        ///     The Title.
        /// </param>
        /// <param name="id">
        ///     The ID.
        /// </param>
        /// <returns>
        ///     The <see cref="StreamWriter" />.
        /// </returns>
        private StreamWriter AccessLog(string title, string id)
        {
            var loggingPath = StaticFunctions.MakeSafeFolderPath(currentCharacter, title, id);

            var fileName = DateToFileName();

            if (!Directory.Exists(loggingPath))
                Directory.CreateDirectory(loggingPath);

            return new StreamWriter(Path.Combine(loggingPath, fileName), true);
        }

        #endregion
    }
}