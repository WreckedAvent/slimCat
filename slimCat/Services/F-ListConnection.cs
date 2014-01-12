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

namespace Slimcat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
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
        #region Fields

        private readonly IEventAggregator events;

        private readonly CookieContainer loginCookies = new CookieContainer();

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
        public ListConnection(IAccount model, IEventAggregator eventagg)
        {
            try
            {
                this.model = model.ThrowIfNull("model");
                events = eventagg.ThrowIfNull("eventagg");

                events.GetEvent<LoginEvent>().Subscribe(GetTicket, ThreadOption.BackgroundThread);
                events.GetEvent<UserCommandEvent>().Subscribe(HandleCommand, ThreadOption.BackgroundThread);
                events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(args => selectedCharacter = args);
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
                            "{0} log upload <br/> All times in 24hr {1} <br/><br/>",
                            Constants.FriendlyName,
                            TimeZone.CurrentTimeZone.StandardName));

                var messages =
                    log.Select(
                        m => string.Format("{0} {1}: {2} <br/>", m.PostedTime.ToTimeStamp(), m.Poster.Name, m.Message));

                var i = 0;
                foreach (var m in messages)
                {
                    sb.Append(m);
                    if (i >= 24)
                    {
                        sb.Append("<br/>"); // legibility
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

                var buffer = GetResponse(Constants.UrlConstants.UploadLog, toUpload, true);
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
                var loginCredentials = new Dictionary<string, object>
                    {
                        {"account", model.AccountName.ToLower()},
                        {"password", model.Password},
                    };

                var buffer = GetResponse(Constants.UrlConstants.GetTicket, loginCredentials);

                // assign the data to our account model
                dynamic result = SimpleJson.DeserializeObject(buffer);

                var hasError = !string.IsNullOrWhiteSpace((string) result.error);

                model.Ticket = (string) result.ticket;

                if (hasError)
                {
                    model.Error = (string) result.error;

                    events.GetEvent<LoginCompleteEvent>().Publish(false);
                    return;
                }

                foreach (var item in result.characters)
                    model.Characters.Add((string) item);

                foreach (var item in result.friends)
                {
                    if (model.AllFriends.ContainsKey(item["source_name"]))
                        model.AllFriends[item["source_name"]].Add((string) item["dest_name"]);
                    else
                    {
                        var list = new List<string> {(string) item["dest_name"]};

                        model.AllFriends.Add(item["source_name"], list);
                    }
                }

                foreach (var item in result.bookmarks)
                {
                    if (!model.Bookmarks.Contains(item["name"] as string))
                        model.Bookmarks.Add(item["name"] as string);
                }

                events.GetEvent<LoginCompleteEvent>().Publish(true);

                loginCredentials = new Dictionary<string, object>
                    {
                        {
                            "username",
                            model.AccountName.ToLower()
                        },
                        {"password", model.Password},
                    };

                GetResponse(Constants.UrlConstants.Login, loginCredentials, true);
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

            var buffer = GetResponse(host, command);

            dynamic result = SimpleJson.DeserializeObject(buffer);

            var hasError = !string.IsNullOrWhiteSpace((string) result.error);

            if (hasError)
                events.GetEvent<ErrorEvent>().Publish((string) result.error);
        }

        private string GetResponse(string host, IEnumerable<KeyValuePair<string, object>> arguments,
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
                totalRequest.Append(HttpUtility.UrlEncode((string) arg.Value));
            }

            var toPost = Encoding.ASCII.GetBytes(totalRequest.ToString());

            var req = (HttpWebRequest) WebRequest.Create(host);
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