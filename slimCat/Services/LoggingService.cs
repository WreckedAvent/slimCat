#region Copyright

// <copyright file="LoggingService.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
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

#endregion

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Web;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Utilities;

    #endregion

    /// <summary>
    ///     The logging service logs to text files.
    /// </summary>
    public class LoggingService : ILogThings
    {
        #region Constructors and Destructors

        public LoggingService(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<CharacterSelectedLoginEvent>().Subscribe(OnCharacterSelected);
        }

        private void OnCharacterSelected(string character)
        {
            CurrentCharacter = character;

            if (!SettingsService.Preferences.IsPortable)
                FullPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "slimCat", CurrentCharacter);
            else
            {
                var exeFolder = (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory.ToString();
                FullPath = Path.Combine(exeFolder, "logs", CurrentCharacter);
            }

            if (!Directory.Exists(FullPath))
                Directory.CreateDirectory(FullPath);
        }

        #endregion

        #region Properties

        private string CurrentCharacter { get; set; }

        private string FullPath { get; set; }

        #endregion

        #region Public Methods and Operators

        public RawChannelLogModel GetLogs(string title, string id)
        {
            var loggingPath = StringExtensions.MakeSafeFolderPath(CurrentCharacter, title, id);
            var toReturn = new RawChannelLogModel();

            if (!Directory.Exists(loggingPath))
                return toReturn;

            var file =
                new DirectoryInfo(loggingPath)
                    .GetFiles("*.txt")
                    .OrderByDescending(x => x.LastWriteTime).FirstOrDefault();

            if (file == null) return toReturn;

            try
            {
                var lines = File.ReadLines(file.FullName);
                var enumerable = lines as IList<string> ?? lines.ToList();

                var toSkip = Math.Max(enumerable.Count() - 25, 0);

                toReturn.RawLogs = enumerable.Skip(toSkip).ToList();
                toReturn.DateOfLog = file.LastWriteTime;
            }
            catch
            {
                // file operations run the risk of exceptions
                return toReturn;
            }

            return toReturn;
        }

        public void LogMessage(string title, string id, IMessage message)
        {
            using (var writer = AccessLog(title, id))
            {
                var safeMessage = HttpUtility.HtmlDecode(message.Message);
                var timestamp = message.TimeStamp;
                var timestampUsernameStub = timestamp + ' ' + message.Poster.Name;

                switch (message.Type)
                {
                    case MessageType.Roll:
                    case MessageType.Normal:
                        writer.WriteLine(timestampUsernameStub + ": " + safeMessage);
                        break;
                    default:
                        writer.WriteLine($"[Ad] {timestampUsernameStub}: {safeMessage}");
                        break;
                }
            }
        }

        public void LogMessage(string title, NotificationModel model)
        {
            using (var writer = AccessLog(title, title))
            {
                var thisMessage = HttpUtility.HtmlDecode(model.ToString());
                var timestamp = model.TimeStamp;

                writer.WriteLine(timestamp + ' ' + thisMessage);
            }
        }

        public void OpenLog(bool isFolder = false, string title = null, string id = null)
        {
            if (id == null)
                Process.Start(FullPath);
            else
            {
                var workingPath = StringExtensions.MakeSafeFolderPath(CurrentCharacter, title, id);

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

                Process.Start(latest?.FullName ?? workingPath);
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

            return year + "-" + month + "-" + day + ".txt";
        }

        private StreamWriter AccessLog(string title, string id)
        {
            var loggingPath = StringExtensions.MakeSafeFolderPath(CurrentCharacter, title, id);

            var fileName = DateToFileName();

            if (!Directory.Exists(loggingPath))
                Directory.CreateDirectory(loggingPath);

            return new StreamWriter(Path.Combine(loggingPath, fileName), true);
        }

        #endregion
    }

    public class RawChannelLogModel
    {
        public RawChannelLogModel()
        {
            RawLogs = new List<string>();
        }

        public IList<string> RawLogs { get; set; }
        public DateTime DateOfLog { get; set; }
    }
}