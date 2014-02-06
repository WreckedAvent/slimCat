namespace slimCat.Services
{
    using System.Collections.Generic;

    public interface IBrowser
    {
        string GetResponse(string host, IEnumerable<KeyValuePair<string, object>> arguments,
            bool useCookies = false);
    }
}
