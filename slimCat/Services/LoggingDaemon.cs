// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingDaemon.cs" company="Justin Kadrovach">
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
//   The logging daemon.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Web;

    using Slimcat.Models;
    using Slimcat.Utilities;

    /// <summary>
    ///     The logging daemon.
    /// </summary>
    public class LoggingDaemon : ILogger
    {
        #region Fields

        private readonly string fullPath;

        private readonly string currentCharacter;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingDaemon"/> class.
        ///     Creates a new logging daemon for a given account name
        /// </summary>
        /// <param name="characterName">
        /// The character Name.
        /// </param>
        public LoggingDaemon(string characterName)
        {
            this.currentCharacter = characterName;

            this.fullPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "slimCat", this.currentCharacter);

            if (!Directory.Exists(this.fullPath))
            {
                Directory.CreateDirectory(this.fullPath);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get logs.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{T}"/>.
        /// </returns>
        public IEnumerable<string> GetLogs(string title, string id)
        {
            var loggingPath = StaticFunctions.MakeSafeFolderPath(this.currentCharacter, title, id);
            IEnumerable<string> toReturn = new List<string>();
            var fileName = DateToFileName();

            if (!Directory.Exists(loggingPath))
            {
                return new List<string>();
            }

            var toGet = Path.Combine(loggingPath, fileName);

            if (File.Exists(toGet))
            {
                var lines = File.ReadLines(Path.Combine(loggingPath, fileName));
                var enumerable = lines as IList<string> ?? lines.ToList();

                var toSkip = Math.Max(enumerable.Count() - 10, 0);

                toReturn = enumerable.Skip(toSkip);
            }

            return toReturn;
        }

        /// <summary>
        /// The log message.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void LogMessage(string title, string id, IMessage message)
        {
            using (var writer = this.AccessLog(title, id))
            {
                var thisMessage = HttpUtility.HtmlDecode(message.Message);
                var timestamp = message.TimeStamp;

                switch (message.Type)
                {
                    case MessageType.Normal:
                        if (!message.Message.StartsWith("/me"))
                        {
                            writer.WriteLine(timestamp + ' ' + message.Poster.Name + ": " + thisMessage);
                        }
                        else
                        {
                            writer.WriteLine(timestamp + ' ' + message.Poster.Name + thisMessage.Substring(3));
                        }

                        break;
                    case MessageType.Roll:
                        writer.WriteLine(timestamp + ' ' + message);
                        break;
                    default:
                        if (!message.Message.StartsWith("/me"))
                        {
                            writer.WriteLine("Ad at " + timestamp + ": " + thisMessage + " ~By " + message.Poster.Name);
                        }
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
        /// The log special.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <param name="specialTitle">
        /// The special title.
        /// </param>
        public void LogSpecial(string title, string id, SpecialLogMessageKind kind, string specialTitle)
        {
            using (StreamWriter writer = this.AccessLog(title, id))
            {
                switch (kind)
                {
                    case SpecialLogMessageKind.LineBreak:
                        writer.WriteLine();
                        break;
                    case SpecialLogMessageKind.Header:
                        {
                            string head = string.Empty;
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
                            string head = string.Empty;
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

        /// <summary>
        /// The open log.
        /// </summary>
        /// <param name="isFolder">
        /// The is folder.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        public void OpenLog(bool isFolder = false, string title = null, string id = null)
        {
            if (id == null)
            {
                Process.Start(this.fullPath);
            }
            else
            {
                var workingPath = StaticFunctions.MakeSafeFolderPath(this.currentCharacter, title, id);

                if (!Directory.Exists(workingPath))
                {
                    Process.Start(this.fullPath);
                    return;
                }

                var latest = DateToFileName();

                if (!isFolder && File.Exists(Path.Combine(workingPath, latest)))
                {
                    Process.Start(Path.Combine(workingPath, latest));
                }
                else
                {
                    Process.Start(workingPath);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Provides a streamwriter, given certain paramters
        /// </summary>
        /// <param name="title">
        /// The Title.
        /// </param>
        /// <param name="id">
        /// The ID.
        /// </param>
        /// <returns>
        /// The <see cref="StreamWriter"/>.
        /// </returns>
        private StreamWriter AccessLog(string title, string id)
        {
            string loggingPath = StaticFunctions.MakeSafeFolderPath(this.currentCharacter, title, id);

            string fileName = DateToFileName();

            if (!Directory.Exists(loggingPath))
            {
                Directory.CreateDirectory(loggingPath);
            }

            return new StreamWriter(Path.Combine(loggingPath, fileName), true);
        }

        private static string DateToFileName()
        {
            DateTime time = DateTimeOffset.Now.Date;

            int month = time.Month;
            int year = time.Year;
            int day = time.Day;

            return month.ToString() + "-" + day.ToString() + "-" + year.ToString() + ".txt";
        }

        #endregion
    }

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
}