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

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using HtmlAgilityPack;
    using Microsoft.Practices.Unity;
    using Models;
    using System.ComponentModel;
    using System.Diagnostics;
    using Utilities;
    using HtmlDocument = HtmlAgilityPack.HtmlDocument;

    #endregion

    public class ProfileService : IProfileService
    {
        #region Fields
        private readonly IBrowser browser;

        private const string ProfileBody = "//div[@id = 'tabs-1']/*[1]";

        private const string ProfileStats = "//div[@class = 'statbox']";

        private readonly Regex quickProfileRegex = new Regex("(.*:.*)");

        private readonly IUnityContainer container;

        private readonly IDictionary<string, string> profileCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); 

        #endregion

        #region Constructors
        public ProfileService(IUnityContainer contain, IBrowser browser)
        {
            this.browser = browser;

            container = contain;
        }
        #endregion

        #region Public Methods

        private void GetProfileDataAsyncHandler(object s, DoWorkEventArgs e)
        {
            var characterName = (string)e.Argument;
            var model = container.Resolve<PmChannelModel>(characterName);

            string cache;
            if (profileCache.TryGetValue(characterName, out cache))
            {
                model.ProfileText = cache;
                return;
            }

            var resp = browser.GetResponse(Constants.UrlConstants.CharacterPage + characterName, true);

            var htmlDoc = new HtmlDocument
            {
                OptionCheckSyntax = false
            };

            HtmlNode.ElementsFlags.Remove("option");
            htmlDoc.LoadHtml(resp);

            if (htmlDoc.DocumentNode == null)
                return;

            string profileBody;
            var result = htmlDoc.DocumentNode.SelectNodes(ProfileBody);
            {
                if (result == null || result.Count != 1)
                    return;

                profileBody = WebUtility.HtmlDecode(result[0].InnerHtml);
                profileBody = profileBody.Replace("<br>", "\n");
            }
            model.ProfileText = profileBody;
            profileCache[characterName] = profileBody;

            var quickSection = htmlDoc.DocumentNode.SelectNodes(ProfileStats);
            {
                if (quickSection == null || quickSection.Count == 0)
                    return;

                return;

                var hits = quickSection[0].InnerText
                    .Split(new[] {':', '\t', '\r', '\n'})
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList()
                    .Chunk(2).ToList();
                    //.Select(x => x.ToList())
                    //.Select(x => new ProfileTag { Label = x[0], Value = x[1] })
                    //.ToList();

                Console.WriteLine(hits[0]);
            }
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

    public class ProfileData
    {
        public string Age { get; set; }

        public Gender Gender { get; set; }

        public string LanguagePreference { get; set; }

        public string FurryPreference { get; set; }

        public string DomSubRole { get; set; }

        public string DesiredRpLength { get; set; }

        public string Species { get; set; }
    }

    class ProfileTag
    {
        public string Label { get; set; }

        public string Value { get; set; }
    }
}
