// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PMChannelModel.cs" company="Justin Kadrovach">
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
//   Used for Private-Message communication between users
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Models
{
    using System;
    using System.Text;
    using System.Timers;

    /// <summary>
    ///     Used for Private-Message communication between users
    /// </summary>
    public sealed class PMChannelModel : ChannelModel, IDisposable
    {
        #region Fields

        private ICharacter _PMCharacter;

        private StringBuilder _isTypingString;

        private Timer _tick;

        private Typing_Status _typing;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PMChannelModel"/> class.
        ///     Creates a new Channel data model which will always be a PM
        /// </summary>
        /// <param name="character">
        /// The character that sent the message
        /// </param>
        public PMChannelModel(ICharacter character)
            : base(character.Name, ChannelType.pm)
        {
            this.PMCharacter = character;
            this.PMCharacter.GetAvatar();

            this._tick = new Timer(1000);
            this._isTypingString = new StringBuilder();
            this._settings = new ChannelSettingsModel(true);

            this._tick.Elapsed += (s, e) =>
                {
                    if (this._isTypingString.Length < 3)
                    {
                        this._isTypingString.Append(".");
                    }
                    else
                    {
                        this._isTypingString.Clear();
                        this._isTypingString.Append(".");
                    }

                    this.OnPropertyChanged("TypingString");
                };
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the display number.
        /// </summary>
        public override int DisplayNumber
        {
            get
            {
                return this.Unread;
            }
        }

        /// <summary>
        ///     Gets or sets the pm character.
        /// </summary>
        public ICharacter PMCharacter
        {
            get
            {
                return this._PMCharacter;
            }

            set
            {
                this._PMCharacter = value;
                this.OnPropertyChanged("PMCharacter");
            }
        }

        /// <summary>
        ///     Gets or sets the typing status.
        /// </summary>
        public Typing_Status TypingStatus
        {
            get
            {
                return this._typing;
            }

            set
            {
                this._typing = value;
                this.OnPropertyChanged("TypingStatus");
                this.OnPropertyChanged("TypingString");

                if (value == Typing_Status.typing)
                {
                    this._tick.Enabled = true;
                }
                else
                {
                    this._tick.Enabled = false;
                }
            }
        }

        /// <summary>
        ///     Gets the typing string.
        /// </summary>
        public string TypingString
        {
            get
            {
                switch (this.TypingStatus)
                {
                    case Typing_Status.paused:
                        return "~";
                    case Typing_Status.clear:
                        return string.Empty;
                    case Typing_Status.typing:
                        return this._isTypingString.ToString();
                }

                return string.Empty;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="IsManaged">
        /// The is managed.
        /// </param>
        protected override void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                this._tick.Dispose();
                this._tick = null;
                this._isTypingString = null;
                this._settings = new ChannelSettingsModel(true);
            }

            base.Dispose(IsManaged);
        }

        #endregion
    }

    /// <summary>
    ///     The possible states of typing
    /// </summary>
    public enum Typing_Status
    {
        /// <summary>
        ///     The clear.
        /// </summary>
        clear, 

        /// <summary>
        ///     The paused.
        /// </summary>
        paused, 

        /// <summary>
        ///     The typing.
        /// </summary>
        typing
    }
}