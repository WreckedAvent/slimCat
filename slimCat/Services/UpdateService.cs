#region Copyright

// <copyright file="UpdateService.cs">
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
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Threading.Tasks;
    using Models;
    using Utilities;

    #endregion

    public class UpdateService : IUpdateMyself
    {
        private readonly IBrowseThings browser;
        private string downloadLocation;
        private LatestConfig lastConfig;
        private bool updateSuccessful;

        public UpdateService(IBrowseThings browser)
        {
            this.browser = browser;
        }

        public async Task<LatestConfig> GetLatestAsync()
        {
            if (lastConfig != null) return lastConfig;

            try
            {
                var resp = await browser.GetResponseAsync(Constants.NewVersionUrl);
                if (resp == null) return null;
                var args = resp.Split(',');

                lastConfig = new LatestConfig(args);

                return lastConfig;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> TryUpdateAsync()
        {
#if DEBUG
            return true;
            #endif

            var config = await GetLatestAsync();

            if (config == null) return true;
            if (!config.IsNewUpdate) return true;
            if (updateSuccessful) return true;

            using (var client = new WebClient())
            {
                var tempLocation = downloadLocation;
                var basePath = SettingsService.Preferences.BasePath;

                if (string.IsNullOrWhiteSpace(tempLocation))
                {
                    tempLocation = Path.GetTempFileName().Replace(".tmp", ".zip");
                    await client.DownloadFileTaskAsync(new Uri(config.DownloadLink), tempLocation);
                    downloadLocation = tempLocation;
                }

                using (var zip = ZipFile.OpenRead(tempLocation))
                    foreach (var file in zip.Entries)
                    {
                        var filePath = Path.Combine(basePath, file.FullName);
                        var fileDir = Path.GetDirectoryName(filePath);

                        // don't update theme or bootstrapper
                        if (!config.UpdateImpactsTheme && fileDir.EndsWith("theme", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (filePath.EndsWith("bootstrapper.exe", StringComparison.OrdinalIgnoreCase)) continue;

                        if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);

                        try
                        {
                            file.ExtractToFile(filePath, true);
                        }
                        catch
                        {
                            if (fileDir.EndsWith("icons", StringComparison.OrdinalIgnoreCase)) continue;
                            return false;
                        }
                    }
            }

            updateSuccessful = true;
            return true;
        }
    }
}