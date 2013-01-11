using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.Practices.Prism.Events;
using Models;
using SimpleJson;
using slimCat;

namespace Services
{
    /// <summary>
    /// F-list connection is used to authenticate the user's details and then get the API ticket. 
    /// Responds to LoginEvent, fires off LoginCompleteEvent
    /// </summary>
    class ListConnection : IListConnection
    {
        #region Fields
        private const string FLIST_HOST = "http://www.f-list.net";
        private IAccount _model;
        private IEventAggregator _event;
        private string _selectedCharacter;
        #endregion

        #region Constructors
        public ListConnection(IAccount model, IEventAggregator eventagg)
        {
            try
            {
                if (model == null) throw new ArgumentNullException("model");
                _model = model;

                if (eventagg == null) throw new ArgumentNullException("eventagg");
                _event = eventagg;

                _event.GetEvent<LoginEvent>().Subscribe(getTicket, ThreadOption.BackgroundThread);
                _event.GetEvent<UserCommandEvent>().Subscribe(handleCommand, ThreadOption.BackgroundThread);
                _event.GetEvent<CharacterSelectedLoginEvent>().Subscribe(args => _selectedCharacter = args);
            }

            catch (Exception ex)
            {
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        public void getTicket( bool sendUpdate )
        {
            try
            {
                _model.Error = "";
                string ticketUrl = "/json/getApiTicket.php";
                string host = FLIST_HOST + ticketUrl;

                string buffer = getResponse(host,
                    new Dictionary<string, object>()
                    {
                        {"account", _model.AccountName.ToLower()},
                        {"password", _model.Password},
                    });

                // assign the data to our account model
                dynamic result = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(buffer);
                buffer = null;

                bool hasError = (!string.IsNullOrWhiteSpace((string)result.error));

                _model.Ticket = (string)result.ticket;

                if (hasError)
                {
                    _model.Error = (string)result.error;

                    this._event.GetEvent<slimCat.LoginCompleteEvent>().Publish(!hasError);
                    return;
                }

                foreach (var item in result.characters)
                    _model.Characters.Add((string)item);

                foreach (var item in result.friends)
                {

                    if (_model.AllFriends.ContainsKey(item["source_name"]))
                        _model.AllFriends[item["source_name"]].Add((string)item["dest_name"]);

                    else
                    {
                        var list = new List<string>();
                        list.Add((string)item["dest_name"]);

                        _model.AllFriends.Add(item["source_name"], list);
                    }
                }

                foreach (var item in result.bookmarks)
                    if (!_model.Bookmarks.Contains(item["name"] as string))
                        _model.Bookmarks.Add(item["name"] as string);

                this._event.GetEvent<slimCat.LoginCompleteEvent>().Publish(!hasError);
               
            }

            catch (Exception ex)
            {
                _model.Error = "Can't connect to F-List! \nError: " + ex.Message;
                this._event.GetEvent<slimCat.LoginCompleteEvent>().Publish(false);
            }
        }

        private void handleCommand(IDictionary<string, object> command)
        {
            if (_model == null || _model.Ticket == null) return;
            if (!command.ContainsKey("type")) return;

            var commandType = command["type"] as string;

            switch (commandType)
            {
                case "bookmark-add":
                case "bookmark-remove":
                {
                    doAPIAction(commandType, command);
                    break;
                }
                case "request-send":
                case "friend-remove":
                {
                    command.Add("source_name", _selectedCharacter);
                    doAPIAction(commandType, command);
                    break;
                }

                default: return;
            }
        }

        private void doAPIAction(string apiName, IDictionary<string, object> command)
        {
            const string API_STUB = "json/api/";
            string host = FLIST_HOST + API_STUB + apiName + ".php";

            command.Add("account", _model.AccountName.ToLower());
            command.Add("ticket", _model.Ticket);

            var buffer = getResponse(host, command);
            
            dynamic result = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(buffer);
            buffer = null;

            bool hasError = (!string.IsNullOrWhiteSpace((string)result.error));

            if (hasError)
                _event.GetEvent<ErrorEvent>().Publish((string)result.error);
        }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private string getResponse(string host, IDictionary<string, object> arguments)
        {
            const string CONTENT_TYPE = "application/x-www-form-urlencoded";
            const string REQUEST_TYPE = "POST";

            byte[] toPost;
            var isFirst = true;

            StringBuilder totalRequest = new StringBuilder();
            foreach (var arg in arguments)
            {
                if (arg.Key == "type")
                    continue;

                if (!isFirst)
                    totalRequest.Append('&');
                else
                    isFirst = false;

                totalRequest.Append(arg.Key);
                totalRequest.Append('=');
                totalRequest.Append(HttpUtility.UrlEncode((string)arg.Value));
            }

            toPost = Encoding.ASCII.GetBytes(totalRequest.ToString()); // translate our request string into a byte array

            var req = (HttpWebRequest)WebRequest.Create(host);
            req.Method = REQUEST_TYPE;
            req.ContentType = CONTENT_TYPE;
            req.ContentLength = toPost.Length;

            using (var postStream = req.GetRequestStream())
                postStream.Write(toPost, 0, toPost.Length); // send the request

            using (HttpWebResponse rep = (HttpWebResponse)req.GetResponse()) // get our request
                using (var answerStream = rep.GetResponseStream()) // turn it into a stream
                    using (var answerReader = new StreamReader(answerStream)) // put the stream into a reader
                        return answerReader.ReadToEnd(); // read our response
        }
        #endregion
    }

    /// <summary>
    /// Used for connectivity to F-list
    /// </summary>
    public interface IListConnection
    {
        /// <summary>
        /// Gets an F-list API ticket
        /// </summary>
        void getTicket(bool sendUpdate );
    }
}
