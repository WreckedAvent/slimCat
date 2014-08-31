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
    using System.Web;
    using System.Windows.Media;
    using HtmlAgilityPack;
    using Microsoft.Practices.Prism.Events;
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

        private readonly IChatModel cm;
        private readonly IEventAggregator events;

        private const string ProfileBodySelector = "//div[@id = 'tabs-1']/*[1]";

        private const string ProfileTagsSelector = "//div[@class = 'itgroup']";

        private const string ProfileKinksSeletor = "//td[contains(@class,'Character_Fetishlist')]";

        private const string ProfileIdSelector = "//input[@id = 'profile-character-id']";

        private readonly IUnityContainer container;

        private readonly IDictionary<string, ProfileData> profileCache = new Dictionary<string, ProfileData>(StringComparer.OrdinalIgnoreCase); 

        #endregion

        #region Constructors
        public ProfileService(IUnityContainer contain, IBrowser browser, IChatModel cm, IEventAggregator events)
        {
            this.browser = browser;
            this.cm = cm;
            this.events = events;

            container = contain;

            events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(GetProfileDataAsync);
        }
        #endregion

        #region Public Methods

        private void GetProfileDataAsyncHandler(object s, DoWorkEventArgs e)
        {
            var characterName = (string)e.Argument;
            PmChannelModel model = null;
            try
            {
                model = container.Resolve<PmChannelModel>(characterName);
            }
            catch (ResolutionFailedException)
            {
            }

            ProfileData cache;
            profileCache.TryGetValue(characterName, out cache);
            cache = cache ?? SettingsService.RetrieveProfile(characterName);
            if (cache != null)
            {
                if (cm.CurrentCharacter.NameEquals(characterName))
                    cm.CurrentCharacterData = cache;
                else if (model != null)
                    model.ProfileData = cache;

                profileCache[characterName] = cache;
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
                var profileBody = String.Empty;
                var profileText = htmlDoc.DocumentNode.SelectNodes(ProfileBodySelector);
                if (profileText != null)
                {
                    profileBody = WebUtility.HtmlDecode(profileText[0].InnerHtml);
                    profileBody = profileBody.Replace("<br>", "\n");
                }

                IEnumerable<ProfileTag> profileTags = new List<ProfileTag>();
                var fullSelection = htmlDoc.DocumentNode.SelectNodes(ProfileTagsSelector);
                if (fullSelection != null)
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
                                Label = WebUtility.HtmlDecode(x[0].Replace(":", "").Trim()),
                                Value = WebUtility.HtmlDecode(WebUtility.HtmlDecode(x[1].Trim()))
                            }));
                }

                var allKinks = new List<ProfileKink>();
                var profileKinks = htmlDoc.DocumentNode.SelectNodes(ProfileKinksSeletor);
                if (profileKinks != null)
                {
                    allKinks = profileKinks.SelectMany(selection =>
                    {
                        var kind = (KinkListKind)Enum.Parse(typeof(KinkListKind), selection.Id.Substring("Character_Fetishlist".Length));
                        return selection.Descendants()
                            .Where(x => x.Name == "a")
                            .Select(x =>
                            {
                                var tagId = int.Parse(x.Id.Substring("Character_Listedfetish".Length));
                                var isCustomKink = x.Attributes.First(y => y.Name.Equals("class")).Value.Contains("FetishGroupCustom");
                                var name = x.InnerText.Trim();

                                return new ProfileKink
                                {
                                    Id = tagId,
                                    IsCustomKink = isCustomKink,
                                    Name = HttpUtility.HtmlDecode(name),
                                    KinkListKind = kind
                                };
                            });
                    }).ToList();
                }

                var id = htmlDoc.DocumentNode.SelectSingleNode(ProfileIdSelector).Attributes["value"].Value;

                var imageResp = browser.GetResponse(Constants.UrlConstants.ProfileImages,
                    new Dictionary<string, object> {{"character_id", id}}, true);
                var images = JsonConvert.DeserializeObject<ApiProfileImagesResponse>(imageResp);

                var profileData = CreateModel(profileBody, profileTags, images, allKinks);
                profileCache[characterName] = profileData;

                SettingsService.SaveProfile(characterName, profileData);

                if (model != null)
                    model.ProfileData = profileData;

                if (cm.CurrentCharacter.NameEquals(characterName))
                    cm.CurrentCharacterData = profileData;
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

        private static ProfileData CreateModel(string profileText, IEnumerable<ProfileTag> tags, ApiProfileImagesResponse imageResponse, List<ProfileKink> kinks)
        {
            var toReturn = new ProfileData
            {
                ProfileText = profileText,
                Kinks = kinks
            };

            var tagActions = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
            {
                {"age", s => toReturn.Age = s},
                {"species", s => toReturn.Species = s},
                {"orientation", s => toReturn.Orientation = s},
                {"build", s => toReturn.Build = s},
                {"height/length", s => toReturn.Height = s},
                {"body type", s => toReturn.BodyType = s},
                {"position", s => toReturn.Position = s},
                {"dom/sub role", s => toReturn.DomSubRole = s}
            };

            var profileTags = tags.ToList();
            profileTags.Each(x =>
            {
                Action<string> action;
                if (tagActions.TryGetValue(x.Label, out action))
                    action(x.Value);
            });

            toReturn.AdditionalTags = profileTags.Where(x => !tagActions.ContainsKey(x.Label)).ToList();

            toReturn.Images = imageResponse.Images.Select(x => new ProfileImage(x)).ToList();

            toReturn.LastRetrieved = DateTime.Now;
            return toReturn;
        }
    }


}
