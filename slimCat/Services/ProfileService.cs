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
    using HtmlAgilityPack;
    using Microsoft.Practices.Unity;
    using Models;
    using System.ComponentModel;
    using System.Diagnostics;
    using Models.Api;
    using Newtonsoft.Json;
    using Utilities;
    using HtmlDocument = HtmlAgilityPack.HtmlDocument;

    #endregion

    public class ProfileService : IProfileService
    {
        #region Fields
        private readonly IBrowser browser;

        private const string ProfileBodySelector = "//div[@id = 'tabs-1']/*[1]";

        private const string ProfileTagsSelector = "//div[@class = 'itgroup']";

        private const string ImageSelector = "//div[@class = 'thumbbox']/a";

        private const string ProfileIdSelector = "//input[@id = 'profile-character-id']";

        private readonly IUnityContainer container;

        private readonly IDictionary<string, ProfileData> profileCache = new Dictionary<string, ProfileData>(StringComparer.OrdinalIgnoreCase); 

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

            ProfileData cache;
            if (profileCache.TryGetValue(characterName, out cache))
            {
                model.ProfileData = cache;
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
            try
            {
                string profileBody;
                var profileText = htmlDoc.DocumentNode.SelectNodes(ProfileBodySelector);
                {
                    profileBody = WebUtility.HtmlDecode(profileText[0].InnerHtml);
                    profileBody = profileBody.Replace("<br>", "\n");
                }

                IEnumerable<ProfileTag> profileTags;
                var fullSelection = htmlDoc.DocumentNode.SelectNodes(ProfileTagsSelector);
                {
                    profileTags = fullSelection.SelectMany(selection =>
                        selection.ChildNodes
                            .Where(x => x.Name == "span" || x.Name == "#text")
                            .Select(x => x.InnerText)
                            .ToList()
                            .Chunk(2)
                            .Select(x => x.ToList())
                            .Select(x => new ProfileTag
                            {
                                Label = WebUtility.HtmlDecode(x[0].ToLower().Replace(":", "").Trim()),
                                Value = WebUtility.HtmlDecode(x[1].Trim())
                            }));
                }

                var id = htmlDoc.DocumentNode.SelectSingleNode(ProfileIdSelector).Attributes["value"].Value;

                var imageResp = browser.GetResponse(Constants.UrlConstants.ProfileImages,
                    new Dictionary<string, object> {{"character_id", id}}, true);
                var images = JsonConvert.DeserializeObject<ApiProfileImagesResponse>(imageResp);

                profileCache[characterName] = model.ProfileData = CreateModel(profileBody, profileTags, images);
            }
            catch {}
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

        private static ProfileData CreateModel(string profileText, IEnumerable<ProfileTag> tags, ApiProfileImagesResponse imageResponse)
        {
            var toReturn = new ProfileData
            {
                ProfileText = profileText
            };

            tags.Each(x =>
            {
                switch (x.Label)
                {
                    case "age": 
                        toReturn.Age = x.Value; break;
                    case "species": 
                        toReturn.Species = x.Value; break;
                    case "language preference": 
                        toReturn.LanguagePreference = x.Value; break;
                    case "furry preference": 
                        toReturn.FurryPreference = x.Value; break;
                    case "desired rp length": 
                        toReturn.DesiredRpLength = x.Value; break;
                    case "orientation": 
                        toReturn.Orientation = x.Value; break;
                    case "dom/sub role": 
                        toReturn.DomSubRole = x.Value; break;
                    case "gender": 
                        toReturn.Gender = x.Value; break;
                    case "build": 
                        toReturn.Build = x.Value; break;
                    case "height/length": 
                        toReturn.Height = x.Value; break;
                    case "body type": 
                        toReturn.BodyType = x.Value; break;
                    case "position":
                        toReturn.Position = x.Value; break;
                }
            });

            toReturn.Images = imageResponse.Images.Select(x => new ProfileImage(x)).ToList();

            return toReturn;
        }
    }

    class ProfileTag
    {
        public string Label { get; set; }

        public string Value { get; set; }
    }
}
