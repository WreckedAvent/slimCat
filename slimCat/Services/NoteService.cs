#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using HtmlAgilityPack;

    using Models;

    using Utilities;

    using System;

    using System.Collections.Generic;

    using System.Linq;

    using System.Text.RegularExpressions;

    using System.Web;

    using Microsoft.Practices.Prism.Events;

    using Newtonsoft.Json;

    using Newtonsoft.Json.Linq;

    using System.ComponentModel;

    #endregion

    public class NoteService : INoteService
    {
        #region Fields
        private readonly IBrowser browser;

        private const string NoteXpath = "//div[contains(@class, 'panel') and contains(@class, 'FormattedBlock')]";

        private const string NoteTitleXpath = "//input[@id='SendNoteTitle']";

        private const string NoteIdXpath = "//select/option[normalize-space(.)='{0}']";

        private IDictionary<string, Conversation> noteCache = new Dictionary<string, Conversation>();

        private readonly ICharacterManager characterManager;

        private readonly IChatModel cm;

        private readonly IEventAggregator events;
        #endregion

        #region Constructors
        public NoteService(IBrowser browser, ICharacterManager characterMan, IChatModel cm, IEventAggregator eventagg)
        {
            this.browser = browser;
            characterManager = characterMan;
            this.cm = cm;
            events = eventagg;
        }
        #endregion

        #region Public Methods
        public IList<IMessage> GetNotes(string characterName)
        {
            Conversation cache;
            if (noteCache.TryGetValue(characterName, out cache))
            {
                return cache.Messages;
            }

            var notes = new List<IMessage>();

            var resp = browser.GetResponse(Constants.UrlConstants.ViewHistory + characterName, true);

            var htmlDoc = new HtmlDocument();
            htmlDoc.OptionCheckSyntax = false;
            HtmlNode.ElementsFlags.Remove("option");
            htmlDoc.LoadHtml(resp);

            if (htmlDoc.DocumentNode == null)
                return notes;

            var result = htmlDoc.DocumentNode.SelectNodes(NoteXpath);

            if (result == null || result.Count == 0)
                return notes;

            var title = string.Empty;
            {
                var titleInput = htmlDoc.DocumentNode.SelectSingleNode(NoteTitleXpath);

                if (titleInput != null)
                {
                    var value = titleInput.Attributes
                        .Where(x => x.Name == "value")
                        .Select(x => x.Value)
                        .FirstOrDefault();

                    title = value == null ? title : value;
                }
            }

            var sourceId = string.Empty;
            {
                var sourceIdInput = htmlDoc.DocumentNode.SelectSingleNode(string.Format(NoteIdXpath, cm.CurrentCharacter.Name));

                if (sourceIdInput != null)
                {
                    var value = sourceIdInput.Attributes
                        .Where(x => x.Name.Equals("value"))
                        .Select(x => x.Value)
                        .FirstOrDefault();

                    sourceId = value == null ? sourceId : value;
                }
            }

            result.Select(x =>
            {
                var split = x.InnerText.Split(new[] { "sent,", "ago:" }, 3, StringSplitOptions.RemoveEmptyEntries);

                return new MessageModel(
                    characterManager.Find(split[0].Trim()),
                    HttpUtility.HtmlDecode(split[2]),
                    FromAgoString(split[1].Trim()));
            })
            .Each(x => notes.Add(x));

            noteCache.Add(characterName, new Conversation { Messages = notes, Subject = title, SourceId = sourceId});

            return notes;
        }

        public void UpdateNoteCache(string characterName)
        {
            noteCache.Remove(characterName);
            GetNotes(characterName);
        }

        public void SendNote(string message, string characterName)
        {
            var conversation = noteCache[characterName];

            var args = new Dictionary<string, object>()
            {
                { "title", conversation.Subject },
                { "message", message },
                { "dest", characterName },
                { "source",  conversation.SourceId }
            };

            var worker = new BackgroundWorker();
            worker.DoWork += (o, arg) =>
            {

                var resp = browser.GetResponse(Constants.UrlConstants.SendNote, args, true);
                var json = (JObject)JsonConvert.DeserializeObject(resp);

                JToken errorMessage;
                if (json.TryGetValue("error", out errorMessage))
                {
                    var value = errorMessage.ToString();
                    events.GetEvent<ErrorEvent>().Publish(value);
                }
            };
            worker.RunWorkerAsync();
        }

        private DateTime FromAgoString(string timeAgo)
        {
            // captures a string like '8m, 25d, 8h, 55m'
            const string regex = @"(\d+y|\d+mo|\d+d|\d+h|\d+m),? *";

            var split = Regex
                .Split(timeAgo, regex, RegexOptions.Compiled)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var toReturn = DateTime.Now;

            foreach (var date in split)
            {
                var splitDate = Regex
                    .Split(date, @"(\d+)", RegexOptions.Compiled)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                int numberPart;
                int.TryParse(splitDate[0], out numberPart);

                var datePart = splitDate[1];

                switch (datePart)
                {
                    case "y": toReturn = toReturn.Subtract(TimeSpan.FromDays(365 * numberPart)); break;
                    case "mo": toReturn = toReturn.Subtract(TimeSpan.FromDays(27 * numberPart)); break;
                    case "d": toReturn = toReturn.Subtract(TimeSpan.FromDays(numberPart)); break;
                    case "h": toReturn = toReturn.Subtract(TimeSpan.FromHours(numberPart)); break;
                    case "m": toReturn = toReturn.Subtract(TimeSpan.FromMinutes(numberPart)); break;
                }
            }

            return toReturn;
        } 
        #endregion
    }

    class Conversation
    {
        public IList<IMessage> Messages { get; set; }
        public string SourceId { get; set; }
        public string Subject { get; set; }
    }

}
