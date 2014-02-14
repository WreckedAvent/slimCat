#region Copyright

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
    using SimpleJson;
    using Utilities;

    #endregion

    internal class TicketProvider : ITicketProvider
    {
        private readonly IBrowser browser;

        private bool hasGottenInfo;

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

            // assign the data to our account model
            dynamic result = SimpleJson.DeserializeObject(buffer);

            var hasError = !string.IsNullOrWhiteSpace((string) result.error);

            lastTicket = (string) result.ticket;

            if (hasError)
                throw new Exception(result.error);

            foreach (var item in result.characters)
                lastAccount.Characters.Add((string) item);

            foreach (var item in result.friends)
            {
                if (lastAccount.AllFriends.ContainsKey(item["source_name"]))
                    lastAccount.AllFriends[item["source_name"]].Add((string) item["dest_name"]);
                else
                {
                    var list = new List<string> {(string) item["dest_name"]};

                    lastAccount.AllFriends.Add(item["source_name"], list);
                }
            }

            foreach (var item in result.bookmarks)
            {
                if (!lastAccount.Bookmarks.Contains(item["name"] as string))
                    lastAccount.Bookmarks.Add(item["name"] as string);
            }

            lastInfoRetrieval = DateTime.Now;
            ShouldGetNewTicket = false;
        }

        private void ReAuthenticate()
        {
            browser.GetResponse(Constants.UrlConstants.Login, loginCredentials, true);
        }
    }
}