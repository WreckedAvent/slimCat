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
    using System.Diagnostics;
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
        #region Fields

        private readonly Timer autoPingTimer = new Timer(45*1000); // every 45 seconds
        private readonly int[] errsThatDisconnect;
        private readonly IEventAggregator events;

        private readonly ITicketProvider provider;
        private readonly Random random = new Random();
        private readonly Queue<KeyValuePair<string, object>> resendQueue = new Queue<KeyValuePair<string, object>>();
        private readonly Timer staggerTimer;
        private bool gaveUp;

        private bool isAuthenticated;
        private StreamWriter logger;

        private int retryAttempts = 0;
        private WebSocket socket;

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
        /// <param name="socket"></param>
        /// <param name="provider"></param>
        public ChatConnection(IAccount user, IEventAggregator eventagg, WebSocket socket, ITicketProvider provider)
        {
            this.socket = socket;
            this.provider = provider;
            Account = user.ThrowIfNull("user");
            events = eventagg.ThrowIfNull("eventagg");

            events.GetEvent<CharacterSelectedLoginEvent>()
                .Subscribe(ConnectToChat, ThreadOption.BackgroundThread, true);

            errsThatDisconnect = new[]
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

            InitializeLog();

            autoPingTimer.Elapsed += (s, e) => TrySend(Constants.ClientCommands.SystemPing);
            autoPingTimer.Start();

            staggerTimer = new Timer(5*1000); // first reconnect is 5 seconds
            staggerTimer.Elapsed += (s, e) => DoReconnect();
        }

        private void DoReconnect()
        {
            ConnectToChat(Character);
            staggerTimer.Interval = (random.Next(10) + 5)*1000; // 5 - 15 seconds
            staggerTimer.Stop();
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
        /// <param name="type">
        ///     The command_type.
        /// </param>
        public void SendMessage(object command, string type)
        {
            if (type.Length != 3)
                throw new ArgumentOutOfRangeException("type", "Command type must be 3 characters long");

            var ser = SimpleJson.SerializeObject(command);

            Log(type, ser);

            TrySend(type, ser);
        }

        /// <summary>
        ///     The send message.
        /// </summary>
        /// <param name="command">
        ///     The command.
        /// </param>
        public void SendMessage(IDictionary<string, object> command)
        {
            var type = command.Get(Constants.Arguments.Type);

            command.Remove(Constants.Arguments.Type);

            var ser = SimpleJson.SerializeObject(command);

            Log(type, ser);

            TrySend(type, ser);
        }

        /// <summary>
        ///     Sends an argument-less command to the server
        /// </summary>
        /// <param name="commandType">
        ///     Type of command to send
        /// </param>
        public void SendMessage(string commandType)
        {
            if (commandType.Length > 3 || commandType.Length < 3)
                throw new ArgumentOutOfRangeException("commandType", "Command type must be 3 characters long");

            Log(commandType);

            TrySend(commandType);
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
            if (isManagedDispose && logger != null)
                logger.Dispose();

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
            Character = character.ThrowIfNull("character");

            events.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(ConnectToChat);

            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting) return;

            socket = new WebSocket(Constants.ServerHost);

            // define socket behavior
            socket.Opened += ConnectionOpened;
            socket.Error += ConnectionError;
            socket.MessageReceived += ConnectionMessageReceived;
            socket.Closed += ConnectionClosed;

            // start connection
            socket.Open();
        }

        private void TrySend(string type, object args = null)
        {
            if (socket.State == WebSocketState.Open)
            {
                if (args != null)
                    socket.Send(type + " " + args);
                else
                    socket.Send(type);

                return;
            }

            resendQueue.Enqueue(new KeyValuePair<string, object>(type, args));

            if (socket.State != WebSocketState.Connecting)
                AttemptReconnect();
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
            if (!isAuthenticated)
            {
                isAuthenticated = true;
                events.GetEvent<LoginAuthenticatedEvent>().Publish(null);
                SendQueue();
            }

            var commandType = e.Message.Substring(0, 3); // type of command sent

            var message = e.Message; // actual arguments sent

            if (e.Message.Length <= 3)
            {
                events.GetEvent<ChatCommandEvent>().Publish(null);
                return;
            }

            // if it has arguments...
            message = message.Remove(0, 4); // chop off the command type

            var json = (IDictionary<string, object>) SimpleJson.DeserializeObject(message);

            Log(commandType, json, false);

            // de-serialize it to an object model
            json.Add(Constants.Arguments.Command, commandType);

            // add back in the command type so our models can listen for them
            if (json.Get(Constants.Arguments.Command) == Constants.ServerCommands.SystemError
                && json.ContainsKey("number"))
            {
                int err;
                int.TryParse(json.Get("number"), out err);

                if (errsThatDisconnect.Contains(err)) isAuthenticated = false;
            }

            events.GetEvent<ChatCommandEvent>().Publish(json);
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
                        ticket = provider.Ticket,
                        method = "ticket",
                        account = provider.Account.AccountName,
                        character = Character,
                        cname = Constants.ClientId,
                        cversion = string.Format("{0} {1}", Constants.ClientName, Constants.ClientVer)
                    };

            SendMessage(authRequest, Constants.ClientCommands.SystemAuthenticate);

            staggerTimer.Stop();
        }

        /// <summary>
        ///     If our connection failed, try to reconnect
        /// </summary>
        private void AttemptReconnect()
        {
            if (staggerTimer.Enabled || socket.State == WebSocketState.Open) return;

            staggerTimer.Start();
            isAuthenticated = false;
            events.GetEvent<LoginFailedEvent>().Publish(null);
        }

        private void SendQueue()
        {
            while (resendQueue.Count > 0)
            {
                var current = resendQueue.Dequeue();
                TrySend(current.Key, current.Value);
            }
        }

        [Conditional("DEBUG")]
        private void Log(string type, object payload = null, bool isSent = true)
        {
            logger.WriteLine("[{0}] {1} {2}{3}",
                DateTime.Now.ToString("h:mm:ss.ff tt"),
                isSent ? "sent" : "received",
                type,
                payload != null ? ":" : string.Empty);

            var dict = payload as IDictionary<string, object>;
            if (dict != null)
            {
                foreach (var pair in dict.Where(pair => pair.Key != Constants.Arguments.Command))
                    logger.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }
            else if (payload != null)
                logger.WriteLine(payload);

            logger.WriteLine();
            logger.Flush();
        }

        [Conditional("DEBUG")]
        private void InitializeLog()
        {
            if (!Directory.Exists(@"Debug"))
                Directory.CreateDirectory("Debug");

            logger = new StreamWriter(@"Debug\Rawchat " + DateTime.Now.Ticks + ".log", true);
        }

        #endregion
    }
}