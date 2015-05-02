#region Copyright

// <copyright file="TicketService.cs">
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
    using System.Diagnostics;
    using System.Linq;
    using HtmlAgilityPack;
    using Models;
    using Models.Api;
    using Utilities;

    #endregion

    internal class TicketService : IGetTickets
    {
        #region Constructors

        public TicketService(IBrowseThings browser)
        {
            this.browser = browser;
        }

        #endregion

        #region Fields

        private const string CsrfTokenSelector = "//input[@name = 'csrf_token']";

        private const string SiteIsDisabled = "The site has been disabled for maintenance, check back later.";

        private readonly IBrowseThings browser;

        private IAccount lastAccount;

        private DateTime lastInfoRetrieval;

        private string lastTicket;

        private Dictionary<string, object> loginCredentials;

        private bool shouldGetNewTicket;

        private Dictionary<string, object> ticketCredentials;

        #endregion

        #region Properties

        public bool ShouldGetNewTicket
        {
            set { shouldGetNewTicket = value; }
            get { return shouldGetNewTicket || lastInfoRetrieval.AddMinutes(4).AddSeconds(30) < DateTime.Now; }
        }

        public string Ticket
        {
            get
            {
                if (ShouldGetNewTicket)
                    GetNewTicket();

                return lastTicket;
            }
        }

        public IAccount Account
        {
            get
            {
                if (lastAccount == null || !lastAccount.Characters.Any())
                    GetNewTicket();

                return lastAccount;
            }
        }

        #endregion

        #region Methods

        public void SetCredentials(string user, string pass)
        {
            if (lastAccount != null
                && lastAccount.AccountName == user
                && lastAccount.Password == pass)
                return;

            ticketCredentials = new Dictionary<string, object>
            {
                {"account", user.ToLower()},
                {"password", pass}
            };

            loginCredentials = new Dictionary<string, object>
            {
                {"username", user.ToLower()},
                {"password", pass}
            };

            lastAccount = new AccountModel
            {
                AccountName = user,
                Password = pass
            };

            ReAuthenticate();
            ShouldGetNewTicket = true;
        }

        private void GetNewTicket()
        {
            Log("Getting new ticket");
            if (lastAccount?.Password == null)
                throw new InvalidOperationException("Set login credentials before logging in!");

            var buffer = browser.GetResponse(Constants.UrlConstants.GetTicket, ticketCredentials);

            if (buffer.Equals(SiteIsDisabled, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Site API disabled for maintenance.");

            // assign the data to our account model
            var result = buffer.DeserializeTo<ApiAuthResponse>();

            var hasError = !string.IsNullOrWhiteSpace(result.Error);

            lastTicket = result.Ticket;

            if (hasError)
                throw new Exception(result.Error);

            foreach (var item in result.Characters.OrderBy(x => x))
                lastAccount.Characters.Add(item);

            foreach (var friend in result.Friends)
            {
                if (lastAccount.AllFriends.ContainsKey(friend.From))
                    lastAccount.AllFriends[friend.From].Add(friend.To);
                else
                {
                    var list = new List<string> {friend.To};

                    lastAccount.AllFriends.Add(friend.From, list);
                }
            }

            foreach (
                var bookmark in result.Bookmarks.Where(bookmark => !lastAccount.Bookmarks.Contains(bookmark.Name)))
                lastAccount.Bookmarks.Add(bookmark.Name);

            lastInfoRetrieval = DateTime.Now;
            ShouldGetNewTicket = false;
            Log("Successfully got a new ticket: " + result.Ticket.Substring(result.Ticket.Length - 6));
        }

        private void ReAuthenticate()
        {
            var buffer = browser.GetResponse(Constants.UrlConstants.Domain);

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
            var csrfToken = csrfField.Attributes.First(y => y.Name.Equals("value")).Value;

            loginCredentials["csrf_token"] = csrfToken;

            browser.GetResponse(Constants.UrlConstants.Login, loginCredentials, true);
        }

        [Conditional("DEBUG")]
        private void Log(string text)
        {
            Logging.LogLine(text, "ticket serv");
        }

        #endregion
    }
}