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

namespace Slimcat.Models
{
    using System.Windows.Documents;

    using Slimcat.Views;

    /// <summary>
    ///     A model to hold data on messages
    /// </summary>
    public class MessageModel : MessageBase, IMessage
    {
        // Messages cannot be modified whence sent, safe to make these readonly
        #region Fields

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
        public MessageModel(ICharacter poster, string message, MessageType type = MessageType.Normal)
        {
            this.Poster = poster;
            this.Message = message;
            this.Type = type;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        ///     Gets the poster.
        /// </summary>
        public ICharacter Poster { get; private set; }

        /// <summary>
        ///     Gets the type.
        /// </summary>
        public MessageType Type { get; private set; }

        /// <summary>
        ///     Gets the view associated with messages.
        /// </summary>
        public Block View
        {
            get
            {
                return new MessageView { DataContext = this };
            }
        }

        #endregion

        #region Methods

        internal override void Dispose(bool isManaged)
        {
            if (!isManaged)
            {
                return;
            }

            this.Poster = null;
        }

        #endregion
    }
}