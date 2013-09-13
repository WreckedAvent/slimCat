// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelManagementViewModel.cs" company="Justin Kadrovach">
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
//   The channel management view model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;

    using Models;

    using slimCat;

    /// <summary>
    ///     The channel management view model.
    /// </summary>
    public class ChannelManagementViewModel : SysProp, IDisposable
    {
        #region Fields

        /// <summary>
        ///     The mode types.
        /// </summary>
        public string[] modeTypes;

        private IEventAggregator _events;

        private bool _isOpen;

        private GeneralChannelModel _model;

        private string _motd = string.Empty;

        private RelayCommand _open;

        private RelayCommand _toggleType;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelManagementViewModel"/> class.
        /// </summary>
        /// <param name="eventagg">
        /// The eventagg.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        public ChannelManagementViewModel(IEventAggregator eventagg, GeneralChannelModel model)
        {
            this._model = model;
            this._motd = this._model.MOTD;
            this._events = eventagg;
            this._model.PropertyChanged += this.UpdateDescription;

            this.modeTypes = new[] { "ads", "chat", "both" };
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets a value indicating whether is managing.
        /// </summary>
        public bool IsManaging
        {
            get
            {
                return this._isOpen;
            }

            set
            {
                this._isOpen = value;
                this.OnPropertyChanged("IsManaging");

                if (!value && this.MOTD != this._model.MOTD)
                {
                    this.UpdateDescription();
                }
            }
        }

        /// <summary>
        ///     Gets or sets the motd.
        /// </summary>
        public string MOTD
        {
            get
            {
                return this._motd;
            }

            set
            {
                this._motd = value;
                this.OnPropertyChanged("MOTD");
            }
        }

        /// <summary>
        ///     Gets the mode types.
        /// </summary>
        public string[] ModeTypes
        {
            get
            {
                return this.modeTypes;
            }
        }

        /// <summary>
        ///     Gets the open channel management command.
        /// </summary>
        public ICommand OpenChannelManagementCommand
        {
            get
            {
                if (this._open == null)
                {
                    this._open = new RelayCommand(args => this.IsManaging = !this.IsManaging);
                }

                return this._open;
            }
        }

        /// <summary>
        ///     Gets the room mode string.
        /// </summary>
        public string RoomModeString
        {
            get
            {
                if (this._model.Mode == ChannelMode.ads)
                {
                    return "allows only ads.";
                }

                if (this._model.Mode == ChannelMode.chat)
                {
                    return "allows only chatting.";
                }

                return "allows ads and chatting.";
            }
        }

        /// <summary>
        ///     Gets or sets the room mode type.
        /// </summary>
        public ChannelMode RoomModeType
        {
            get
            {
                return this._model.Mode;
            }

            set
            {
                if (this._model.Mode != value)
                {
                    this._model.Mode = value;
                    this.OnRoomModeChanged(null);
                }
            }
        }

        /// <summary>
        ///     Gets the room type string.
        /// </summary>
        public string RoomTypeString
        {
            get
            {
                if (this._model.Type == ChannelType.closed)
                {
                    return "Closed Private Channel";
                }

                if (this._model.Type == ChannelType.priv)
                {
                    return "Open Private Channel";
                }
                else
                {
                    return "Public Channel";
                }
            }
        }

        /// <summary>
        ///     Gets the toggle room tool tip.
        /// </summary>
        public string ToggleRoomToolTip
        {
            get
            {
                if (this._model.Type == ChannelType.closed)
                {
                    return
                        "The room is currently closed and requires an invite to join. Click to declare the room open, which will allow anyone to join it.";
                }

                if (this._model.Type == ChannelType.priv)
                {
                    return
                        "The room is currently open and does not require an invite to join. Click to declare the room closed, which will only allow those with an invite to join it.";
                }
                else
                {
                    return "The room is currently a public room and cannot be closed.";
                }
            }
        }

        /// <summary>
        ///     Gets the toggle room type command.
        /// </summary>
        public ICommand ToggleRoomTypeCommand
        {
            get
            {
                if (this._toggleType == null)
                {
                    this._toggleType = new RelayCommand(this.OnToggleRoomType, this.CanToggleRoomType);
                }

                return this._toggleType;
            }
        }

        /// <summary>
        ///     Gets the toggle room type string.
        /// </summary>
        public string ToggleRoomTypeString
        {
            get
            {
                if (this._model.Type == ChannelType.closed)
                {
                    return "Open this channel";
                }

                if (this._model.Type == ChannelType.priv)
                {
                    return "Close this channel";
                }
                else
                {
                    return "Cannot close channel";
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The dispose.
        /// </summary>
        /// <param name="IsManaged">
        /// The is managed.
        /// </param>
        public void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                this._model.PropertyChanged -= this.UpdateDescription;
                this._events = null;
                this._model = null;
            }
        }

        #endregion

        #region Explicit Interface Methods

        void IDisposable.Dispose()
        {
            this.Dispose(true);
        }

        #endregion

        #region Methods

        private bool CanToggleRoomType(object args)
        {
            return this._model.Type != ChannelType.pub;
        }

        private void OnRoomModeChanged(object args)
        {
            this._events.GetEvent<UserCommandEvent>()
                .Publish(
                    CommandDefinitions.CreateCommand("setmode", new[] { this._model.Mode.ToString() }, this._model.ID)
                                      .toDictionary());
        }

        private void OnToggleRoomType(object args)
        {
            if (this._model.Type == ChannelType.closed)
            {
                this._events.GetEvent<UserCommandEvent>()
                    .Publish(
                        CommandDefinitions.CreateCommand("openroom", new List<string>(), this._model.ID).toDictionary());
            }
            else
            {
                this._events.GetEvent<UserCommandEvent>()
                    .Publish(CommandDefinitions.CreateCommand("closeroom", null, this._model.ID).toDictionary());
            }
        }

        private void UpdateDescription(object sender = null, PropertyChangedEventArgs e = null)
        {
            if (e != null)
            {
                // if its our property changed sending this
                if (e.PropertyName == "MOTD")
                {
                    this._motd = this._model.MOTD;
                    this.OnPropertyChanged("MOTD");
                }
                else if (e.PropertyName == "Type")
                {
                    this.OnPropertyChanged("ToggleRoomTip");
                    this.OnPropertyChanged("ToggleRoomTypeString");
                    this.OnPropertyChanged("RoomTypeString");
                }
                else if (e.PropertyName == "Mode")
                {
                    this.OnPropertyChanged("RoomModeString");
                }
            }
            else
            {
                // if its us updating it
                this._events.GetEvent<UserCommandEvent>()
                    .Publish(
                        CommandDefinitions.CreateCommand("setdescription", new[] { this._motd }, this._model.ID)
                                          .toDictionary());
            }
        }

        #endregion
    }
}