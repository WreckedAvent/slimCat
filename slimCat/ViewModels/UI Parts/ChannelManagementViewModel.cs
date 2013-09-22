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

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;

    using Slimcat;
    using Slimcat.Libraries;
    using Slimcat.Models;

    /// <summary>
    ///     The channel management view model.
    /// </summary>
    public class ChannelManagementViewModel : SysProp, IDisposable
    {
        #region Fields

        /// <summary>
        ///     The mode types.
        /// </summary>
        private readonly string[] modeTypes;

        private IEventAggregator events;

        private bool isOpen;

        private GeneralChannelModel model;

        private string description = string.Empty;

        private RelayCommand open;

        private RelayCommand toggleType;

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
            this.model = model;
            this.description = this.model.Description;
            this.events = eventagg;
            this.model.PropertyChanged += this.UpdateDescription;

            this.modeTypes = new[] { "Ads", "chat", "both" };
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
                return this.isOpen;
            }

            set
            {
                this.isOpen = value;
                this.OnPropertyChanged("IsManaging");

                if (!value && this.Description != this.model.Description)
                {
                    this.UpdateDescription();
                }
            }
        }

        /// <summary>
        ///     Gets or sets the motd.
        /// </summary>
        public string Description
        {
            get
            {
                return this.description;
            }

            set
            {
                this.description = value;
                this.OnPropertyChanged("Description");
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
                return this.open ?? (this.open = new RelayCommand(args => this.IsManaging = !this.IsManaging));
            }
        }

        /// <summary>
        ///     Gets the room mode string.
        /// </summary>
        public string RoomModeString
        {
            get
            {
                if (this.model.Mode == ChannelMode.Ads)
                {
                    return "allows only Ads.";
                }

                return this.model.Mode == ChannelMode.Chat ? "allows only chatting." : "allows Ads and chatting.";
            }
        }

        /// <summary>
        ///     Gets or sets the room mode type.
        /// </summary>
        public ChannelMode RoomModeType
        {
            get
            {
                return this.model.Mode;
            }

            set
            {
                if (this.model.Mode != value)
                {
                    this.model.Mode = value;
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
                if (this.model.Type == ChannelType.InviteOnly)
                {
                    return "Closed Private Channel";
                }

                return this.model.Type == ChannelType.Private ? "Open Private Channel" : "Public Channel";
            }
        }

        /// <summary>
        ///     Gets the toggle room tool tip.
        /// </summary>
        public string ToggleRoomToolTip
        {
            get
            {
                if (this.model.Type == ChannelType.InviteOnly)
                {
                    return
                        "The room is currently closed and requires an invite to join. Click to declare the room open, which will allow anyone to join it.";
                }

                return this.model.Type == ChannelType.Private ? "The room is currently open and does not require an invite to join. Click to declare the room closed, which will only allow those with an invite to join it." : "The room is currently a public room and cannot be closed.";
            }
        }

        /// <summary>
        ///     Gets the toggle room type command.
        /// </summary>
        public ICommand ToggleRoomTypeCommand
        {
            get
            {
                return this.toggleType
                       ?? (this.toggleType = new RelayCommand(this.OnToggleRoomType, this.CanToggleRoomType));
            }
        }

        /// <summary>
        ///     Gets the toggle room type string.
        /// </summary>
        public string ToggleRoomTypeString
        {
            get
            {
                if (this.model.Type == ChannelType.InviteOnly)
                {
                    return "Open this channel";
                }

                return this.model.Type == ChannelType.Private ? "Close this channel" : "Cannot close channel";
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
            if (!IsManaged)
            {
                return;
            }

            this.model.PropertyChanged -= this.UpdateDescription;
            this.events = null;
            this.model = null;
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
            return this.model.Type != ChannelType.Public;
        }

        private void OnRoomModeChanged(object args)
        {
            this.events.GetEvent<UserCommandEvent>()
                .Publish(
                    CommandDefinitions.CreateCommand("setmode", new[] { this.model.Mode.ToString() }, this.model.Id)
                                      .ToDictionary());
        }

        private void OnToggleRoomType(object args)
        {
            this.events.GetEvent<UserCommandEvent>()
                .Publish(
                    this.model.Type == ChannelType.InviteOnly
                        ? CommandDefinitions.CreateCommand("openroom", new List<string>(), this.model.Id).ToDictionary()
                        : CommandDefinitions.CreateCommand("closeroom", null, this.model.Id).ToDictionary());
        }

        private void UpdateDescription(object sender = null, PropertyChangedEventArgs e = null)
        {
            if (e != null)
            {
                // if its our property changed sending this
                if (e.PropertyName == "MOTD")
                {
                    this.description = this.model.Description;
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
                this.events.GetEvent<UserCommandEvent>()
                    .Publish(
                        CommandDefinitions.CreateCommand("setdescription", new[] { this.description }, this.model.Id)
                                          .ToDictionary());
            }
        }

        #endregion
    }
}