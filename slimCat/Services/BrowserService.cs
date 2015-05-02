#region Copyright

// <copyright file="BrowserService.cs">
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
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Cache;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using HtmlAgilityPack;
    using Utilities;

    #endregion

    /// <summary>
    ///     This is a wrapper around a web browser to get pages from the internet. It also handles F-list CSRF.
    /// </summary>
    internal class BrowserService : IBrowseThings
    {
        private const string CsrfTokenSelector = "//meta[@name = 'csrf-token']";
        private const string SiteIsDisabled = "The site has been disabled for maintenance, check back later.";
        private readonly CookieContainer loginCookies = new CookieContainer();
        private string csrfString;

        public string GetResponse(string host, IDictionary<string, object> arguments,
            bool useCookies = false)
        {
            const string contentType = "application/x-www-form-urlencoded";
            const string requestType = "POST";

            Logging.LogLine("POSTing to " + host + " " + arguments.GetHashCode(), "browser serv");

            var isFirst = true;

            if (useCookies)
            {
                if (!arguments.ContainsKey("csrf_token"))
                    arguments["csrf_token"] = GetCsrfToken();
            }

            var totalRequest = new StringBuilder();
            foreach (var arg in arguments.Where(arg => arg.Key != "type"))
            {
                if (!isFirst)
                    totalRequest.Append('&');
                else
                    isFirst = false;

                totalRequest.Append(arg.Key);
                totalRequest.Append('=');
                totalRequest.Append(HttpUtility.UrlEncode((string) arg.Value));
            }

            var toPost = Encoding.ASCII.GetBytes(totalRequest.ToString());

            var cachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            var req = (HttpWebRequest) WebRequest.Create(host);
            req.CachePolicy = cachePolicy;
            req.Method = requestType;
            req.ContentType = contentType;
            req.ContentLength = toPost.Length;
            req.UserAgent = Constants.FriendlyName;

            if (useCookies)
                req.CookieContainer = loginCookies;

            using (var postStream = req.GetRequestStream())
                postStream.Write(toPost, 0, toPost.Length);

            using (var rep = (HttpWebResponse) req.GetResponse())
            using (var answerStream = rep.GetResponseStream())
            {
                if (answerStream == null)
                    return null;
                using (var answerReader = new StreamReader(answerStream))
                    return answerReader.ReadToEnd();
            }
        }

        public string GetResponse(string host, bool useCookies = false)
        {
            var req = (HttpWebRequest) WebRequest.Create(host);
            req.Method = "GET";

            if (useCookies)
                req.CookieContainer = loginCookies;

            req.UserAgent = Constants.FriendlyName;

            using (var rep = (HttpWebResponse) req.GetResponse())
            using (var answerStream = rep.GetResponseStream())
            {
                if (answerStream == null)
                    return null;
                using (var answerReader = new StreamReader(answerStream))
                    return answerReader.ReadToEnd();
            }
        }

        public async Task<string> GetResponseAsync(string host, bool useCookies = false)
        {
            var req = (HttpWebRequest) WebRequest.Create(host);
            req.Method = "GET";

            if (useCookies) req.CookieContainer = loginCookies;

            req.UserAgent = Constants.FriendlyName;

            using (var rep = await req.GetResponseAsync())
            using (var answerStream = rep.GetResponseStream())
            {
                if (answerStream == null)
                    return null;

                using (var answerReader = new StreamReader(answerStream))
                    return await answerReader.ReadToEndAsync();
            }
        }

        private string GetCsrfToken()
        {
            if (!string.IsNullOrWhiteSpace(csrfString)) return csrfString;

            var buffer = GetResponse(Constants.UrlConstants.Domain, true);

            if (buffer.Equals(SiteIsDisabled, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Site API disabled for maintenance.");

            var htmlDoc = new HtmlDocument
            {
                OptionCheckSyntax = false
            };

            HtmlNode.ElementsFlags.Remove("option");
            htmlDoc.LoadHtml(buffer);

            if (htmlDoc.DocumentNode == null)
                throw new Exception("Could not parse login page. Please try again later.");

            var csrfField = htmlDoc.DocumentNode.SelectSingleNode(CsrfTokenSelector);
            csrfString = csrfField.Attributes.First(y => y.Name.Equals("content")).Value;

            return csrfString;
        }
    }
}