/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Models
{
    public abstract class MessageBase : IDisposable
    {
        internal readonly DateTimeOffset _posted;
        public DateTimeOffset PostedTime { get { return _posted; } }
        public string TimeStamp { get { return _posted.ToTimeStamp(); } }

        public MessageBase()
        {
            _posted = DateTimeOffset.Now;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        internal abstract void Dispose(bool IsManaged);
    }

    /// <summary>
    /// A model to hold data on messages
    /// </summary>
    public class MessageModel : MessageBase, IMessage
    {
        // Messages cannot be modified whence sent, safe to make these readonly
        #region Fields
        private ICharacter _poster;
        private string _message;
        private MessageType _type;
        #endregion

        #region Properties
        public ICharacter Poster { get { return _poster;} }
        public string Message { get { return _message; } }
        public MessageType Type { get { return _type; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new message model
        /// </summary>
        /// <param name="poster">The character which posted the message</param>
        /// <param name="message">The message posted</param>
        /// <param name="posted_time">The time it was posted</param>
        /// <param name="type">The type of message posted</param>
        public MessageModel(ICharacter poster, string message, MessageType type = MessageType.normal)
            :base()
        {
            _poster = poster;
            _message = message;
            _type = type;
        }
        #endregion

        internal override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                _poster = null;
                _message = null;
            }
        }
    }

    /// <summary>
    /// Used to represent possible types of message sent to the client
    /// </summary>
    public enum MessageType
    {
        ad,
        normal,
        roll,
    }

    public interface IMessage : IDisposable
    {
        ICharacter Poster { get; }
        string Message { get; }
        string TimeStamp { get; }
        DateTimeOffset PostedTime { get; }
        MessageType Type { get; }
    }
}
