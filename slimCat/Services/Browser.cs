#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Browser.cs">
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

    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Cache;
    using System.Text;
    using System.Web;
    using Utilities;

    #endregion

    internal class Browser : IBrowser
    {
        private readonly CookieContainer loginCookies = new CookieContainer();

        public string GetResponse(string host, IEnumerable<KeyValuePair<string, object>> arguments,
            bool useCookies = false)
        {
            const string contentType = "application/x-www-form-urlencoded";
            const string requestType = "POST";

            Logging.LogLine("POSTing to " + host + " " + arguments.GetHashCode(), "browser serv");

            var isFirst = true;

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
                    return answerReader.ReadToEnd(); // read our response
            }
        }
    }
}