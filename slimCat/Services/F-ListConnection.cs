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

namespace Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;

    using Microsoft.Practices.Prism.Events;

    using Models;

    using SimpleJson;

    using slimCat;

    /// <summary>
    ///     F-list connection is used to authenticate the user's details and then get the API ticket.
    ///     Responds to LoginEvent, fires off LoginCompleteEvent
    /// </summary>
    internal class ListConnection : IListConnection
    {
        #region Fields

        private readonly IEventAggregator _event;

        private readonly CookieContainer _loginCookies = new CookieContainer();

        private readonly IAccount _model;

        private string _selectedCharacter;

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
                this._model = model.ThrowIfNull("model");
                this._event = eventagg.ThrowIfNull("eventagg");

                this._event.GetEvent<LoginEvent>().Subscribe(this.getTicket, ThreadOption.BackgroundThread);
                this._event.GetEvent<UserCommandEvent>().Subscribe(this.handleCommand, ThreadOption.BackgroundThread);
                this._event.GetEvent<CharacterSelectedLoginEvent>().Subscribe(args => this._selectedCharacter = args);
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
                int logId = -1;

                // log upload format doesn't allow much HTML or anything other than line breaks.
                var sb =
                    new StringBuilder(
                        string.Format(
                            "{0} log upload <br/> All times in 24hr {1} <br/><br/>", 
                            Constants.FRIENDLY_NAME, 
                            TimeZone.CurrentTimeZone.StandardName));

                IEnumerable<string> messages =
                    log.Select(
                        m => string.Format("{0} {1}: {2} <br/>", m.PostedTime.ToTimeStamp(), m.Poster.Name, m.Message));

                int i = 0;
                foreach (string m in messages)
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

                string buffer = this.getResponse(Constants.UrlConstants.UPLOAD_LOG, toUpload, true);
                dynamic result = SimpleJson.DeserializeObject(buffer);
                buffer = null;

                if (result.log_id != null)
                {
                    // sometimes log_id appears to be a string
                    int.TryParse(result.log_id, out logId);
                }

                return logId;
            }
            catch (Exception ex)
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
        public void getTicket(bool sendUpdate)
        {
            try
            {
                this._model.Error = string.Empty;
                var loginCredentials = new Dictionary<string, object>
                                           {
                                               { "account", this._model.AccountName.ToLower() }, 
                                               { "password", this._model.Password }, 
                                           };

                string buffer = this.getResponse(Constants.UrlConstants.GET_TICKET, loginCredentials);

                // assign the data to our account model
                dynamic result = SimpleJson.DeserializeObject(buffer);
                buffer = null;

                bool hasError = !string.IsNullOrWhiteSpace((string)result.error);

                this._model.Ticket = (string)result.ticket;

                if (hasError)
                {
                    this._model.Error = (string)result.error;

                    this._event.GetEvent<LoginCompleteEvent>().Publish(!hasError);
                    return;
                }

                foreach (dynamic item in result.characters)
                {
                    this._model.Characters.Add((string)item);
                }

                foreach (dynamic item in result.friends)
                {
                    if (this._model.AllFriends.ContainsKey(item["source_name"]))
                    {
                        this._model.AllFriends[item["source_name"]].Add((string)item["dest_name"]);
                    }
                    else
                    {
                        var list = new List<string>();
                        list.Add((string)item["dest_name"]);

                        this._model.AllFriends.Add(item["source_name"], list);
                    }
                }

                foreach (dynamic item in result.bookmarks)
                {
                    if (!this._model.Bookmarks.Contains(item["name"] as string))
                    {
                        this._model.Bookmarks.Add(item["name"] as string);
                    }
                }

                this._event.GetEvent<LoginCompleteEvent>().Publish(!hasError);

                // login to F-list for our cookies, so that we can post logs
                if (!hasError)
                {
                    loginCredentials = new Dictionary<string, object>
                                           {
                                               {
                                                   "username", 
                                                   this._model.AccountName.ToLower()
                                               }, 
                                               { "password", this._model.Password }, 
                                           };

                    this.getResponse(Constants.UrlConstants.LOGIN, loginCredentials, true);
                }
            }
            catch (Exception ex)
            {
                this._model.Error = "Can't connect to F-List! \nError: " + ex.Message;
                this._event.GetEvent<LoginCompleteEvent>().Publish(false);
            }
        }

        #endregion

        #region Methods

        private void doAPIAction(string apiName, IDictionary<string, object> command)
        {
            string host = Constants.UrlConstants.API + apiName + ".php";

            command.Add("account", this._model.AccountName.ToLower());
            command.Add("ticket", this._model.Ticket);

            string buffer = this.getResponse(host, command);

            dynamic result = SimpleJson.DeserializeObject(buffer);
            buffer = null;

            bool hasError = !string.IsNullOrWhiteSpace((string)result.error);

            if (hasError)
            {
                this._event.GetEvent<ErrorEvent>().Publish((string)result.error);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private string getResponse(string host, IDictionary<string, object> arguments, bool useCookies = false)
        {
            const string CONTENT_TYPE = "application/x-www-form-urlencoded";
            const string REQUEST_TYPE = "POST";

            byte[] toPost;
            bool isFirst = true;

            var totalRequest = new StringBuilder();
            foreach (var arg in arguments)
            {
                if (arg.Key == "type")
                {
                    continue;
                }

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

            toPost = Encoding.ASCII.GetBytes(totalRequest.ToString()); // translate our request string into a byte array

            var req = (HttpWebRequest)WebRequest.Create(host);
            req.Method = REQUEST_TYPE;
            req.ContentType = CONTENT_TYPE;
            req.ContentLength = toPost.Length;
            if (useCookies)
            {
                req.CookieContainer = this._loginCookies;
            }

            using (Stream postStream = req.GetRequestStream()) postStream.Write(toPost, 0, toPost.Length); // send the request

            using (var rep = (HttpWebResponse)req.GetResponse()) // get our request
            using (Stream answerStream = rep.GetResponseStream()) // turn it into a stream
            using (var answerReader = new StreamReader(answerStream)) // put the stream into a reader
                return answerReader.ReadToEnd(); // read our response
        }

        private void handleCommand(IDictionary<string, object> command)
        {
            if (this._model == null || this._model.Ticket == null)
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
                        this.doAPIAction(commandType, command);
                        break;
                    }

                case "request-send":
                case "friend-remove":
                    {
                        command.Add("source_name", this._selectedCharacter);
                        this.doAPIAction(commandType, command);
                        break;
                    }

                default:
                    return;
            }
        }

        #endregion
    }

    /// <summary>
    ///     Used for connectivity to F-list
    /// </summary>
    public interface IListConnection
    {
        #region Public Methods and Operators

        /// <summary>
        /// Uploads a lot to F-list.net f.e reporting a user
        /// </summary>
        /// <param name="report">
        /// relevant data about the report
        /// </param>
        /// <param name="log">
        /// the log to upload
        /// </param>
        /// <returns>
        /// an int corresonding to the logid the server assigned
        /// </returns>
        int UploadLog(ReportModel report, IEnumerable<IMessage> log);

        /// <summary>
        /// Gets an F-list API ticket
        /// </summary>
        /// <param name="sendUpdate">
        /// The send Update.
        /// </param>
        void getTicket(bool sendUpdate);

        #endregion
    }
}