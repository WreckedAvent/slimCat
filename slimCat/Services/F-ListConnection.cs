#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="F-ListConnection.cs">
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
    using System.Net;
    using System.Text;
    using Microsoft.Practices.Prism;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using Models.Api;
    using SimpleJson;
    using Utilities;

    #endregion

    /// <summary>
    ///     F-list connection is used to authenticate the user's details and then get the API ticket.
    ///     Responds to LoginEvent, fires off LoginCompleteEvent
    /// </summary>
    internal class FlistService : IListConnection
    {

        #region Fields

        private readonly IEventAggregator events;

        private readonly IAccount model;

        private string selectedCharacter;

        private readonly IBrowser browser;

        private readonly ITicketProvider ticketProvider;

        #endregion

        #region Constructors and Destructors

        public FlistService(IAccount model, IEventAggregator eventagg, IBrowser browser,
            ITicketProvider ticketProvider)
        {
            this.browser = browser;
            this.ticketProvider = ticketProvider;

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
                    .Select(m => string.Format("{0} {1}: {2} \n", m.PostedTime.ToUniversalTime().ToTimeStamp(), m.Poster.Name, m.Message))
                    .Each(m => sb.Append(m));

                var toUpload = new Dictionary<string, object>
                    {
                        {"character", report.Reporter.Name},
                        {"log", sb.ToString()},
                        {"reportText", report.Complaint},
                        {"reportUser", report.Reported},
                        {"channel", report.Tab}
                    };

                var buffer = browser.GetResponse(Constants.UrlConstants.UploadLog, toUpload, true);
                var result = buffer.DeserializeTo<ApiUploadLogResponse>();

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
                ticketProvider.SetCredentials(model.AccountName, model.Password);

                var acc = ticketProvider.Account;

                model.Characters.AddRange(acc.Characters);
                acc.AllFriends.Each(model.AllFriends.Add);
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
            command.Add("ticket", model.Ticket);

            var buffer = browser.GetResponse(host, command);

            dynamic result = SimpleJson.DeserializeObject(buffer);

            var hasError = !string.IsNullOrWhiteSpace((string) result.error);

            if (hasError)
                events.GetEvent<ErrorEvent>().Publish((string) result.error);
        }

        private void HandleCommand(IDictionary<string, object> command)
        {
            if (model == null || model.Ticket == null)
                return;

            if (!command.ContainsKey("type"))
                return;

            var commandType = command["type"] as string;

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
                    DoApiAction(commandType, command);
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