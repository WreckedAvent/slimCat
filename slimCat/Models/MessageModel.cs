#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageModel.cs">
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

namespace Slimcat.Models
{
    #region Usings

    using System.Windows.Documents;
    using Views;

    #endregion

    /// <summary>
    ///     A model to hold data on messages
    /// </summary>
    public class MessageModel : MessageBase, IMessage
    {
        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageModel" /> class.
        /// </summary>
        /// <param name="poster">
        ///     The character which posted the message
        /// </param>
        /// <param name="message">
        ///     The message posted
        /// </param>
        /// <param name="type">
        ///     The type of message posted
        /// </param>
        public MessageModel(ICharacter poster, string message, MessageType type = MessageType.Normal)
        {
            Poster = poster;
            Message = message;
            Type = type;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageModel" /> class.
        /// </summary>
        /// <param name="message">
        ///     The message posted at a previous date
        /// </param>
        public MessageModel(string message)
        {
            Poster = new CharacterModel {Name = string.Empty};
            IsHistoryMessage = true;
            Type = message.StartsWith("[")
                ? MessageType.Normal
                : MessageType.Ad;

            Message = message;
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


        public bool IsHistoryMessage { get; private set; }

        /// <summary>
        ///     Gets the view associated with messages.
        /// </summary>
        public Block View
        {
            get
            {
                if (IsHistoryMessage)
                    return new HistoryView {DataContext = Message};

                return new MessageView {DataContext = this};
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (!isManaged)
                return;

            Poster = null;
        }

        #endregion
    }
}