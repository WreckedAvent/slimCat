// --------------------------------------------------------------------------------------------------------------------
// <copyright file="F-ListConnection.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   F-list connection is used to authenticate the user's details and then get the API ticket.
//   Responds to LoginEvent, fires off LoginCompleteEvent
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;

    using Microsoft.Practices.Prism.Events;

    using SimpleJson;

    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Utilities;

    /// <summary>
    ///     F-list connection is used to authenticate the user's details and then get the API ticket.
    ///     Responds to LoginEvent, fires off LoginCompleteEvent
    /// </summary>
// ReSharper disable ClassNeverInstantiated.Global
    internal class ListConnection : IListConnection
// ReSharper restore ClassNeverInstantiated.Global
    {
        #region Fields

        private readonly IEventAggregator events;

        private readonly CookieContainer loginCookies = new CookieContainer();

        private readonly IAccount model;

        private string selectedCharacter;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ListConnection"/> class.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="eventagg">
        /// The eventagg.
        /// </param>
        public ListConnection(IAccount model, IEventAggregator eventagg)
        {
            try
            {
                this.model = model.ThrowIfNull("model");
                this.events = eventagg.ThrowIfNull("eventagg");

                this.events.GetEvent<LoginEvent>().Subscribe(this.GetTicket, ThreadOption.BackgroundThread);
                this.events.GetEvent<UserCommandEvent>().Subscribe(this.HandleCommand, ThreadOption.BackgroundThread);
                this.events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(args => this.selectedCharacter = args);
            }
            catch (Exception ex)
            {
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The upload log.
        /// </summary>
        /// <param name="report">
        /// The report.
        /// </param>
        /// <param name="log">
        /// The log.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
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
                                       { "character", report.Reporter.Name }, 
                                       { "log", sb.ToString() }, 
                                       { "reportText", report.Complaint }, 
                                       { "reportUser", report.Reported }, 
                                       { "channel", report.Tab }
                                   };

                var buffer = this.GetResponse(Constants.UrlConstants.UploadLog, toUpload, true);
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
        /// The get ticket.
        /// </summary>
        /// <param name="sendUpdate">
        /// The send update.
        /// </param>
        public void GetTicket(bool sendUpdate)
        {
            try
            {
                this.model.Error = string.Empty;
                var loginCredentials = new Dictionary<string, object>
                                           {
                                               { "account", this.model.AccountName.ToLower() }, 
                                               { "password", this.model.Password }, 
                                           };

                var buffer = this.GetResponse(Constants.UrlConstants.GetTicket, loginCredentials);

                // assign the data to our account model
                dynamic result = SimpleJson.DeserializeObject(buffer);

                var hasError = !string.IsNullOrWhiteSpace((string)result.error);

                this.model.Ticket = (string)result.ticket;

                if (hasError)
                {
                    this.model.Error = (string)result.error;

                    this.events.GetEvent<LoginCompleteEvent>().Publish(false);
                    return;
                }

                foreach (var item in result.characters)
                {
                    this.model.Characters.Add((string)item);
                }

                foreach (var item in result.friends)
                {
                    if (this.model.AllFriends.ContainsKey(item["source_name"]))
                    {
                        this.model.AllFriends[item["source_name"]].Add((string)item["dest_name"]);
                    }
                    else
                    {
                        var list = new List<string> { (string)item["dest_name"] };

                        this.model.AllFriends.Add(item["source_name"], list);
                    }
                }

                foreach (var item in result.bookmarks)
                {
                    if (!this.model.Bookmarks.Contains(item["name"] as string))
                    {
                        this.model.Bookmarks.Add(item["name"] as string);
                    }
                }

                this.events.GetEvent<LoginCompleteEvent>().Publish(true);

                loginCredentials = new Dictionary<string, object>
                                       {
                                           {
                                               "username", 
                                               this.model.AccountName.ToLower()
                                           }, 
                                           { "password", this.model.Password }, 
                                       };

                this.GetResponse(Constants.UrlConstants.Login, loginCredentials, true);
            }
            catch (Exception ex)
            {
                this.model.Error = "Can't connect to F-List! \nError: " + ex.Message;
                this.events.GetEvent<LoginCompleteEvent>().Publish(false);
            }
        }

        #endregion

        #region Methods

        private void DoApiAction(string apiName, IDictionary<string, object> command)
        {
            var host = Constants.UrlConstants.Api + apiName + ".php";

            command.Add("account", this.model.AccountName.ToLower());
            command.Add("ticket", this.model.Ticket);

            var buffer = this.GetResponse(host, command);

            dynamic result = SimpleJson.DeserializeObject(buffer);

            var hasError = !string.IsNullOrWhiteSpace((string)result.error);

            if (hasError)
            {
                this.events.GetEvent<ErrorEvent>().Publish((string)result.error);
            }
        }

        private string GetResponse(string host, IEnumerable<KeyValuePair<string, object>> arguments, bool useCookies = false)
        {
            const string ContentType = "application/x-www-form-urlencoded";
            const string RequestType = "POST";

            var isFirst = true;

            var totalRequest = new StringBuilder();
            foreach (var arg in arguments.Where(arg => arg.Key != "type"))
            {
                if (!isFirst)
                {
                    totalRequest.Append('&');
                }
                else
                {
                    isFirst = false;
                }

                totalRequest.Append(arg.Key);
                totalRequest.Append('=');
                totalRequest.Append(HttpUtility.UrlEncode((string)arg.Value));
            }

            var toPost = Encoding.ASCII.GetBytes(totalRequest.ToString());

            var req = (HttpWebRequest)WebRequest.Create(host);
            req.Method = RequestType;
            req.ContentType = ContentType;
            req.ContentLength = toPost.Length;
            if (useCookies)
            {
                req.CookieContainer = this.loginCookies;
            }

            using (var postStream = req.GetRequestStream())
            {
                postStream.Write(toPost, 0, toPost.Length);
            }

            using (var rep = (HttpWebResponse)req.GetResponse())
            {
                using (var answerStream = rep.GetResponseStream()) 
                {
                    using (var answerReader = new StreamReader(answerStream))
                    {
                        return answerReader.ReadToEnd(); // read our response
                    }
                }
            }
        }

        private void HandleCommand(IDictionary<string, object> command)
        {
            if (this.model == null || this.model.Ticket == null)
            {
                return;
            }

            if (!command.ContainsKey("type"))
            {
                return;
            }

            var commandType = command["type"] as string;

            switch (commandType)
            {
                case "bookmark-add":
                case "bookmark-remove":
                    {
                        this.DoApiAction(commandType, command);
                        break;
                    }

                case "request-send":
                case "friend-remove":
                    {
                        command.Add("source_name", this.selectedCharacter);
                        this.DoApiAction(commandType, command);
                        break;
                    }

                default:
                    return;
            }
        }

        #endregion
    }
}