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

namespace Slimcat.Models
{
    using System.Text;
    using System.Timers;

    /// <summary>
    ///     Used for Private-Message communication between users
    /// </summary>
    public sealed class PMChannelModel : ChannelModel
    {
        #region Fields

        private ICharacter targetCharacter;

        private StringBuilder isTypingString;

        private Timer updateTick;

        private TypingStatus typing;

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
            : base(character.Name, ChannelType.PrivateMessage)
        {
            this.TargetCharacter = character;
            this.TargetCharacter.GetAvatar();

            this.updateTick = new Timer(1000);
            this.isTypingString = new StringBuilder();
            this.Settings = new ChannelSettingsModel(true);

            this.updateTick.Elapsed += (s, e) =>
                {
                    if (this.isTypingString.Length < 3)
                    {
                        this.isTypingString.Append(".");
                    }
                    else
                    {
                        this.isTypingString.Clear();
                        this.isTypingString.Append(".");
                    }

                    this.OnPropertyChanged("TypingString");
                };
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the display number.
        /// </summary>
        public int DisplayNumber
        {
            get
            {
                return this.Unread;
            }
        }

        /// <summary>
        ///     Gets or sets the PrivateMessage character.
        /// </summary>
        public ICharacter TargetCharacter
        {
            get
            {
                return this.targetCharacter;
            }

            set
            {
                this.targetCharacter = value;
                this.OnPropertyChanged("TargetCharacter");
            }
        }

        /// <summary>
        ///     Gets or sets the typing status.
        /// </summary>
        public TypingStatus TypingStatus
        {
            get
            {
                return this.typing;
            }

            set
            {
                this.typing = value;
                this.OnPropertyChanged("TypingStatus");
                this.OnPropertyChanged("TypingString");

                this.updateTick.Enabled = value == TypingStatus.Typing;
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
                    case TypingStatus.Paused:
                        return "~";
                    case TypingStatus.Clear:
                        return string.Empty;
                    case TypingStatus.Typing:
                        return this.isTypingString.ToString();
                }

                return string.Empty;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="isManaged">
        /// The is managed.
        /// </param>
        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this.updateTick.Dispose();
                this.updateTick = null;
                this.isTypingString = null;
                this.Settings = new ChannelSettingsModel(true);
            }

            base.Dispose(isManaged);
        }

        #endregion
    }
}