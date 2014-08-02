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

    #endregion

    public class NoteService : INoteService
    {
        #region Fields
        private readonly IBrowser browser;

        private const string NoteUrl = "https://www.f-list.net/history.php?name=";

        private const string NoteXpath = "//div[contains(@class, 'panel') and contains(@class, 'FormattedBlock')]";

        private IDictionary<string, IList<IMessage>> noteCache = new Dictionary<string, IList<IMessage>>();

        private readonly ICharacterManager characterManager;
        #endregion

        #region Constructors
        public NoteService(IBrowser browser, ICharacterManager characterMan)
        {
            this.browser = browser;
            characterManager = characterMan;
        }
        #endregion

        #region Public Methods
        public IList<IMessage> GetNotes(string characterName)
        {
            IList<IMessage> notes;
            if (noteCache.TryGetValue(characterName, out notes))
            {
                return notes;
            }
            notes = new List<IMessage>();

            var resp = browser.GetResponse(NoteUrl + characterName, true);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(resp);

            if (htmlDoc.DocumentNode == null)
                return notes;

            var result = htmlDoc.DocumentNode.SelectNodes(NoteXpath);

            if (result == null || result.Count == 0)
                return notes;

            result.Select(x =>
            {
                var split = x.InnerText.Split(new[] { "sent,", "ago:" }, 3, StringSplitOptions.RemoveEmptyEntries);

                return new MessageModel(
                    characterManager.Find(split[0].Trim()),
                    HttpUtility.HtmlDecode(split[2]),
                    FromAgoString(split[1].Trim()));
            })
            .Each(x => notes.Add(x));

            noteCache.Add(characterName, notes);

            return notes;
        }

        public void RemoveNoteCache(string characterName)
        {
            noteCache.Remove(characterName);
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
}
