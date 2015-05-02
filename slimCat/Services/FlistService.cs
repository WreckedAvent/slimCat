#region Copyright

// <copyright file="FlistService.cs">
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
    using System.Linq;
    using System.Net;
    using System.Text;
    using Microsoft.Practices.Prism;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Models.Api;
    using SimpleJson;
    using Utilities;

    #endregion

    internal class FlistService : IHandleApi
    {
        #region Constructors and Destructors

        public FlistService(IAccount model, IEventAggregator eventagg, IBrowseThings browser,
            IGetTickets ticketService, IFriendRequestService requestService)
        {
            this.browser = browser;
            this.ticketService = ticketService;
            this.requestService = requestService;

            try
            {
                this.model = model.ThrowIfNull("model");
                events = eventagg.ThrowIfNull("eventagg");

                events.GetEvent<LoginEvent>().Subscribe(GetTicket, ThreadOption.BackgroundThread);
                events.GetEvent<UserCommandEvent>().Subscribe(HandleCommand, ThreadOption.BackgroundThread);
                events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(args => selectedCharacter = args);

                // fix problem with SSL not being trusted on some machines
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;
            }
            catch (Exception ex)
            {
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Fields

        private readonly IBrowseThings browser;

        private readonly IEventAggregator events;

        private readonly IAccount model;

        private readonly IFriendRequestService requestService;

        private readonly IGetTickets ticketService;

        private string selectedCharacter;

        #endregion

        #region Public Methods and Operators

        public int UploadLog(ReportModel report, IEnumerable<IMessage> log)
        {
            try
            {
                var logId = -1;

                // log upload format doesn't allow much HTML or anything other than line breaks.
                var sb = new StringBuilder();
                sb.Append("==================================\n");
                sb.Append("{0} log upload \n{1} in {2}\nreported user: {3}\ntime stamps in 24hr UTC\n"
                    .FormatWith(Constants.FriendlyName,
                        DateTime.UtcNow.ToShortDateString(),
                        report.Tab,
                        report.Reported));
                sb.Append("==================================\n");

                log.Where(x => !x.IsHistoryMessage)
                    .Select(m => $"{m.PostedTime.ToUniversalTime().ToTimeStamp()} {m.Poster.Name}: {m.Message} \n")
                    .Each(m => sb.Append(m));

                var toUpload = new Dictionary<string, object>
                {
                    {"account", model.AccountName.ToLower()},
                    {"ticket", ticketService.Ticket},
                    {"character", report.Reporter.Name},
                    {"log", sb.ToString()},
                    {"reportText", report.Complaint},
                    {"reportUser", report.Reported},
                    {"channel", report.Tab}
                };

                var buffer = browser.GetResponse(Constants.UrlConstants.UploadLog, toUpload, true);
                var result = buffer.DeserializeTo<ApiUploadLogResponse>();

                var hasError = !string.IsNullOrWhiteSpace(result.Error);

                if (hasError)
                {
                    ticketService.ShouldGetNewTicket = true;
                    UploadLog(report, log);
                }

                if (result.LogId != null)
                {
                    int.TryParse(result.LogId, out logId);
                }

                Log("Uploaded report log in tab {0} with id of {1}".FormatWith(report.Tab, logId));
                return logId;
            }
            catch (Exception)
            {
                // when dealing with the web it's always possible something could mess up
                Log("Failed to get id for report log in tab {0}".FormatWith(report.Tab));
                return -1;
            }
        }

        public void GetTicket(bool _)
        {
            try
            {
                model.Error = string.Empty;
                ticketService.SetCredentials(model.AccountName, model.Password);

                var acc = ticketService.Account;

                model.Characters.Clear();
                model.Characters.AddRange(acc.Characters);

                model.AllFriends.Clear();
                acc.AllFriends.Each(model.AllFriends.Add);

                model.Bookmarks.Clear();
                acc.Bookmarks.Each(model.Bookmarks.Add);

                events.GetEvent<LoginCompleteEvent>().Publish(true);
            }
            catch (Exception ex)
            {
                Log("Could not get ticket: {0}".FormatWith(ex.Message));
                model.Error = "Can't connect to F-List! \nError: " + ex.Message;
                events.GetEvent<LoginCompleteEvent>().Publish(false);
            }
        }

        #endregion

        #region Methods

        private void DoApiAction(string apiName, IDictionary<string, object> command)
        {
            var host = Constants.UrlConstants.Api + apiName + ".php";

            command.Add("account", model.AccountName.ToLower());
            command.Add("ticket", ticketService.Ticket);

            var buffer = browser.GetResponse(host, command);

            dynamic result = SimpleJson.DeserializeObject(buffer);

            var hasError = !string.IsNullOrWhiteSpace((string) result.error);

            if (string.Equals("Ticked expired", result.error, StringComparison.OrdinalIgnoreCase))
            {
                ticketService.ShouldGetNewTicket = true;
                DoApiAction(apiName, command);
            }

            if (hasError)
                events.GetEvent<ErrorEvent>().Publish((string) result.error);
        }

        private void HandleCommand(IDictionary<string, object> command)
        {
            if (model == null)
                return;

            if (!command.ContainsKey("type"))
                return;

            var commandType = command["type"] as string;

            command = new Dictionary<string, object>(command);

            switch (commandType)
            {
                case "bookmark-add":
                case "bookmark-remove":
                {
                    DoApiAction(commandType, command);
                    break;
                }

                case "request-send":
                case "friend-remove":
                {
                    command.Add("source_name", selectedCharacter);
                    command["dest_name"] = command[Constants.Arguments.Character];
                    command.Remove(Constants.Arguments.Character);

                    DoApiAction(commandType, command);
                    if (commandType == "request-send")
                        requestService.UpdateOutgoingRequests();
                    break;
                }

                case "request-accept":
                case "request-cancel":
                case "request-deny":
                {
                    var character = command.Get(Constants.Arguments.Character);
                    command.Remove(Constants.Arguments.Character);

                    var id = requestService.GetRequestForCharacter(character);
                    if (id == null)
                    {
                        events.GetEvent<ErrorEvent>()
                            .Publish("Could not find any friend requests for/from {0}".FormatWith(character));
                        return;
                    }

                    command.Add("request_id", id.ToString());
                    DoApiAction(commandType, command);

                    if (commandType == "request-deny" || commandType == "request-accept")
                        requestService.UpdatePendingRequests();
                    else
                        requestService.UpdateOutgoingRequests();
                    break;
                }

                default:
                    return;
            }
        }

        private void Log(string text)
        {
            Logging.Log(text, "site");
        }

        #endregion
    }
}