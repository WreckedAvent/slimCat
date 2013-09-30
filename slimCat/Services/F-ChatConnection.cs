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

namespace Slimcat.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Timers;

    using Microsoft.Practices.Prism.Events;

    using SimpleJson;

    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Utilities;

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
        private const string Host = "ws://chat.f-list.net:9722/";

        #endregion

        #region Fields

        private readonly IEventAggregator events;

#if (DEBUG)
        private readonly StreamWriter logger;
#endif
        private WebSocket socket;

        private Timer staggerTimer;

        private bool isAuthenticated;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the account.
        /// </summary>
        public IAccount Account { get; private set; }

        /// <summary>
        ///     Gets the character.
        /// </summary>
        public string Character { get; private set; }

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
            this.Account = user.ThrowIfNull("user");
            this.events = eventagg.ThrowIfNull("eventagg");

            this.events.GetEvent<CharacterSelectedLoginEvent>()
                .Subscribe(this.ConnectToChat, ThreadOption.BackgroundThread, true);

            #if (DEBUG)
            if (!Directory.Exists(@"Debug"))
            {
                Directory.CreateDirectory("Debug");
            }

            this.logger = new StreamWriter(@"Debug\Rawchat " + DateTime.Now.Ticks + ".log", true);
            #endif
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends a message to the server
        /// </summary>
        /// <param name="command">
        /// non-serialized data to be sent
        /// </param>
        /// <param name="commandType">
        /// The command_type.
        /// </param>
        public void SendMessage(object command, string commandType)
        {
            try
            {
                if (commandType.Length > 3 || commandType.Length < 3)
                {
                    throw new ArgumentOutOfRangeException("commandType", "Command type must be 3 characters long");
                }

                var ser = SimpleJson.SerializeObject(command);

#if (DEBUG)

                // debug information
                this.logger.WriteLine("->> Command: " + commandType);
                this.logger.WriteLine("Data: " + ser);
                this.logger.WriteLine();
                this.logger.Flush();
#endif

                this.socket.Send(commandType + " " + ser);
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

                var ser = SimpleJson.SerializeObject(command);

#if (DEBUG)
                this.logger.WriteLine("->> Command: " + type);
                this.logger.WriteLine("Data: " + ser);
                this.logger.WriteLine();
                this.logger.Flush();
#endif

                this.socket.Send(type + " " + ser);
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
                this.logger.WriteLine("->> Command: " + commandType);
                this.logger.WriteLine();
                this.logger.Flush();
#endif

                this.socket.Send(commandType);
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
                this.logger.Dispose();
            }

            #endif
            this.socket.Close();
        }

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
                this.Character = character.ThrowIfNull("character");

                this.events.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(this.ConnectToChat);

                this.socket = new WebSocket(Host);

                // define socket behavior
                this.socket.Opened += this.ConnectionOpened;
                this.socket.Error += this.ConnectionError;
                this.socket.MessageReceived += this.ConnectionMessageReceived;
                this.socket.Closed += this.ConnectionClosed;

                // start connection
                this.socket.Open();
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
            if (!this.isAuthenticated)
            {
                this.events.GetEvent<LoginFailedEvent>().Publish("Server closed the connection");
                this.AttemptReconnect();
                return;
            }

            // todo: reconnect 
            Exceptions.HandleException(
                new Exception("Connection to the server was closed"),
                "The connection to the server was closed.\n\nApplication will now exit.");

#if (DEBUG)
            this.logger.Close();
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
            if (!this.isAuthenticated)
            {
                this.isAuthenticated = true;
            }

            var commandType = e.Message.Substring(0, 3); // type of command sent

            var message = e.Message; // actual arguments sent

            if (e.Message.Length > 3)
            {
                // if it has arguments...
                message = message.Remove(0, 4); // chop off the command type

                var json = (IDictionary<string, object>)SimpleJson.DeserializeObject(message);

                // de-serialize it to an object model
                json.Add("command", commandType);

                // add back in the command type so our models can listen for them
#if (DEBUG)
                // for debug, write the command received to file
                this.logger.WriteLine("<<- Command: {0}", json["command"]);

                foreach (var pair in json.Where(pair => pair.Key != "command"))
                {
                    this.logger.WriteLine("{0}: {1}", pair.Key, pair.Value);
                }

                this.logger.WriteLine();
                this.logger.Flush();
#endif

                if ((json["command"] as string) == "ERR" && json.ContainsKey("number"))
                {
                    if (json["number"] as string == "2")
                    {
                        // no login spaces error
                        this.isAuthenticated = false;
                    }

                    if (json["number"] as string == "62")
                    {
                        // no login slots error
                        this.isAuthenticated = false;
                    }
                }

                this.events.GetEvent<ChatCommandEvent>().Publish(json);
            }
            else
            {
                switch (e.Message)
                {
                    case "PIN":
                        this.SendMessage("PIN"); // auto-respond to pings
                        break;
                    case "LRP":
                        break;
                }
            }
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
            this.events.GetEvent<LoginFailedEvent>().Publish(e.Exception.Message);
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
                    ticket = this.Account.Ticket,
                    method = "ticket",
                    account = this.Account.AccountName,
                    character = this.Character,
                    cname = Constants.ClientID,
                    cversion = string.Format("{0} {1}", Constants.ClientName, Constants.ClientVer)
                };

            this.SendMessage(idn, "IDN");
        }

        /// <summary>
        ///     If our connection failed, try to reconnect
        /// </summary>
        private void AttemptReconnect()
        {
            if (this.staggerTimer != null)
            {
                this.staggerTimer.Dispose();
                this.staggerTimer = null;
            }

            this.staggerTimer = new Timer((new Random().Next(10) + 5) * 1000); // between 5 and 15 seconds
            this.staggerTimer.Elapsed += (s, e) =>
            {
                this.ConnectToChat(this.Character);
                this.events.GetEvent<ReconnectingEvent>().Publish(string.Empty);
            };
            this.staggerTimer.Enabled = true;
        }

        #endregion
    }
}