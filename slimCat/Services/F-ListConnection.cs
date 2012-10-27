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

namespace Services
{
    /// <summary>
    /// F-list connection is used to authenticate the user's details and then get the API ticket. 
    /// Responds to LoginEvent, fires off LoginCompleteEvent
    /// </summary>
    class ListConnection : IListConnection
    {
        #region Fields
        private const string _flistHost = "http://www.f-list.net";
        private const string _fchatHost = "";
        private IAccount _model;
        private IEventAggregator _event;
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

                this._event.GetEvent<slimCat.LoginEvent>().Subscribe(getTicket, ThreadOption.BackgroundThread);
            }

            catch (Exception ex)
            {
                Exceptions.HandleException(ex);
            }
        }
        #endregion

        #region Methods
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public void getTicket( bool sendUpdate )
        {
            try
            {
                _model.Error = "";
                string ticketUrl = "/json/getApiTicket.php";
                string request = _flistHost + ticketUrl;

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(request);
                // POST data
                byte[] toPost = Encoding.ASCII.GetBytes("account=" + HttpUtility.UrlEncode(_model.AccountName.ToLower())
                    + "&password=" + HttpUtility.UrlEncode(_model.Password));

                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = toPost.Length;
                
                string buffer;

                using (var postStream = req.GetRequestStream())
                    postStream.Write(toPost, 0, toPost.Length);

                using (HttpWebResponse rep = (HttpWebResponse)req.GetResponse())
                    using (var answerStream = rep.GetResponseStream())
                        using (var answerReader = new StreamReader(answerStream))
                            buffer = answerReader.ReadToEnd();

                { // assign the data to our account model
                    dynamic result = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(buffer);
                    buffer = null;

                    bool hasError = (!string.IsNullOrWhiteSpace(result.error as string));

                    _model.Ticket = (string)result.ticket;

                    if (hasError)
                        _model.Error = (string)result.error;

                    else
                    {
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
                    }

                    this._event.GetEvent<slimCat.LoginCompleteEvent>().Publish(!hasError);
                }
            }

            catch (Exception ex)
            {
                _model.Error = "Can't connect to F-List! \nError: " + ex.Message;
                this._event.GetEvent<slimCat.LoginCompleteEvent>().Publish(false);
            }
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
