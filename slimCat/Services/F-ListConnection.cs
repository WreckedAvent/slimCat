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
    using SimpleJson;
    using Utilities;

    #endregion

    /// <summary>
    ///     F-list connection is used to authenticate the user's details and then get the API ticket.
    ///     Responds to LoginEvent, fires off LoginCompleteEvent
    /// </summary>
    internal class ListConnection : IListConnection
    {
        private readonly IBrowser browser;
        private readonly ITicketProvider ticketProvider;

        #region Fields

        private readonly IEventAggregator events;

        private readonly IAccount model;

        private string selectedCharacter;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ListConnection" /> class.
        /// </summary>
        /// <param name="model">
        ///     The model.
        /// </param>
        /// <param name="eventagg">
        ///     The eventagg.
        /// </param>
        /// <param name="browser"></param>
        /// <param name="ticketProvider"></param>
        public ListConnection(IAccount model, IEventAggregator eventagg, IBrowser browser, ITicketProvider ticketProvider)
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

        /// <summary>
        ///     The upload log.
        /// </summary>
        /// <param name="report">
        ///     The report.
        /// </param>
        /// <param name="log">
        ///     The log.
        /// </param>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        public int UploadLog(ReportModel report, IEnumerable<IMessage> log)
        {
            try
            {
                var logId = -1;

                // log upload format doesn't allow much HTML or anything other than line breaks.
                var sb =
                    new StringBuilder(
                        string.Format(
                            "{0} log upload \nAll times in 24hr {1} \n\n",
                            Constants.FriendlyName,
                            TimeZone.CurrentTimeZone.StandardName));

                var messages =
                    log.Select(
                        m => string.Format("{0} {1}: {2} \n", m.PostedTime.ToTimeStamp(), m.Poster.Name, m.Message));

                var i = 0;
                foreach (var m in messages)
                {
                    sb.Append(m);
                    if (i >= 24)
                    {
                        sb.Append("\n\n"); // legibility
                        i = 0;
                    }

                    i++;
                }

                var toUpload = new Dictionary<string, object>
                    {
                        {"character", report.Reporter.Name},
                        {"log", sb.ToString()},
                        {"reportText", report.Complaint},
                        {"reportUser", report.Reported},
                        {"channel", report.Tab}
                    };

                var buffer = browser.GetResponse(Constants.UrlConstants.UploadLog, toUpload, true);
                dynamic result = SimpleJson.DeserializeObject(buffer);

                if (result.log_id != null)
                {
                    // sometimes log_id appears to be a string
                    int.TryParse(result.log_id, out logId);
                }

                return logId;
            }
            catch (Exception)
            {
                // when dealing with the web it's always possible something could mess up
                return -1;
            }
        }

        /// <summary>
        ///     The get ticket.
        /// </summary>
        /// <param name="sendUpdate">
        ///     The send update.
        /// </param>
        public void GetTicket(bool sendUpdate)
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

        #endregion
    }
}