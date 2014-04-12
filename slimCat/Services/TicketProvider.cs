﻿#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TicketProvider.cs">
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
    using Models;
    using Models.Api;
    using Utilities;

    #endregion

    internal class TicketProvider : ITicketProvider
    {
        private const string siteIsDisabled = "The site has been disabled for maintenance, check back later.";
        private readonly IBrowser browser;

        private IAccount lastAccount;
        private DateTime lastInfoRetrieval;

        private string lastTicket;

        private Dictionary<string, object> loginCredentials;
        private bool shouldGetNewTicket;

        public TicketProvider(IBrowser browser)
        {
            this.browser = browser;
        }

        public bool ShouldGetNewTicket
        {
            set { shouldGetNewTicket = value; }
            get { return shouldGetNewTicket || lastInfoRetrieval.AddMinutes(4).AddSeconds(30) < DateTime.Now; }
        }


        public string Ticket
        {
            get
            {
                if (!ShouldGetNewTicket)
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

        public void SetCredentials(string user, string pass)
        {
            loginCredentials = new Dictionary<string, object>
                {
                    {"account", user.ToLower()},
                    {"password", pass},
                };

            lastAccount = lastAccount ?? new AccountModel();
            lastAccount.AccountName = user;
            lastAccount.Password = pass;

            ReAuthenticate();
        }

        private void GetNewTicket()
        {
            if (lastAccount == null || lastAccount.Password == null)
                throw new InvalidOperationException("Set login credentials before logging in!");

            var buffer = browser.GetResponse(Constants.UrlConstants.GetTicket, loginCredentials);

            if (buffer.Equals(siteIsDisabled, StringComparison.OrdinalIgnoreCase))
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

            foreach (var bookmark in result.Bookmarks.Where(bookmark => !lastAccount.Bookmarks.Contains(bookmark.Name)))
                lastAccount.Bookmarks.Add(bookmark.Name);

            lastInfoRetrieval = DateTime.Now;
            ShouldGetNewTicket = false;
        }

        private void ReAuthenticate()
        {
            browser.GetResponse(Constants.UrlConstants.Login, loginCredentials, true);
        }
    }
}