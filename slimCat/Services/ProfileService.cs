#region Copyright

// <copyright file="ProfileService.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using HtmlAgilityPack;
    using Microsoft.Practices.Unity;
    using Models;
    using Models.Api;
    using Newtonsoft.Json;
    using Utilities;

    #endregion

    public class ProfileService : IGetProfiles
    {
        #region Constructors

        public ProfileService(IChatState chatState, IHandleApi api, IBrowseThings browser)
        {
            cm = chatState.ChatModel;
            state = chatState;
            this.api = api;
            this.browser = browser;

            chatState.EventAggregator.GetEvent<LoginAuthenticatedEvent>().Subscribe(GetProfileDataAsync);

            var worker = new BackgroundWorker();
            worker.DoWork += GetKinkDataAsync;
            worker.RunWorkerAsync();
        }

        #endregion

        public void GetProfileDataAsync(string character)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += GetProfileDataAsyncHandler;
            worker.RunWorkerAsync(character);
        }

        public void ClearCache(string character)
        {
            invalidCacheList.Add(character);
            GetProfileDataAsync(character);
        }

        public void GetProfileDataAsync(bool? success)
        {
            var character = cm.CurrentCharacter.Name;
            var worker = new BackgroundWorker();
            worker.DoWork += GetProfileDataAsyncHandler;
            worker.RunWorkerAsync(character);
        }

        #region Fields

        private readonly IChatModel cm;

        private readonly IDictionary<int, ProfileKink> kinkData = new Dictionary<int, ProfileKink>();

        private readonly IDictionary<string, ProfileData> profileCache =
            new Dictionary<string, ProfileData>(StringComparer.OrdinalIgnoreCase);

        private readonly IList<string> invalidCacheList = new List<string>();

        private readonly IChatState state;
        private readonly IHandleApi api;
        private readonly IBrowseThings browser;

        #endregion

        #region Public Methods

        private void GetProfileDataAsyncHandler(object s, DoWorkEventArgs e)
        {
            var characterName = (string) e.Argument;
            PmChannelModel model = null;
            try
            {
                model = state.Resolve<PmChannelModel>(characterName);
            }
            catch (ResolutionFailedException)
            {
            }

            if (!invalidCacheList.Contains(characterName))
            {
                ProfileData cache;
                profileCache.TryGetValue(characterName, out cache);
                cache = cache ?? SettingsService.RetrieveProfile(characterName);
                if (cache != null)
                {
                    //if (!profileCache.ContainsKey(characterName))
                    //    cache.Kinks = cache.Kinks.Select(GetFullKink).ToList();

                    if (cm.CurrentCharacter.NameEquals(characterName))
                        cm.CurrentCharacterData = cache;
                    if (model != null)
                        model.ProfileData = cache;

                    profileCache[characterName] = cache;
                    return;
                }
            }
            else
            {
                invalidCacheList.Remove(characterName);
            }

            var resp = api.DoApiAction<ApiProfileResponse>(
                "character-data",
                new Dictionary<string, object>
                {
                    {"name", characterName},
                });

            var profileData = new ProfileData
            {
                ProfileText = resp.Description,
                Images = resp.Images.Select(x => new ProfileImage(x)).ToList(),
            };

            SettingsService.SaveProfile(characterName, profileData);

            profileCache[characterName] = profileData;

            if (model != null)
                model.ProfileData = profileData;

            if (cm.CurrentCharacter.NameEquals(characterName))
                cm.CurrentCharacterData = profileData;
        }

        private void GetKinkDataAsync(object s, DoWorkEventArgs e)
        {
            var kinkDataCache = SettingsService.RetrieveProfile("!kinkdata");
            if (kinkDataCache == null)
            {
                var response = browser.GetResponse(Constants.UrlConstants.MappingList);

                var data = JsonConvert.DeserializeObject<ApiMappingResponse>(response);

                //var apiKinks = data.Kinks
                //    .SelectMany(x => x.Value.Kinks)
                //    .Select(x => new ProfileKink
                //    {
                //        Id = x.Id,
                //        IsCustomKink = false,
                //        Name = x.Name.DoubleDecode(),
                //        Tooltip = x.Description.DoubleDecode(),
                //        KinkListKind = KinkListKind.MasterList
                //    }).ToList();

                //kinkDataCache = new ProfileData
                //{
                //    Kinks = apiKinks
                //};
                //SettingsService.SaveProfile("!kinkdata", kinkDataCache);
            }

            kinkData.Clear();
            kinkDataCache.Kinks.Each(x => kinkData.Add(x.Id, x));
        }

        [Conditional("DEBUG")]
        private static void Log(string text) => Logging.LogLine(text, "profile serv");

        #endregion
    }
}