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

    using System.Net;
    using HtmlAgilityPack;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Unity;
    using Models;
    using System;
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

        private readonly ICharacterManager characterManager;

        private readonly IChatModel cm;

        private readonly IEventAggregator events;

        private readonly IUnityContainer container;

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
                profileBody = profileBody.Replace("<br>", "");
            }

            var model = container.Resolve<PmChannelModel>(characterName);

            model.ProfileText = profileBody;
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
