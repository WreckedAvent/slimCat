#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModel.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Services
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using Properties;
    using Utilities;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class UpdateService : IUpdateService
    {
        private readonly IBrowser browser;
        private LatestConfig lastConfig;
        private string downloadLocation;
        private bool updateSuccessful;

        public UpdateService(IBrowser browser)
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
                var basePath = Path.GetDirectoryName(Settings.Default.BasePath);

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
                    if (!config.UpdateImpactsTheme && fileDir.EndsWith("theme", StringComparison.OrdinalIgnoreCase)) continue;
                    if (filePath.EndsWith("bootstrapper.exe", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);

                    try { file.ExtractToFile(filePath, true); }
                    catch
                    {
                        //if (fileDir.EndsWith("icons", StringComparison.OrdinalIgnoreCase)) continue;
                        return false;
                    }
                }
            }

            updateSuccessful = true;
            return true;
        }
    }

    public class LatestConfig
    {
        public LatestConfig(IList<string> args)
        {
            ClientName = args[0];
            DownloadLink = args[1];
            PublishDate = args[2];
            ChangelogLink = args[3];
            SlimCatChannelId = args[4];
            UpdateImpactsTheme = bool.Parse(args[5]);

            #if DEBUG
            IsNewUpdate = false;
            #else 
            IsNewUpdate = StaticFunctions.IsUpdate(ClientName);
            #endif
        }

        public string ClientName { get; private set; }
        public string DownloadLink { get; private set; }

        public string PublishDate { get; private set; }

        public string ChangelogLink { get; private set; }

        public string SlimCatChannelId { get; private set; }

        public bool UpdateImpactsTheme { get; private set; }

        public bool IsNewUpdate { get; private set; }
    }
}
