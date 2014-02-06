namespace slimCat.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Cache;
    using System.Text;
    using System.Web;

    class Browser : IBrowser
    {
        private readonly CookieContainer loginCookies = new CookieContainer();

        public string GetResponse(string host, IEnumerable<KeyValuePair<string, object>> arguments,
            bool useCookies = false)
        {
            const string contentType = "application/x-www-form-urlencoded";
            const string requestType = "POST";

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
                totalRequest.Append(HttpUtility.UrlEncode((string)arg.Value));
            }

            var toPost = Encoding.ASCII.GetBytes(totalRequest.ToString());

            var cachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            var req = (HttpWebRequest)WebRequest.Create(host);
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
