// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageModel.cs" company="Justin Kadrovach">
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
//   The message base.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Models
{
    using System;

    /// <summary>
    ///     The message base.
    /// </summary>
    public abstract class MessageBase : IDisposable
    {
        #region Fields

        internal readonly DateTimeOffset _posted;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageBase" /> class.
        /// </summary>
        public MessageBase()
        {
            this._posted = DateTimeOffset.Now;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the posted time.
        /// </summary>
        public DateTimeOffset PostedTime
        {
            get
            {
                return this._posted;
            }
        }

        /// <summary>
        ///     Gets the time stamp.
        /// </summary>
        public string TimeStamp
        {
            get
            {
                return this._posted.ToTimeStamp();
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        #endregion

        #region Methods

        internal abstract void Dispose(bool IsManaged);

        #endregion
    }

    /// <summary>
    ///     A model to hold data on messages
    /// </summary>
    public class MessageModel : MessageBase, IMessage
    {
        // Messages cannot be modified whence sent, safe to make these readonly
        #region Fields

        private readonly MessageType _type;

        private string _message;

        private ICharacter _poster;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageModel"/> class.
        ///     Creates a new message model
        /// </summary>
        /// <param name="poster">
        /// The character which posted the message
        /// </param>
        /// <param name="message">
        /// The message posted
        /// </param>
        /// <param name="type">
        /// The type of message posted
        /// </param>
        public MessageModel(ICharacter poster, string message, MessageType type = MessageType.normal)
        {
            this._poster = poster;
            this._message = message;
            this._type = type;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the message.
        /// </summary>
        public string Message
        {
            get
            {
                return this._message;
            }
        }

        /// <summary>
        ///     Gets the poster.
        /// </summary>
        public ICharacter Poster
        {
            get
            {
                return this._poster;
            }
        }

        /// <summary>
        ///     Gets the type.
        /// </summary>
        public MessageType Type
        {
            get
            {
                return this._type;
            }
        }

        #endregion

        #region Methods

        internal override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this._poster = null;
                this._message = null;
            }
        }

        #endregion
    }

    /// <summary>
    ///     Used to represent possible types of message sent to the client
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        ///     The ad.
        /// </summary>
        ad, 

        /// <summary>
        ///     The normal.
        /// </summary>
        normal, 

        /// <summary>
        ///     The roll.
        /// </summary>
        roll, 
    }

    /// <summary>
    ///     The Message interface.
    /// </summary>
    public interface IMessage : IDisposable
    {
        #region Public Properties

        /// <summary>
        ///     Gets the message.
        /// </summary>
        string Message { get; }

        /// <summary>
        ///     Gets the posted time.
        /// </summary>
        DateTimeOffset PostedTime { get; }

        /// <summary>
        ///     Gets the poster.
        /// </summary>
        ICharacter Poster { get; }

        /// <summary>
        ///     Gets the time stamp.
        /// </summary>
        string TimeStamp { get; }

        /// <summary>
        ///     Gets the type.
        /// </summary>
        MessageType Type { get; }

        #endregion
    }
}