// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelViewModelBase.cs" company="Justin Kadrovach">
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
//   This holds most of the logic for channel view models. Changing behaviors between channels should be done by overriding methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Models;

    using slimCat;

    /// <summary>
    ///     This holds most of the logic for channel view models. Changing behaviors between channels should be done by overriding methods.
    /// </summary>
    public abstract class ChannelViewModelBase : ViewModelBase
    {
        #region Static Fields

        private static string _error;

        private static Timer _errorRemoveTimer;

        #endregion

        #region Fields

        /// <summary>
        ///     The on line break event.
        /// </summary>
        public EventHandler OnLineBreakEvent;

        private RelayCommand _clear;

        private RelayCommand _clearLog;

        private RelayCommand _linebreak;

        private string _message = string.Empty;

        private ChannelModel _model;

        private RelayCommand _navDown;

        private RelayCommand _navUp;

        private RelayCommand _openLog;

        private RelayCommand _openLogFolder;

        private RelayCommand _sendText;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelViewModelBase"/> class.
        /// </summary>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        public ChannelViewModelBase(
            IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            this._events.GetEvent<ErrorEvent>().Subscribe(this.UpdateError);

            this.PropertyChanged += this.OnThisPropertyChanged;

            if (_errorRemoveTimer == null)
            {
                _errorRemoveTimer = new Timer(5000);
                _errorRemoveTimer.Elapsed += (s, e) => { this.Error = null; };

                _errorRemoveTimer.AutoReset = false;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the channel settings.
        /// </summary>
        public ChannelSettingsModel ChannelSettings
        {
            get
            {
                return this._model.Settings;
            }
        }

        /// <summary>
        ///     Gets the clear error command.
        /// </summary>
        public ICommand ClearErrorCommand
        {
            get
            {
                if (this._clear == null)
                {
                    this._clear = new RelayCommand(delegate { this.Error = null; });
                }

                return this._clear;
            }
        }

        /// <summary>
        ///     Gets the clear log command.
        /// </summary>
        public ICommand ClearLogCommand
        {
            get
            {
                if (this._clearLog == null)
                {
                    this._clearLog =
                        new RelayCommand(
                            args =>
                                {
                                    this._events.GetEvent<UserCommandEvent>()
                                        .Publish(CommandDefinitions.CreateCommand("clear").toDictionary());
                                });
                }

                return this._clearLog;
            }
        }

        /// <summary>
        ///     Gets or sets the error.
        /// </summary>
        public string Error
        {
            get
            {
                return _error;
            }

            set
            {
                _error = value;
                this.OnPropertyChanged("Error");
                this.OnPropertyChanged("HasError");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has error.
        /// </summary>
        public bool HasError
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.Error);
            }
        }

        /// <summary>
        ///     Gets the insert line break command.
        /// </summary>
        public ICommand InsertLineBreakCommand
        {
            get
            {
                if (this._linebreak == null)
                {
                    this._linebreak = new RelayCommand(args => this.Message = this.Message + '\n');
                }

                return this._linebreak;
            }
        }

        /// <summary>
        ///     Message is what the user inputs to send
        /// </summary>
        public string Message
        {
            get
            {
                return this._message;
            }

            set
            {
                this._message = value;
                this.OnPropertyChanged("Message");
            }
        }

        /// <summary>
        ///     Gets or sets the model.
        /// </summary>
        public ChannelModel Model
        {
            get
            {
                return this._model;
            }

            set
            {
                this._model = value;
                this.OnPropertyChanged("Model");
            }
        }

        /// <summary>
        ///     Gets the navigate down command.
        /// </summary>
        public ICommand NavigateDownCommand
        {
            get
            {
                if (this._navDown == null)
                {
                    this._navDown = new RelayCommand(args => this.RequestNavigateDirectionalEvent(false));
                }

                return this._navDown;
            }
        }

        /// <summary>
        ///     Gets the navigate up command.
        /// </summary>
        public ICommand NavigateUpCommand
        {
            get
            {
                if (this._navUp == null)
                {
                    this._navUp = new RelayCommand(args => this.RequestNavigateDirectionalEvent(true));
                }

                return this._navUp;
            }
        }

        /// <summary>
        ///     Gets the open log command.
        /// </summary>
        public ICommand OpenLogCommand
        {
            get
            {
                if (this._openLog == null)
                {
                    this._openLog = new RelayCommand(this.OnOpenLogEvent);
                }

                return this._openLog;
            }
        }

        /// <summary>
        ///     Gets the open log folder command.
        /// </summary>
        public ICommand OpenLogFolderCommand
        {
            get
            {
                if (this._openLogFolder == null)
                {
                    this._openLogFolder = new RelayCommand(this.OnOpenLogFolderEvent);
                }

                return this._openLogFolder;
            }
        }

        /// <summary>
        ///     Gets the send message command.
        /// </summary>
        public ICommand SendMessageCommand
        {
            get
            {
                if (this._sendText == null)
                {
                    this._sendText = new RelayCommand(param => this.ParseAndSend());
                }

                return this._sendText;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
        }

        /// <summary>
        /// The on open log event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public void OnOpenLogEvent(object args)
        {
            this.OpenLogEvent(args, false);
        }

        /// <summary>
        /// The on open log folder event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public void OnOpenLogFolderEvent(object args)
        {
            this.OpenLogEvent(args, true);
        }

        /// <summary>
        /// The open log event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <param name="isFolder">
        /// The is folder.
        /// </param>
        public void OpenLogEvent(object args, bool isFolder)
        {
            IDictionary<string, object> toSend =
                CommandDefinitions.CreateCommand(isFolder ? "openlogfolder" : "openlog").toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(toSend);
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
                this.PropertyChanged -= this.OnThisPropertyChanged;
                this.Model.PropertyChanged -= this.OnModelPropertyChanged;
                this._model = null;
                this.OnLineBreakEvent = null;
            }

            base.Dispose(IsManaged);
        }

        /// <summary>
        /// When properties change on the model
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected virtual void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// When properties on this class change
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected virtual void OnThisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Parsing behavior
        /// </summary>
        protected virtual void ParseAndSend()
        {
            if (this.Message != null)
            {
                if (CommandParser.HasNonCommand(this.Message))
                {
                    this.SendMessage();
                    return;
                }

                try
                {
                    var messageToCommand = new CommandParser(this.Message, this._model.ID);

                    if (!messageToCommand.HasCommand)
                    {
                        this.SendMessage();
                    }
                    else if ((messageToCommand.RequiresMod && !this.HasPermissions)
                             || (messageToCommand.Type.Equals("warn") && !this.HasPermissions))
                    {
                        this.UpdateError(
                            string.Format(
                                "I'm sorry Dave, I can't let you do the {0} command.", messageToCommand.Type));
                    }
                    else if (messageToCommand.IsValid)
                    {
                        this.SendCommand(messageToCommand.toDictionary());
                    }
                    else
                    {
                        this.UpdateError(string.Format("I don't know the {0} command.", messageToCommand.Type));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    this.UpdateError(ex.Message);
                }
            }
        }

        /// <summary>
        /// Command sending behavior
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        protected virtual void SendCommand(IDictionary<string, object> command)
        {
            if (this.Error != null)
            {
                this.Error = null;
            }

            this.Message = null;
            this._events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        ///     Message sending behavior
        /// </summary>
        protected abstract void SendMessage();

        /// <summary>
        /// Error handling behavior
        /// </summary>
        /// <param name="error">
        /// The error.
        /// </param>
        protected virtual void UpdateError(string error)
        {
            if (_errorRemoveTimer != null)
            {
                _errorRemoveTimer.Stop();
            }

            this.Error = error;
            _errorRemoveTimer.Start();
        }

        private void RequestNavigateDirectionalEvent(bool isUp)
        {
            if (this._cm.SelectedChannel is PMChannelModel)
            {
                int index = this._cm.CurrentPMs.IndexOf(this._cm.SelectedChannel as PMChannelModel);
                if (index == 0 && isUp)
                {
                    this._navigateStub(false, false);
                    return;
                }
                else if (index + 1 == this._cm.CurrentPMs.Count() && !isUp)
                {
                    this._navigateStub(true, false);
                    return;
                }
                else
                {
                    index += isUp ? -1 : 1;
                    this.RequestPMEvent(this._cm.CurrentPMs[index].ID);
                    return;
                }
            }
            else
            {
                int index = this._cm.CurrentChannels.IndexOf(this._cm.SelectedChannel as GeneralChannelModel);
                if (index == 0 && isUp)
                {
                    this._navigateStub(false, true);
                    return;
                }
                else if (index + 1 == this._cm.CurrentChannels.Count() && !isUp)
                {
                    this._navigateStub(true, true);
                    return;
                }
                else
                {
                    index += isUp ? -1 : 1;
                    this.RequestChannelJoinEvent(this._cm.CurrentChannels[index].ID);
                    return;
                }
            }
        }

        private void _navigateStub(bool getTop, bool fromPMs)
        {
            if (fromPMs)
            {
                ObservableCollection<PMChannelModel> collection = this._cm.CurrentPMs;
                if (collection.Count() == 0)
                {
                    this._navigateStub(false, false);
                    return;
                }

                string target = (getTop ? collection.First() : collection.Last()).ID;
                this.RequestPMEvent(target);
            }
            else
            {
                ObservableCollection<GeneralChannelModel> collection = this._cm.CurrentChannels;
                string target = (getTop ? collection.First() : collection.Last()).ID;
                this.RequestChannelJoinEvent(target);
            }
        }

        #endregion
    }
}