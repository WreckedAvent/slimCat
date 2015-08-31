#region Copyright

// <copyright file="MessageModel.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Documents;
    using Views;
    using Utilities;

    #endregion

    /// <summary>
    ///     A model to hold data on messages
    /// </summary>
    public class MessageModel : MessageBase, IMessage
    {
        private bool isLastViewed;
        private bool isOfInterest;

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            Poster = null;
        }

        #endregion

        #region Constructors and Destructors

        public MessageModel(ICharacter poster, string message, MessageType type = MessageType.Normal)
        {
            Poster = poster;
            Message = message;
            Type = type;
        }


        public MessageModel(string fullText, Func<string, ICharacter> getCharacter, DateTime dateOfLogs)
        {
            var adSignifier = "[Ad]";
            Poster = new CharacterModel {Name = string.Empty};
            Type = fullText.StartsWith(adSignifier)
                ? MessageType.Ad
                : MessageType.Normal;

            IsHistoryMessage = true;
            Message = fullText;


            var parts = fullText.Split(new[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                try
                {
                    var nameBadge = parts[0];
                    var message = parts[1];

                    if (nameBadge.StartsWith(adSignifier)) nameBadge = nameBadge.Substring(adSignifier.Length);

                    var format = DateTimeExtensions.GetTimestampFormat();
                    var nameIdx = nameBadge.IndexOf(format.Trim().Last()) + 1;

                    var name = nameBadge.Substring(nameIdx).Trim();
                    var timeStamp = nameBadge.Substring(0, nameIdx).Trim();


                    // parse our date, and set the date component based on the last write time of the log
                    DateTime parsedDate;
                    if (!DateTime.TryParseExact(timeStamp, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)) return;
                    parsedDate = new DateTime(dateOfLogs.Year, dateOfLogs.Month, dateOfLogs.Day, parsedDate.Hour, parsedDate.Minute, parsedDate.Second);

                    PostedTime = parsedDate;
                    Poster = getCharacter(name);
                    Message = message;
                    IsHistoryMessage = false;
                }
                catch
                {
                }
            }
        }

        public MessageModel(ICharacter poster, string message, DateTimeOffset posted)
            : base(posted)
        {
            Poster = poster;
            Message = message;
            Type = MessageType.Normal;
        }

        #endregion

        #region Public Properties

        public string Message { get; }

        public ICharacter Poster { get; set; }

        public MessageType Type { get; }

        public bool IsHistoryMessage { get; }

        public bool IsOfInterest
        {
            get { return isOfInterest; }
            set
            {
                isOfInterest = value;
                OnPropertyChanged(string.Empty);
            }
        }

        public bool IsLastViewed
        {
            get { return isLastViewed; }
            set
            {
                isLastViewed = value;
                OnPropertyChanged(string.Empty);
            }
        }

        public MessageModel This => this;

        public Block View
        {
            get
            {
                if (IsHistoryMessage)
                    return new HistoryView {DataContext = Message};

                return new MessageView {DataContext = this};
            }
        }

        #endregion
    }
}