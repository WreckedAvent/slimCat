#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProfileService.cs">
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
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Unity;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Utilities;
    using HtmlDocument = HtmlAgilityPack.HtmlDocument;

    #endregion

    public class ProfileService : IProfileService
    {
        #region Fields
        private readonly IBrowser browser;

        private const string ProfileBody = "//div[@id = 'tabs-1']/*[1]";

        private readonly ICharacterManager characterManager;

        private readonly IChatModel cm;

        private readonly IEventAggregator events;

        private readonly IUnityContainer container;

        private readonly Regex amPmRegex = new Regex("sent,|(AM:)|(PM:)", RegexOptions.Compiled);
        #endregion

        #region Constructors
        public ProfileService(IUnityContainer contain, IBrowser browser, ICharacterManager characterMan, IChatModel cm, IEventAggregator eventagg)
        {
            this.browser = browser;
            characterManager = characterMan;
            this.cm = cm;
            events = eventagg;
            container = contain;
        }
        #endregion

        #region Public Methods

        private void GetProfileDataAsyncHandler(object s, DoWorkEventArgs e)
        {
            var characterName = (string)e.Argument;

            var notes = new List<IMessage>();

            var resp = browser.GetResponse(Constants.UrlConstants.CharacterPage + characterName, true);

            var htmlDoc = new HtmlDocument
            {
                OptionCheckSyntax = false
            };

            HtmlNode.ElementsFlags.Remove("option");
            htmlDoc.LoadHtml(resp);

            if (htmlDoc.DocumentNode == null)
                return;


            var result = htmlDoc.DocumentNode.SelectNodes(ProfileBody);
            {
                if (result == null || result.Count != 1)
                    return;

                Console.WriteLine(result[0].InnerHtml.Substring(0, 400));

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
            Logging.LogLine(text, "profile serv");
        }
        #endregion

        public void GetProfileDataAsync(string character)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += GetProfileDataAsyncHandler;
            worker.RunWorkerAsync(character);
        }
    }
}
