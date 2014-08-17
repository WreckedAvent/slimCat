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
    using Microsoft.Practices.Prism;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Unity;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web;
    using Utilities;
    using Application = System.Windows.Application;
    using HtmlDocument = HtmlAgilityPack.HtmlDocument;

    #endregion

    public class NoteService : INoteService
    {
        #region Fields
        private readonly IBrowser browser;

        private const string NoteXpath = "//div[contains(@class, 'panel') and contains(@class, 'FormattedBlock')]";

        private const string NoteTitleXpath = "//input[@id='SendNoteTitle']";

        private const string NoteIdXpath = "//select/option[normalize-space(.)='{0}']";

        private readonly IDictionary<string, Conversation> noteCache = new Dictionary<string, Conversation>();

        private readonly ICharacterManager characterManager;

        private readonly IChatModel cm;

        private readonly IEventAggregator events;

        private readonly IUnityContainer container;

        private readonly Regex amPmRegex = new Regex("sent,|(AM:)|(PM:)", RegexOptions.Compiled);
        #endregion

        #region Constructors
        public NoteService(IUnityContainer contain, IBrowser browser, ICharacterManager characterMan, IChatModel cm, IEventAggregator eventagg)
        {
            this.browser = browser;
            characterManager = characterMan;
            this.cm = cm;
            events = eventagg;
            container = contain;
        }
        #endregion

        #region Public Methods
        public void GetNotes(string characterName)
        {
            Log(string.Format("getting notes for {0}", characterName));

            if (characterName.Equals(cm.CurrentCharacter.Name))
                return;

            Conversation cache;
            if (noteCache.TryGetValue(characterName, out cache))
            {
                Log(string.Format("notes for {0} in cache already", characterName));
                var model = container.Resolve<PmChannelModel>(characterName);
                if (model == null) return;

                model.Notes.Clear();
                model.Notes.AddRange(cache.Messages);
                return;
            }

            var worker = new BackgroundWorker();
            worker.DoWork += GetNotesAsync;
            worker.RunWorkerAsync(characterName);
        }

        public void UpdateNotes(string characterName)
        {
            noteCache.Remove(characterName);
            GetNotes(characterName);
        }

        public string GetLastSubject(string character)
        {
            Conversation conversation;
            return noteCache.TryGetValue(character, out conversation) 
                ? conversation.Subject 
                : string.Empty;
        }

        public void SendNote(string message, string characterName, string subject)
        {
            var conversation = noteCache[characterName];

            var args = new Dictionary<string, object>
            {
                { "title", subject ?? conversation.Subject },
                { "message", message },
                { "dest", characterName },
                { "source",  conversation.SourceId }
            };

            var worker = new BackgroundWorker();
            worker.DoWork += SendNoteAsync;
            worker.RunWorkerAsync(args);
        }

        private void SendNoteAsync(object sender, DoWorkEventArgs e)
        {
            var args = (IDictionary<string, object>) e.Argument;
            var characterName = (string)args["dest"];
            var message = (string)args["message"];
            var subject = (string)args["title"];

            var resp = browser.GetResponse(Constants.UrlConstants.SendNote, args, true);
            var json = (JObject)JsonConvert.DeserializeObject(resp);

            JToken errorMessage;
            var error = string.Empty;
            if (json.TryGetValue("error", out errorMessage))
            {
                error = errorMessage.ToString();
                events.GetEvent<ErrorEvent>().Publish(error);
            }

            if (!string.IsNullOrEmpty(error)) return;

            var model = cm.CurrentPms.FirstByIdOrNull(characterName);
            if (model == null) return;

            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                model.Notes.Add(
                    new MessageModel(cm.CurrentCharacter,
                        message));
                model.NoteSubject = subject;
            }));
        }

        private void GetNotesAsync(object s, DoWorkEventArgs e)
        {
            var characterName = (string)e.Argument;

            var notes = new List<IMessage>();

            var resp = browser.GetResponse(Constants.UrlConstants.ViewHistory + characterName, true);

            var htmlDoc = new HtmlDocument
            {
                OptionCheckSyntax = false
            };

            HtmlNode.ElementsFlags.Remove("option");
            htmlDoc.LoadHtml(resp);

            if (htmlDoc.DocumentNode == null)
                return;

            var title = string.Empty;
            {
                var titleInput = htmlDoc.DocumentNode.SelectSingleNode(NoteTitleXpath);

                if (titleInput != null)
                {
                    var value = titleInput.Attributes
                        .Where(x => x.Name == "value")
                        .Select(x => x.Value)
                        .FirstOrDefault();

                    title = value ?? title;

                    // our title will get escaped each time it is sent with entities in it
                    // so try and decode it until there's none left
                    bool hasEscaped;
                    do
                    {
                        var escaped = HttpUtility.HtmlDecode(title);
                        hasEscaped = escaped != title;
                        title = escaped;
                    } while (hasEscaped);
                    Log("note subject to {0} is {1}".FormatWith(characterName, title));
                }
            }

            var sourceId = string.Empty;
            {
                var sourceIdInput = htmlDoc.DocumentNode.SelectSingleNode(NoteIdXpath.FormatWith(cm.CurrentCharacter.Name));

                if (sourceIdInput != null)
                {
                    var value = sourceIdInput.Attributes
                        .Where(x => x.Name.Equals("value"))
                        .Select(x => x.Value)
                        .FirstOrDefault();

                    sourceId = value ?? sourceId;
                    Log("ID for sending notes to {0} is {1}".FormatWith(characterName, sourceId));
                }
            }


            var result = htmlDoc.DocumentNode.SelectNodes(NoteXpath);
            {
                if (result != null && result.Count > 0)
                {
                    Log("parsing note history for {0}, {1} items".FormatWith(characterName, result.Count));
                    result.Select(x =>
                    {
                        Log(x.InnerText);
                        var isFuzzyTime = true;
                        var split = x.InnerText.Split(new[] { "sent,", "ago:" }, 3, StringSplitOptions.RemoveEmptyEntries);
                        if (split.Length < 3)
                        {
                            split = amPmRegex.Split(x.InnerText, 3).ToArray();
                            split[1] = split[1] + split[2];
                            split[2] = split[3];
                            split[3] = null;

                            isFuzzyTime = false;
                        }

                        return new MessageModel(
                            characterManager.Find(split[0].Trim()),
                            HttpUtility.HtmlDecode(split[2]),
                            isFuzzyTime ? FromFuzzyString(split[1].Trim()) : FromExactString(split[1].Trim()));
                    })
                    .Each(notes.Add);
                }

                noteCache.Add(characterName,
                    new Conversation
                    {
                        Messages = notes,
                        Subject = title,
                        SourceId = sourceId
                    });

                try
                {
                    var model = container.Resolve<PmChannelModel>(characterName);
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        model.Notes.Clear();
                        model.Notes.AddRange(notes);
                        model.NoteSubject = title;
                    });
                }
                catch
                {
                }
            }
        }

        private static DateTime FromFuzzyString(string timeAgo)
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

        private static DateTime FromExactString(string date)
        {
            // "at 03 Aug,2014 6:35:49"
            const string format = "'at' dd MMM,yyyy h:mm:ss tt:";
            return DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
        }

        [Conditional("DEBUG")]
        private static void Log(string text)
        {
            Logging.LogLine(text, "note serv");
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
