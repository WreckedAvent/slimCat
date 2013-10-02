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

namespace Slimcat.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Timers;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat.Libraries;
    using Slimcat.Models;
    using Slimcat.Utilities;

    /// <summary>
    ///     This holds most of the logic for channel view models. Changing behaviors between channels should be done by overriding methods.
    /// </summary>
    public abstract class ChannelViewModelBase : ViewModelBase
    {
        #region Static Fields

        private static string error;

        private static Timer errorRemoveTimer;

        #endregion

        #region Fields

        private RelayCommand clear;

        private RelayCommand clearLog;

        private RelayCommand linebreak;

        private string message = string.Empty;

        private ChannelModel model;

        private RelayCommand navDown;

        private RelayCommand navUp;

        private RelayCommand openLog;

        private RelayCommand openLogFolder;

        private RelayCommand sendText;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelViewModelBase" /> class.
        /// </summary>
        /// <param name="contain">
        ///     The contain.
        /// </param>
        /// <param name="regman">
        ///     The regman.
        /// </param>
        /// <param name="events">
        ///     The events.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        protected ChannelViewModelBase(
            IUnityContainer contain, IRegionManager regman, IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            this.Events.GetEvent<ErrorEvent>().Subscribe(this.UpdateError);

            this.PropertyChanged += this.OnThisPropertyChanged;

            if (errorRemoveTimer != null)
            {
                return;
            }

            errorRemoveTimer = new Timer(5000);
            errorRemoveTimer.Elapsed += (s, e) => { this.Error = null; };

            errorRemoveTimer.AutoReset = false;
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
                return this.model.Settings;
            }
        }

        /// <summary>
        ///     Gets the clear error command.
        /// </summary>
        public ICommand ClearErrorCommand
        {
            get
            {
                return this.clear ?? (this.clear = new RelayCommand(delegate { this.Error = null; }));
            }
        }

        /// <summary>
        ///     Gets the clear log command.
        /// </summary>
        public ICommand ClearLogCommand
        {
            get
            {
                return this.clearLog
                       ?? (this.clearLog =
                           new RelayCommand(
                               args =>
                               this.Events.GetEvent<UserCommandEvent>()
                                   .Publish(CommandDefinitions.CreateCommand("clear").ToDictionary())));
            }
        }

        /// <summary>
        ///     Gets or sets the error.
        /// </summary>
        public string Error
        {
            get
            {
                return error;
            }

            set
            {
                error = value;
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
                return this.linebreak ?? (this.linebreak = new RelayCommand(args => this.Message = this.Message + '\n'));
            }
        }

        /// <summary>
        ///     Message is what the user inputs to send
        /// </summary>
        public string Message
        {
            get
            {
                return this.message;
            }

            set
            {
                this.message = value;
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
                return this.model;
            }

            set
            {
                this.model = value;
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
                return this.navDown
                       ?? (this.navDown = new RelayCommand(args => this.RequestNavigateDirectionalEvent(false)));
            }
        }

        /// <summary>
        ///     Gets the navigate up command.
        /// </summary>
        public ICommand NavigateUpCommand
        {
            get
            {
                return this.navUp ?? (this.navUp = new RelayCommand(args => this.RequestNavigateDirectionalEvent(true)));
            }
        }

        /// <summary>
        ///     Gets the open log command.
        /// </summary>
        public ICommand OpenLogCommand
        {
            get
            {
                return this.openLog ?? (this.openLog = new RelayCommand(this.OnOpenLogEvent));
            }
        }

        /// <summary>
        ///     Gets the open log folder command.
        /// </summary>
        public ICommand OpenLogFolderCommand
        {
            get
            {
                return this.openLogFolder ?? (this.openLogFolder = new RelayCommand(this.OnOpenLogFolderEvent));
            }
        }

        /// <summary>
        ///     Gets the send message command.
        /// </summary>
        public ICommand SendMessageCommand
        {
            get
            {
                return this.sendText ?? (this.sendText = new RelayCommand(param => this.ParseAndSend()));
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
        ///     The on open log event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        public void OnOpenLogEvent(object args)
        {
            this.OpenLogEvent(args, false);
        }

        /// <summary>
        ///     The on open log folder event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        public void OnOpenLogFolderEvent(object args)
        {
            this.OpenLogEvent(args, true);
        }

        /// <summary>
        ///     The open log event.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <param name="isFolder">
        ///     The is folder.
        /// </param>
        public void OpenLogEvent(object args, bool isFolder)
        {
            var toSend =
                CommandDefinitions.CreateCommand(isFolder ? "openlogfolder" : "openlog").ToDictionary();

            this.Events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="isManaged">
        ///     The is managed.
        /// </param>
        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this.PropertyChanged -= this.OnThisPropertyChanged;
                this.Model.PropertyChanged -= this.OnModelPropertyChanged;
                this.model = null;
            }

            base.Dispose(isManaged);
        }

        /// <summary>
        ///     When properties change on the model
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        protected virtual void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     When properties on this class change
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        protected virtual void OnThisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Command sending behavior
        /// </summary>
        /// <param name="command">
        ///     The command.
        /// </param>
        protected void SendCommand(IDictionary<string, object> command)
        {
            this.Error = null;

            this.Message = null;
            this.Events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        ///     Message sending behavior
        /// </summary>
        protected abstract void SendMessage();

        /// <summary>
        ///     Error handling behavior
        /// </summary>
        /// <param name="error">
        ///     The error.
        /// </param>
        protected void UpdateError(string error)
        {
            if (errorRemoveTimer != null)
            {
                errorRemoveTimer.Stop();
            }

            this.Error = error;
            errorRemoveTimer.Start();
        }

        private void NavigateStub(bool getTop, bool fromPms)
        {
            if (fromPms)
            {
                var collection = this.ChatModel.CurrentPms;
                if (!collection.Any())
                {
                    this.NavigateStub(false, false);
                    return;
                }

                var target = (getTop ? collection.First() : collection.Last()).Id;
                this.RequestPmEvent(target);
            }
            else
            {
                var collection = this.ChatModel.CurrentChannels;
                var target = (getTop ? collection.First() : collection.Last()).Id;
                this.RequestChannelJoinEvent(target);
            }
        }

        private void ParseAndSend()
        {
            if (this.Message == null)
            {
                return;
            }

            if (CommandParser.HasNonCommand(this.Message))
            {
                this.SendMessage();
                return;
            }

            try
            {
                var messageToCommand = new CommandParser(this.Message, this.model.Id);

                if (!messageToCommand.HasCommand)
                {
                    this.SendMessage();
                }
                else if ((messageToCommand.RequiresMod && !this.HasPermissions)
                         || (messageToCommand.Type.Equals("warn") && !this.HasPermissions))
                {
                    this.UpdateError(
                        string.Format("I'm sorry Dave, I can't let you do the {0} command.", messageToCommand.Type));
                }
                else if (messageToCommand.IsValid)
                {
                    this.SendCommand(messageToCommand.ToDictionary());
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

        private void RequestNavigateDirectionalEvent(bool isUp)
        {
            if (this.ChatModel.CurrentChannel is PmChannelModel)
            {
                var index = this.ChatModel.CurrentPms.IndexOf(this.ChatModel.CurrentChannel as PmChannelModel);
                if (index == 0 && isUp)
                {
                    this.NavigateStub(false, false);
                    return;
                }

                if (index + 1 == this.ChatModel.CurrentPms.Count() && !isUp)
                {
                    this.NavigateStub(true, false);
                    return;
                }

                index += isUp ? -1 : 1;
                this.RequestPmEvent(this.ChatModel.CurrentPms[index].Id);
            }
            else
            {
                var index = this.ChatModel.CurrentChannels.IndexOf(this.ChatModel.CurrentChannel as GeneralChannelModel);
                if (index == 0 && isUp)
                {
                    this.NavigateStub(false, true);
                    return;
                }

                if (index + 1 == this.ChatModel.CurrentChannels.Count() && !isUp)
                {
                    this.NavigateStub(true, true);
                    return;
                }

                index += isUp ? -1 : 1;
                this.RequestChannelJoinEvent(this.ChatModel.CurrentChannels[index].Id);
            }
        }

        #endregion
    }
}