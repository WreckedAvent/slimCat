// --------------------------------------------------------------------------------------------------------------------
// <copyright file="F-ChatConnection.cs" company="Justin Kadrovach">
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
//   Maintains the connection to F-Chat's server. Used to send/receive commands.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Timers;

    using Microsoft.Practices.Prism.Events;

    using Models;

    using SimpleJson;

    using slimCat;

    using WebSocket4Net;

    using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

    /// <summary>
    ///     Maintains the connection to F-Chat's server. Used to send/receive commands.
    /// </summary>
    public class ChatConnection : IChatConnection, IDisposable
    {
        #region Constants

        /// <summary>
        ///     The host.
        /// </summary>
        public const string host = "ws://chat.f-list.net:9722/";

        #endregion

        #region Fields

        private readonly IAccount _account;

        private readonly IEventAggregator _events;

        private string _selectedCharacter;

        private WebSocket _ws;

#if (DEBUG)
        private readonly StreamWriter _logger;
#endif

        private Timer _stagger;

        private bool _isAuth;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the account.
        /// </summary>
        public IAccount Account
        {
            get
            {
                return this._account;
            }
        }

        /// <summary>
        ///     Gets the character.
        /// </summary>
        public string Character
        {
            get
            {
                return this._selectedCharacter;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatConnection"/> class.
        ///     Chat connection is used to communicate with F-Chat using websockets.
        /// </summary>
        /// <param name="user">
        /// The user.
        /// </param>
        /// <param name="eventagg">
        /// The eventagg.
        /// </param>
        public ChatConnection(IAccount user, IEventAggregator eventagg)
        {
            this._account = user.ThrowIfNull("user");
            this._events = eventagg.ThrowIfNull("eventagg");

            this._events.GetEvent<CharacterSelectedLoginEvent>()
                .Subscribe(this.ConnectToChat, ThreadOption.BackgroundThread, true);

#if (DEBUG)
            if (!Directory.Exists(@"Debug"))
            {
                Directory.CreateDirectory("Debug");
            }

            this._logger = new StreamWriter(@"Debug\Rawchat " + DateTime.Now.Ticks + ".log", true);
#endif
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// When the user has picked a character and is ready to connect.
        /// </summary>
        /// <param name="character">
        /// Character to connect with
        /// </param>
        private void ConnectToChat(string character)
        {
            try
            {
                this._selectedCharacter = character.ThrowIfNull("character");

                this._events.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(this.ConnectToChat);

                this._ws = new WebSocket(host);

                // define socket behavior
                this._ws.Opened += this.ConnectionOpened;
                this._ws.Error += this.ConnectionError;
                this._ws.MessageReceived += this.ConnectionMessageReceived;
                this._ws.Closed += this.ConnectionClosed;

                // start connection
                this._ws.Open();
            }
            catch (Exception ex)
            {
                ex.Source = "F-Chat Connection Service, init";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        /// When our connection was closed.
        /// </summary>
        /// <param name="s">
        /// The s.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void ConnectionClosed(object s, EventArgs e)
        {
            if (!this._isAuth)
            {
                this._events.GetEvent<LoginFailedEvent>().Publish("Server closed the connection");
                this.AttemptReconnect();
                return;
            }

            // todo: reconnect 
            Exceptions.HandleException(
                new Exception("Connection to the server was closed"), 
                "The connection to the server was closed.\n\nApplication will now exit.");
#if (DEBUG)
            this._logger.Close();
#endif
        }

        /// <summary>
        /// When we got something from the server!
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void ConnectionMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!this._isAuth)
            {
                this._isAuth = true;
            }

            string command_type = e.Message.Substring(0, 3); // type of command sent

            string message = e.Message; // actual arguments sent

            if (e.Message.Length > 3)
            {
                // if it has arguments...
                message = message.Remove(0, 4); // chop off the command type

                var json = (IDictionary<string, object>)SimpleJson.DeserializeObject(message);

                // de-serialize it to an object model
                json.Add("command", command_type);

                // add back in the command type so our models can listen for them
#if (DEBUG)

                // for debug, write the command received to file
                this._logger.WriteLine("<<- Command: {0}", json["command"]);

                foreach (var pair in json)
                {
                    if (pair.Key != "command")
                    {
                        this._logger.WriteLine("{0}: {1}", pair.Key, pair.Value);
                    }
                }

                this._logger.WriteLine();
                this._logger.Flush();
#endif

                if ((json["command"] as string) == "ERR" && json.ContainsKey("number"))
                {
                    if (json["number"] as string == "2")
                    {
                        // no login spaces error
                        this._isAuth = false;
                    }

                    if (json["number"] as string == "62")
                    {
                        // no login slots error
                        this._isAuth = false;
                    }
                }

                this._events.GetEvent<ChatCommandEvent>().Publish(json);
            }
            else if (e.Message == "PIN")
            {
                this.SendMessage("PIN"); // auto-respond to pings
            }
            else if (e.Message == "LRP")
            {
            }
                
                // useless to us
            
#if (DEBUG)
            else
            {
                // some other, odd, no argument command not specified
                this._logger.WriteLine("Server sent unknown command: " + e.Message);
                this._logger.WriteLine();
                this._logger.Flush();
            }

#endif
        }

        /// <summary>
        /// When something done goofed itself
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void ConnectionError(object sender, ErrorEventArgs e)
        {
            this._events.GetEvent<LoginFailedEvent>().Publish(e.Exception.Message);
            this.AttemptReconnect();
        }

        /// <summary>
        /// When we have connection to F-chat.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void ConnectionOpened(object sender, EventArgs e)
        {
            // Handshake completed, send login command
            object idn =
                new
                    {
                        ticket = this._account.Ticket, 
                        method = "ticket", 
                        account = this._account.AccountName, 
                        character = this._selectedCharacter, 
                        cname = Constants.CLIENT_ID, 
                        cversion = string.Format("{0} {1}", Constants.CLIENT_NAME, Constants.CLIENT_VER)
                    };

            this.SendMessage(idn, "IDN");
        }

        /// <summary>
        ///     If our connection failed, try to reconnect
        /// </summary>
        private void AttemptReconnect()
        {
            if (this._stagger != null)
            {
                this._stagger.Dispose();
                this._stagger = null;
            }

            this._stagger = new Timer((new Random().Next(10) + 5) * 1000); // between 5 and 15 seconds
            this._stagger.Elapsed += (s, e) =>
                {
                    this.ConnectToChat(this._selectedCharacter);
                    this._events.GetEvent<ReconnectingEvent>().Publish(string.Empty);
                };
            this._stagger.Enabled = true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends a message to the server
        /// </summary>
        /// <param name="command">
        /// non-serialized data to be sent
        /// </param>
        /// <param name="command_type">
        /// The command_type.
        /// </param>
        public void SendMessage(object command, string command_type)
        {
            try
            {
                if (command_type.Length > 3 || command_type.Length < 3)
                {
                    throw new ArgumentOutOfRangeException("command_type", "Command type must be 3 characters long");
                }

                string ser = SimpleJson.SerializeObject(command);

#if (DEBUG)

                // debug information
                this._logger.WriteLine("->> Command: " + command_type);
                this._logger.WriteLine("Data: " + ser);
                this._logger.WriteLine();
                this._logger.Flush();
#endif

                this._ws.Send(command_type + " " + ser);
            }
            catch (Exception ex)
            {
                ex.Source = "F-Chat connection, SendMessage method";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        public void SendMessage(IDictionary<string, object> command)
        {
            try
            {
                var type = command["type"] as string;

                command.Remove("type");

                string ser = SimpleJson.SerializeObject(command);

#if (DEBUG)
                this._logger.WriteLine("->> Command: " + type);
                this._logger.WriteLine("Data: " + ser);
                this._logger.WriteLine();
                this._logger.Flush();
#endif

                this._ws.Send(type + " " + ser);
            }
            catch (Exception ex)
            {
                ex.Source = "F-Chat connection, Send Message Method, IDictionary<string, object> overload";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        /// Sends an argument-less command to the server
        /// </summary>
        /// <param name="commandType">
        /// Type of command to send
        /// </param>
        public void SendMessage(string commandType)
        {
            try
            {
                if (commandType.Length > 3 || commandType.Length < 3)
                {
                    throw new ArgumentOutOfRangeException("commandType", "Command type must be 3 characters long");
                }

#if (DEBUG)
                this._logger.WriteLine("->> Command: " + commandType);
                this._logger.WriteLine();
                this._logger.Flush();
#endif

                this._ws.Send(commandType);
            }
            catch (Exception ex)
            {
                ex.Source = "F-Chat connection, SendMessage method";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="isManagedDispose">
        /// The is managed dispose.
        /// </param>
        protected virtual void Dispose(bool isManagedDispose)
        {
#if (DEBUG)
            if (isManagedDispose)
            {
                this._logger.Dispose();
            }

#endif
            this._ws.Close();
        }
    }

    /// <summary>
    ///     The ChatConnection interface.
    /// </summary>
    public interface IChatConnection
    {
        #region Public Properties

        /// <summary>
        ///     Gets the account.
        /// </summary>
        IAccount Account { get; }

        /// <summary>
        ///     Gets the character.
        /// </summary>
        string Character { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        /// <param name="command_type">
        /// The command_type.
        /// </param>
        void SendMessage(object command, string command_type);

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="commandType">
        /// The command type.
        /// </param>
        void SendMessage(string commandType);

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        void SendMessage(IDictionary<string, object> command);

        #endregion
    }
}