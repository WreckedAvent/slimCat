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

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Models;
using System.Diagnostics;

namespace Services
{
    public class LoggingDaemon : ILogger
    {
        private string _fullPath;
        private string _thisCharacter;

        /// <summary>
        /// Creates a new logging daemon for a given account name
        /// </summary>
        public LoggingDaemon(string characterName)
        {
            _thisCharacter = characterName;

            _fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "slimCat", _thisCharacter);

            if (!Directory.Exists(_fullPath))
                Directory.CreateDirectory(_fullPath);
        }

        public void LogMessage(string Title, string ID, IMessage message)
        {
            using (var writer = accessLog(Title, ID))
            {
                var thisMessage = System.Web.HttpUtility.HtmlDecode(message.Message);
                var timestamp = message.TimeStamp;

                if (message.Type == MessageType.normal)
                {
                    if (!message.Message.StartsWith("/me"))
                        writer.WriteLine(timestamp + ' ' + message.Poster.Name + ": " + thisMessage);
                    else
                        writer.WriteLine(timestamp + ' ' + message.Poster.Name + thisMessage.Substring(3));
                }

                else if (message.Type == MessageType.roll)
                    writer.WriteLine(timestamp + ' ' + message);

                else
                {
                    if (!message.Message.StartsWith("/me"))
                        writer.WriteLine("Ad at " + timestamp + ": " + thisMessage + " ~By " + message.Poster.Name);
                    else
                        writer.WriteLine("Ad at " + timestamp + ": " + message.Poster.Name + " " + thisMessage.Substring(3));
                }
            }
        }

        public void LogSpecial(string title, string ID, SpecialLogMessageKind kind, string specialTitle)
        {
            using (var writer = accessLog(title, ID))
            {
                switch (kind)
                {
                    case SpecialLogMessageKind.LineBreak: writer.WriteLine(); break;
                    case SpecialLogMessageKind.Header:
                        {
                            string head = "";
                            while (head.Length < specialTitle.Length + 4)
                                head += "=";

                            writer.WriteLine();
                            writer.WriteLine(head);
                            writer.WriteLine("= " + specialTitle + " =");
                            writer.WriteLine(head);
                            writer.WriteLine();

                            break;
                        }

                    case SpecialLogMessageKind.Section:
                        {
                            string head = "";
                            while (head.Length < specialTitle.Length + 4)
                                head += "-";

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

        public void OpenLog(bool isFolder = false, string Title = null, string ID = null)
        {
            if (ID == null)
                Process.Start(_fullPath);
            else
            {
                var workingPath = StaticFunctions.MakeSafeFolderPath(_thisCharacter, Title, ID);

                if (!Directory.Exists(workingPath))
                {
                    Process.Start(_fullPath);
                    return;
                }

                var latest = dateToFileName();

                if (!isFolder && File.Exists(Path.Combine(workingPath, latest)))
                    Process.Start(Path.Combine(workingPath, latest));
                else
                    Process.Start(workingPath);
            }
        }
        /// <summary>
        /// Provides a streamwriter, given certain paramters
        /// </summary>
        private StreamWriter accessLog(string Title, string ID)
        {
            string loggingPath = StaticFunctions.MakeSafeFolderPath(_thisCharacter, Title, ID);

            var fileName = dateToFileName();

            if (!Directory.Exists(loggingPath))
                Directory.CreateDirectory(loggingPath);

            return new StreamWriter(Path.Combine(loggingPath, fileName), true);
        }

        private string dateToFileName()
        {
            var time = DateTimeOffset.Now.Date;

            var month = time.Month;
            var year = time.Year;
            var day = time.Day;

            return month.ToString() + "-" + day.ToString() + "-" + year.ToString() + ".txt";
        }

        public IEnumerable<IMessage> GetLogs(string ID)
        {
            throw new NotImplementedException();
        }
    }

    public interface ILogger
    {
        /// <summary>
        /// Logs a given message in a given channel
        /// </summary>
        void LogMessage(string title, string ID, IMessage message);

        /// <summary>
        /// Prints a special message to the log, such as a header 
        /// </summary>
        /// <param name="type">The type of special message</param>
        /// <param name="specialTitle">The title for the special message</param>
        void LogSpecial(string title, string ID, SpecialLogMessageKind type, string specialTitle);

        /// <summary>
        /// Opens the log in the default text editor
        /// </summary>
        void OpenLog(bool isFolder, string Title = null, string ID = null);

        /// <summary>
        /// Returns the last few messages from a given channel
        /// </summary>
        IEnumerable<IMessage> GetLogs(string ID);
    }

    public enum SpecialLogMessageKind
    {
        Header,
        Section,
        LineBreak
    }
}
