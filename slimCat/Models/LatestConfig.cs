#region Copyright

// <copyright file="LatestConfig.cs">
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

namespace slimCat.Models
{
    #region Usings

    using System.Collections.Generic;

    #endregion

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
            IsNewUpdate = StringExtensions.IsUpdate(ClientName);
#endif
        }

        public string ClientName { get; }
        public string DownloadLink { get; }
        public string PublishDate { get; private set; }
        public string ChangelogLink { get; private set; }
        public string SlimCatChannelId { get; private set; }
        public bool UpdateImpactsTheme { get; }
        public bool IsNewUpdate { get; }
    }
}