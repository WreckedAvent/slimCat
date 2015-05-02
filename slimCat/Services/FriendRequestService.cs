#region Copyright

// <copyright file="FriendRequestService.cs">
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
    using System.ComponentModel;
    using System.Linq;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Models.Api;
    using SimpleJson;
    using Utilities;

    #endregion

    internal class FriendRequestService : IFriendRequestService
    {
        private readonly IAccount account;

        private readonly IBrowseThings browser;

        private readonly ICharacterManager characterManager;

        private readonly IEventAggregator events;

        private readonly IDictionary<string, int> requestsReceived =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly IDictionary<string, int> requestsSent =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly IGetTickets ticketService;

        private string selectedCharacter;

        public FriendRequestService(IBrowseThings browser, IGetTickets ticketService,
            IAccount account, IEventAggregator events, ICharacterManager characterManager)
        {
            this.browser = browser;
            this.ticketService = ticketService;
            this.account = account;
            this.events = events;
            this.characterManager = characterManager;

            events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(result =>
            {
                selectedCharacter = result;

                UpdateOutgoingRequests();
                UpdatePendingRequests();
            });
        }

        public void UpdatePendingRequests()
        {
            var worker = new BackgroundWorker();

            worker.DoWork += (s, e) =>
            {
                var result = DoApiAction(Constants.UrlConstants.IncomingFriendRequests);

                var relevant = result
                    .Where(x => x.Destination.Equals(selectedCharacter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                requestsReceived.Clear();
                relevant.Each(x => requestsReceived.Add(x.Source, x.Id));
                characterManager.Set(relevant.Select(x => x.Source), ListKind.FriendRequestReceived);
            };

            worker.RunWorkerAsync();
        }

        public void UpdateOutgoingRequests()
        {
            var worker = new BackgroundWorker();

            worker.DoWork += (s, e) =>
            {
                var result = DoApiAction(Constants.UrlConstants.OutgoingFriendRequests);

                var relevant = result
                    .Where(x => x.Source.Equals(selectedCharacter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                requestsSent.Clear();
                relevant.Each(x => requestsSent.Add(x.Destination, x.Id));
                characterManager.Set(relevant.Select(x => x.Destination), ListKind.FriendRequestSent);
            };

            worker.RunWorkerAsync();
        }

        public int? GetRequestForCharacter(string character)
        {
            int requestId;
            if (requestsReceived.TryGetValue(character, out requestId))
            {
                return requestId;
            }

            if (requestsSent.TryGetValue(character, out requestId))
            {
                return requestId;
            }
            return null;
        }

        private IList<ApiFriendRequest> DoApiAction(string endpoint)
        {
            var command = new Dictionary<string, object>
            {
                {"account", account.AccountName.ToLower()},
                {"ticket", ticketService.Ticket}
            };

            var buffer = browser.GetResponse(endpoint, command);

            var result =
                (ApiFriendRequestsResponse) SimpleJson.DeserializeObject(buffer, typeof (ApiFriendRequestsResponse));

            var hasError = !string.IsNullOrWhiteSpace(result.Error);

            if (string.Equals("Ticked expired", result.Error, StringComparison.OrdinalIgnoreCase))
            {
                ticketService.ShouldGetNewTicket = true;
                DoApiAction(endpoint);
            }

            if (hasError)
                events.GetEvent<ErrorEvent>().Publish(result.Error);

            return result.Requests;
        }
    }
}