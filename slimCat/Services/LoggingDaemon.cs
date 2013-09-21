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

namespace Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Web;

    using Models;

    /// <summary>
    ///     The logging daemon.
    /// </summary>
    public class LoggingDaemon : ILogger
    {
        #region Fields

        private readonly string _fullPath;

        private readonly string _thisCharacter;

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
            this._thisCharacter = characterName;

            this._fullPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "slimCat", this._thisCharacter);

            if (!Directory.Exists(this._fullPath))
            {
                Directory.CreateDirectory(this._fullPath);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get logs.
        /// </summary>
        /// <param name="Title">
        /// The title.
        /// </param>
        /// <param name="ID">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public IEnumerable<string> GetLogs(string Title, string ID)
        {
            string loggingPath = StaticFunctions.MakeSafeFolderPath(this._thisCharacter, Title, ID);
            IEnumerable<string> toReturn = new List<string>();
            string fileName = this.dateToFileName();

            if (!Directory.Exists(loggingPath))
            {
                return new List<string>();
            }

            string toGet = Path.Combine(loggingPath, fileName);

            if (File.Exists(toGet))
            {
                IEnumerable<string> lines = File.ReadLines(Path.Combine(loggingPath, fileName));
                int toSkip = Math.Max(lines.Count() - 10, 0);

                toReturn = lines.Skip(toSkip);
            }

            return toReturn;
        }

        /// <summary>
        /// The log message.
        /// </summary>
        /// <param name="Title">
        /// The title.
        /// </param>
        /// <param name="ID">
        /// The id.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void LogMessage(string Title, string ID, IMessage message)
        {
            using (StreamWriter writer = this.accessLog(Title, ID))
            {
                string thisMessage = HttpUtility.HtmlDecode(message.Message);
                string timestamp = message.TimeStamp;

                if (message.Type == MessageType.normal)
                {
                    if (!message.Message.StartsWith("/me"))
                    {
                        writer.WriteLine(timestamp + ' ' + message.Poster.Name + ": " + thisMessage);
                    }
                    else
                    {
                        writer.WriteLine(timestamp + ' ' + message.Poster.Name + thisMessage.Substring(3));
                    }
                }
                else if (message.Type == MessageType.roll)
                {
                    writer.WriteLine(timestamp + ' ' + message);
                }
                else
                {
                    if (!message.Message.StartsWith("/me"))
                    {
                        writer.WriteLine("Ad at " + timestamp + ": " + thisMessage + " ~By " + message.Poster.Name);
                    }
                    else
                    {
                        writer.WriteLine(
                            "Ad at " + timestamp + ": " + message.Poster.Name + " " + thisMessage.Substring(3));
                    }
                }
            }
        }

        /// <summary>
        /// The log special.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="ID">
        /// The id.
        /// </param>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <param name="specialTitle">
        /// The special title.
        /// </param>
        public void LogSpecial(string title, string ID, SpecialLogMessageKind kind, string specialTitle)
        {
            using (StreamWriter writer = this.accessLog(title, ID))
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
        /// <param name="Title">
        /// The title.
        /// </param>
        /// <param name="ID">
        /// The id.
        /// </param>
        public void OpenLog(bool isFolder = false, string Title = null, string ID = null)
        {
            if (ID == null)
            {
                Process.Start(this._fullPath);
            }
            else
            {
                string workingPath = StaticFunctions.MakeSafeFolderPath(this._thisCharacter, Title, ID);

                if (!Directory.Exists(workingPath))
                {
                    Process.Start(this._fullPath);
                    return;
                }

                string latest = this.dateToFileName();

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
        /// <param name="Title">
        /// The Title.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        /// <returns>
        /// The <see cref="StreamWriter"/>.
        /// </returns>
        private StreamWriter accessLog(string Title, string ID)
        {
            string loggingPath = StaticFunctions.MakeSafeFolderPath(this._thisCharacter, Title, ID);

            string fileName = this.dateToFileName();

            if (!Directory.Exists(loggingPath))
            {
                Directory.CreateDirectory(loggingPath);
            }

            return new StreamWriter(Path.Combine(loggingPath, fileName), true);
        }

        private string dateToFileName()
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
    ///     The Logger interface.
    /// </summary>
    public interface ILogger
    {
        #region Public Methods and Operators

        /// <summary>
        /// Returns the last few messages from a given channel
        /// </summary>
        /// <param name="Title">
        /// The Title.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        IEnumerable<string> GetLogs(string Title, string ID);

        /// <summary>
        /// Logs a given message in a given channel
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        void LogMessage(string title, string ID, IMessage message);

        /// <summary>
        /// Prints a special message to the log, such as a header
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        /// <param name="type">
        /// The type of special message
        /// </param>
        /// <param name="specialTitle">
        /// The title for the special message
        /// </param>
        void LogSpecial(string title, string ID, SpecialLogMessageKind type, string specialTitle);

        /// <summary>
        /// Opens the log in the default text editor
        /// </summary>
        /// <param name="isFolder">
        /// The is Folder.
        /// </param>
        /// <param name="Title">
        /// The Title.
        /// </param>
        /// <param name="ID">
        /// The ID.
        /// </param>
        void OpenLog(bool isFolder, string Title = null, string ID = null);

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