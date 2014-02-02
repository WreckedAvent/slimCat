#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="F-ChatConnection.cs">
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
    // used by debug build
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Timers;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using SimpleJson;
    using Utilities;
    using WebSocket4Net;
    using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

    #endregion

    /// <summary>
    ///     Maintains the connection to F-Chat's server. Used to send/receive commands.
    /// </summary>
    public class ChatConnection : IChatConnection, IDisposable
    {
        #region Constants

        /// <summary>
        ///     The host.
        /// </summary>
        private const string Host = "wss://chat.f-list.net:9799/";

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

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChatConnection" /> class.
        ///     Chat connection is used to communicate with F-Chat using websockets.
        /// </summary>
        /// <param name="user">
        ///     The user.
        /// </param>
        /// <param name="eventagg">
        ///     The eventagg.
        /// </param>
        public ChatConnection(IAccount user, IEventAggregator eventagg)
        {
            Account = user.ThrowIfNull("user");
            events = eventagg.ThrowIfNull("eventagg");

            events.GetEvent<CharacterSelectedLoginEvent>()
                .Subscribe(ConnectToChat, ThreadOption.BackgroundThread, true);

#if (DEBUG)
            if (!Directory.Exists(@"Debug"))
                Directory.CreateDirectory("Debug");

            logger = new StreamWriter(@"Debug\Rawchat " + DateTime.Now.Ticks + ".log", true);
#endif
        }

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

        #region Methods

        /// <summary>
        ///     Sends a message to the server
        /// </summary>
        /// <param name="command">
        ///     non-serialized data to be sent
        /// </param>
        /// <param name="commandType">
        ///     The command_type.
        /// </param>
        public void SendMessage(object command, string commandType)
        {
            try
            {
                if (commandType.Length > 3 || commandType.Length < 3)
                    throw new ArgumentOutOfRangeException("commandType", "Command type must be 3 characters long");

                var ser = SimpleJson.SerializeObject(command);

#if (DEBUG)

    // debug information
                logger.WriteLine("->> Command: " + commandType);
                logger.WriteLine("Data: " + ser);
                logger.WriteLine();
                logger.Flush();
#endif

                socket.Send(commandType + " " + ser);
            }
            catch (Exception ex)
            {
                ex.Source = "F-Chat connection, SendMessage method";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        ///     The send message.
        /// </summary>
        /// <param name="command">
        ///     The command.
        /// </param>
        public void SendMessage(IDictionary<string, object> command)
        {
            try
            {
                var type = command.Get(Constants.Arguments.Type);

                command.Remove(Constants.Arguments.Type);

                var ser = SimpleJson.SerializeObject(command);

#if (DEBUG)
                logger.WriteLine("->> Command: " + type);
                logger.WriteLine("Data: " + ser);
                logger.WriteLine();
                logger.Flush();
#endif

                socket.Send(type + " " + ser);
            }
            catch (Exception ex)
            {
                ex.Source = "F-Chat connection, Send Message Method, IDictionary<string, object> overload";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        ///     Sends an argument-less command to the server
        /// </summary>
        /// <param name="commandType">
        ///     Type of command to send
        /// </param>
        public void SendMessage(string commandType)
        {
            try
            {
                if (commandType.Length > 3 || commandType.Length < 3)
                    throw new ArgumentOutOfRangeException("commandType", "Command type must be 3 characters long");

#if (DEBUG)
                logger.WriteLine("->> Command: " + commandType);
                logger.WriteLine();
                logger.Flush();
#endif

                socket.Send(commandType);
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
            Dispose(true);
        }

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="isManagedDispose">
        ///     The is managed dispose.
        /// </param>
        protected virtual void Dispose(bool isManagedDispose)
        {
#if (DEBUG)
            if (isManagedDispose)
                logger.Dispose();

#endif
            socket.Close();
        }

        #region Connection Management

        /// <summary>
        ///     When the user has picked a character and is ready to connect.
        /// </summary>
        /// <param name="character">
        ///     Character to connect with
        /// </param>
        private void ConnectToChat(string character)
        {
            try
            {
                Character = character.ThrowIfNull("character");

                events.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(ConnectToChat);

                socket = new WebSocket(Host);
                // define socket behavior
                socket.Opened += ConnectionOpened;
                socket.Error += ConnectionError;
                socket.MessageReceived += ConnectionMessageReceived;
                socket.Closed += ConnectionClosed;

                // start connection
                socket.Open();
            }
            catch (Exception ex)
            {
                ex.Source = "F-Chat Connection Service, init";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        ///     When our connection was closed.
        /// </summary>
        /// <param name="s">
        ///     The s.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void ConnectionClosed(object s, EventArgs e)
        {
            if (!isAuthenticated)
            {
                events.GetEvent<LoginFailedEvent>().Publish("Server closed the connection");
                AttemptReconnect();
                return;
            }

            events.GetEvent<ConnectionClosedEvent>().Publish(string.Empty);
            AttemptReconnect();
        }

        /// <summary>
        ///     When we got something from the server!
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void ConnectionMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            isAuthenticated = true;

            var commandType = e.Message.Substring(0, 3); // type of command sent

            var message = e.Message; // actual arguments sent

            if (e.Message.Length > 3)
            {
                // if it has arguments...
                message = message.Remove(0, 4); // chop off the command type

                var json = (IDictionary<string, object>) SimpleJson.DeserializeObject(message);

                // de-serialize it to an object model
                json.Add(Constants.Arguments.Command, commandType);

                // add back in the command type so our models can listen for them
#if (DEBUG)
    // for debug, write the command received to file
                logger.WriteLine("<<- Command: {0}", json["command"]);

                foreach (var pair in json.Where(pair => pair.Key != "command"))
                    logger.WriteLine("{0}: {1}", pair.Key, pair.Value);

                logger.WriteLine();
                logger.Flush();
#endif
                if (json.Get(Constants.Arguments.Command) == Constants.ServerCommands.SystemError 
                    && json.ContainsKey("number"))
                {
                    int err;
                    int.TryParse(json.Get("number"), out err);
                    var errsThatDisconnect = new[]
                        {
                            Constants.Errors.NoLoginSlots,
                            Constants.Errors.NoServerSlots,
                            Constants.Errors.KickedFromServer,
                            Constants.Errors.SimultaneousLoginKick,
                            Constants.Errors.BannedFromServer,
                            Constants.Errors.BadLoginInfo,
                            Constants.Errors.TooManyConnections,
                            Constants.Errors.UnknownLoginMethod
                        };

                    if (errsThatDisconnect.Contains(err)) isAuthenticated = false;
                }

                events.GetEvent<ChatCommandEvent>().Publish(json);
            }
            else
            {
                switch (e.Message)
                {
                    case Constants.ServerCommands.SystemPing:
                        SendMessage(Constants.ClientCommands.SystemPing); // auto-respond to pings
                        events.GetEvent<ChatCommandEvent>().Publish(null);
                        break;
                }
            }

            if (isAuthenticated)
                events.GetEvent<LoginAuthenticatedEvent>().Publish(null);
        }

        /// <summary>
        ///     When something done goofed itself
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void ConnectionError(object sender, ErrorEventArgs e)
        {
            events.GetEvent<LoginFailedEvent>().Publish(e.Exception.Message);
            AttemptReconnect();
        }

        /// <summary>
        ///     When we have connection to F-chat.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void ConnectionOpened(object sender, EventArgs e)
        {
            // Handshake completed, send login command
            object authRequest =
                new
                    {
                        ticket = Account.Ticket,
                        method = "ticket",
                        account = Account.AccountName,
                        character = Character,
                        cname = Constants.ClientId,
                        cversion = string.Format("{0} {1}", Constants.ClientName, Constants.ClientVer)
                    };

            SendMessage(authRequest, Constants.ClientCommands.SystemAuthenticate);

            if (staggerTimer != null)
            {
                staggerTimer.Dispose();
                staggerTimer = null;
            }
        }

        /// <summary>
        ///     If our connection failed, try to reconnect
        /// </summary>
        private void AttemptReconnect()
        {
            if (staggerTimer != null)
            {
                staggerTimer.Dispose();
                staggerTimer = null;
            }

            staggerTimer = new Timer((new Random().Next(10) + 5)*1000); // between 5 and 15 seconds
            staggerTimer.Elapsed += (s, e) =>
                {
                    ConnectToChat(Character);
                    events.GetEvent<ReconnectingEvent>().Publish(string.Empty);
                };
            staggerTimer.Enabled = true;
        }

        #endregion
    }
}