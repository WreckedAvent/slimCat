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
        private string _basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\slimCat\logs";
        private string _fullPath;
        private string _thisCharacter;

        /// <summary>
        /// Creates a new logging daemon for a given account name
        /// </summary>
        public LoggingDaemon(string characterName)
        {
            _thisCharacter = characterName;

            _fullPath = _basePath + "\\" + _thisCharacter;

            if (!Directory.Exists(_fullPath))
                Directory.CreateDirectory(_fullPath);
        }

        public void LogMessage(string Title, string ID, IMessage message)
        {
            using (var writer = accessLog(Title, ID))
            {
                var timestamp = message.TimeStamp;

                if (message.Type == MessageType.normal)
                {
                    if (!message.Message.StartsWith("/me"))
                        writer.WriteLine(timestamp + ' ' + message.Poster.Name + ": " + message.Message);
                    else
                        writer.WriteLine(timestamp + ' ' + message.Poster.Name + message.Message.Substring(3));
                }
                else
                {
                    if (!message.Message.StartsWith("/me"))
                        writer.WriteLine("Ad at " + timestamp + ": " + message.Message + " ~By " + message.Poster.Name);
                    else
                        writer.WriteLine("Ad at " + timestamp + ": " + message.Poster.Name + " " + message.Message.Substring(3));
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

        /// <summary>
        /// Provides a streamwriter, given certain paramters
        /// </summary>
        private StreamWriter accessLog(string Title, string ID)
        {
            string loggingPath;
            if (Title.Equals(ID))
                loggingPath = _fullPath + "\\" + ID + "\\";
            else
            {
                string safeTitle = Title;
                foreach (var c in Path.GetInvalidPathChars())
                    safeTitle = safeTitle.Replace(c.ToString(), "");

                loggingPath = _fullPath + "\\" + safeTitle + " (" + ID + ")" + "\\";
            }

            var time = DateTimeOffset.Now.Date;

            var month = time.Month;
            var year = time.Year;
            var day = time.Day;

            var fileName = month.ToString() + "-" + day.ToString() + "-" + year.ToString() + ".txt";

            if (!Directory.Exists(loggingPath))
                Directory.CreateDirectory(loggingPath);

            return new StreamWriter(loggingPath + fileName, true);
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
