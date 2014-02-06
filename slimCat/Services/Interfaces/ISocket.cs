namespace slimCat.Services
{
    using System;
    using SuperSocket.ClientEngine;
    using WebSocket4Net;

    public interface ISocket
    {
        event EventHandler Opened;

        event EventHandler<ErrorEventArgs> Error;

        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        event EventHandler Closed;

        void Open();

        void Close();

        void Send(string message);

        SocketState State { get; }
    }

    public enum SocketState
    {
        None,
        Open,
        Closed,
        Connecting,
        Closing
    }
}
