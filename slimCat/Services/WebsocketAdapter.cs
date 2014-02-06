namespace slimCat.Services
{
    using System;
    using System.Collections.Generic;
    using SuperSocket.ClientEngine;
    using WebSocket4Net;

    public class WebsocketAdapter : ISocket
    {
        private readonly WebSocket socket;
        private readonly IDictionary<WebSocketState, SocketState> states; 

        public WebsocketAdapter(string host)
        {
            socket = new WebSocket(host);
            states = new Dictionary<WebSocketState, SocketState>
                {
                    {WebSocketState.Closed, SocketState.Closed},
                    {WebSocketState.None, SocketState.None},
                    {WebSocketState.Connecting, SocketState.Connecting},
                    {WebSocketState.Open, SocketState.Open},
                    {WebSocketState.Closing, SocketState.Closing}
                };

            socket.Opened += (s, e) => Opened.Invoke(s, e);
            socket.Error += (s, e) => Error.Invoke(s, e);
            socket.MessageReceived += (s, e) => MessageReceived.Invoke(s, e);
            socket.Closed += (s, e) => Closed.Invoke(s, e);
        }

        public event EventHandler Opened;

        public event EventHandler<ErrorEventArgs> Error;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event EventHandler Closed;

        public void Open()
        {
            socket.Open();
        }

        public void Close()
        {
            socket.Close();
        }

        public void Send(string message)
        {
            socket.Send(message);
        }

        public SocketState State
        {
            get { return states[socket.State]; }
        }
    }
}
