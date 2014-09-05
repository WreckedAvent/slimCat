#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggingService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Web;
    using Microsoft.Practices.Prism.Events;
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

    public class LoggingService : ILoggingService
    {
        #region Constructors and Destructors

        public LoggingService(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<CharacterSelectedLoginEvent>().Subscribe(OnCharacterSelected);
        }

        private void OnCharacterSelected(string character)
        {
            CurrentCharacter = character;

            FullPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "slimCat", CurrentCharacter);

            if (!Directory.Exists(FullPath))
                Directory.CreateDirectory(FullPath);
        }

        #endregion

        #region Properties

        private string CurrentCharacter { get; set; }

        private string FullPath { get; set; }

        #endregion

        #region Public Methods and Operators

        public IEnumerable<string> GetLogs(string title, string id)
        {
            var loggingPath = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, title, id);
            var toReturn = new List<string>();

            if (!Directory.Exists(loggingPath))
                return new List<string>();


            var file =
                new DirectoryInfo(loggingPath)
                    .GetFiles("*.txt")
                    .OrderByDescending(x => x.LastWriteTime).FirstOrDefault();

            if (file == null) return toReturn;

            try
            {
                var lines = File.ReadLines(file.FullName);
                var enumerable = lines as IList<string> ?? lines.ToList();

                var toSkip = Math.Max(enumerable.Count() - 10, 0);

                toReturn = enumerable.Skip(toSkip).ToList();
                toReturn.Insert(0, "[b]Log from {0}[/b]".FormatWith(file.LastWriteTime.ToShortDateString()));
            }
            catch
            {
                // file operations run the risk of exceptions
                return new List<string>();
            }

            return toReturn;
        }

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

        public void OpenLog(bool isFolder = false, string title = null, string id = null)
        {
            if (id == null)
                Process.Start(FullPath);
            else
            {
                var workingPath = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, title, id);

                if (!Directory.Exists(workingPath))
                {
                    Process.Start(FullPath);
                    return;
                }

                if (isFolder)
                {
                    Process.Start(workingPath);
                    return;
                }

                var latest = new DirectoryInfo(workingPath)
                    .GetFiles("*.txt")
                    .OrderByDescending(x => x.LastWriteTime)
                    .FirstOrDefault();

                Process.Start(latest != null ? latest.FullName : workingPath);
            }
        }

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

        private StreamWriter AccessLog(string title, string id)
        {
            var loggingPath = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, title, id);

            var fileName = DateToFileName();

            if (!Directory.Exists(loggingPath))
                Directory.CreateDirectory(loggingPath);

            return new StreamWriter(Path.Combine(loggingPath, fileName), true);
        }

        #endregion
    }
}