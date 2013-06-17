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
using System.Windows.Input;
using slimCat;
using lib;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Unity;
using Models;
using ViewModels;
using System.ComponentModel;

namespace ViewModels
{
    /// <summary>
    /// This holds most of the logic for channel view models. Changing behaviors between channels should be done by overriding methods.
    /// </summary>
    public abstract class ChannelViewModelBase : ViewModelBase
    {
        #region Fields
        private string _message = "";
        private static string _error;
        private ChannelModel _model;
        public EventHandler OnLineBreakEvent;
        private static System.Timers.Timer _errorRemoveTimer;
        #endregion

        #region Properties
        /// <summary>
        /// Message is what the user inputs to send
        /// </summary>
        public string Message
        {
            get { return _message; }
            set 
            { 
                _message = value; 
                OnPropertyChanged("Message");
            }
        }

        public string Error
        {
            get { return _error; }
            set { _error = value; OnPropertyChanged("Error"); OnPropertyChanged("HasError"); }
        }

        public ChannelModel Model
        {
            get { return _model; }
            set { _model = value; OnPropertyChanged("Model"); }
        }

        public bool HasError { get { return !string.IsNullOrWhiteSpace(Error); } }

        public ChannelSettingsModel ChannelSettings { get { return _model.Settings; } }
        #endregion

        #region Constructors
        public ChannelViewModelBase(IUnityContainer contain, IRegionManager regman,
                                IEventAggregator events, IChatModel cm)
            : base(contain, regman, events, cm)
        {
            _events.GetEvent<ErrorEvent>().Subscribe(UpdateError);

            PropertyChanged += OnThisPropertyChanged;

            if (_errorRemoveTimer == null)
            {
                _errorRemoveTimer = new System.Timers.Timer(5000);
                _errorRemoveTimer.Elapsed += (s, e) =>
                    {
                        this.Error = null;
                    };

                _errorRemoveTimer.AutoReset = false;
            }
        }

        public override void Initialize() { }
        #endregion

        #region Comands
        private RelayCommand _clear;
        public ICommand ClearErrorCommand
        {
            get
            {
                if (_clear == null)
                    _clear = new RelayCommand(delegate { Error = null; });

                return _clear;
            }
        }

        private RelayCommand _sendText;
        public ICommand SendMessageCommand
        {
            get
            {
                if (_sendText == null)
                    _sendText = new RelayCommand(param => this.ParseAndSend());

                return _sendText;
            }
        }

        private RelayCommand _linebreak;
        public ICommand InsertLineBreakCommand
        {
            get
            {
                if (_linebreak == null)
                    _linebreak = new RelayCommand(args => Message = Message + '\n');
                return _linebreak;
            }
        }

        private RelayCommand _openLog;
        public ICommand OpenLogCommand
        {
            get
            {
                if (_openLog == null)
                    _openLog = new RelayCommand(OnOpenLogEvent);
                return _openLog;
            }
        }

        private RelayCommand _openLogFolder;
        public ICommand OpenLogFolderCommand
        {
            get
            {
                if (_openLogFolder == null)
                    _openLogFolder = new RelayCommand(OnOpenLogFolderEvent);
                return _openLogFolder;
            }
        }

        public void OnOpenLogEvent(object args) { OpenLogEvent(args, false); }
        public void OnOpenLogFolderEvent(object args) { OpenLogEvent(args, true); }

        public void OpenLogEvent(object args, bool isFolder)
        {
            IDictionary<string, object> toSend = CommandDefinitions.CreateCommand((isFolder ? "openlogfolder" : "openlog")).toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(toSend);
        }

        RelayCommand _clearLog;
        public ICommand ClearLogCommand
        {
            get
            {
                if (_clearLog == null)
                    _clearLog = new RelayCommand(args =>
                        {
                            _events.GetEvent<UserCommandEvent>().Publish(
                                CommandDefinitions.CreateCommand("clear").toDictionary()
                            );
                        });
                return _clearLog;
            }
        }

        #region Navigate Shortcuts
        private RelayCommand _navUp;
        private RelayCommand _navDown;

        public ICommand NavigateUpCommand
        {
            get
            {
                if (_navUp == null)
                    _navUp = new RelayCommand(args => RequestNavigateDirectionalEvent(true));
                return _navUp;
            }
        }

        public ICommand NavigateDownCommand
        {
            get
            {
                if (_navDown == null)
                    _navDown = new RelayCommand(args => RequestNavigateDirectionalEvent(false));
                return _navDown;
            }
        }

        private void RequestNavigateDirectionalEvent(bool isUp)
        {
            if (_cm.SelectedChannel is PMChannelModel)
            {
                var index = _cm.CurrentPMs.IndexOf(_cm.SelectedChannel as PMChannelModel);
                if (index == 0 && isUp)
                {
                    _navigateStub(false, false);
                    return;
                }
                else if (index+1 == _cm.CurrentPMs.Count() && !isUp)
                {
                    _navigateStub(true, false);
                    return;
                }
                else
                {
                    index += isUp ? -1 : 1;
                    RequestPMEvent(_cm.CurrentPMs[index].ID);
                    return;
                }
            }
            else
            {
                var index = _cm.CurrentChannels.IndexOf(_cm.SelectedChannel as GeneralChannelModel);
                if (index == 0 && isUp)
                {
                    _navigateStub(false, true);
                    return;
                }
                else if (index+1 == _cm.CurrentChannels.Count() && !isUp)
                {
                    _navigateStub(true, true);
                    return;
                }
                else
                {
                    index += isUp ? -1 : 1;
                    RequestChannelJoinEvent(_cm.CurrentChannels[index].ID);
                    return;
                }
            }
        }

        private void _navigateStub(bool getTop, bool fromPMs)
        {
            if (fromPMs)
            {
                var collection = _cm.CurrentPMs;
                if (collection.Count() == 0)
                {
                    _navigateStub(false, false);
                    return;
                }

                var target = (getTop ? collection.First() : collection.Last()).ID;
                RequestPMEvent(target);
            }
            else
            {
                var collection = _cm.CurrentChannels;
                var target = (getTop ? collection.First() : collection.Last()).ID;
                RequestChannelJoinEvent(target);
            }
        }
        #endregion
        #endregion

        #region Methods
        /// <summary>
        /// Parsing behavior
        /// </summary>
        protected virtual void ParseAndSend()
        {
            if (Message != null)
            {
                if (CommandParser.HasNonCommand(Message))
                {
                    SendMessage();
                    return;
                }

                try
                {
                    CommandParser messageToCommand = new CommandParser(Message, _model.ID);

                    if (!messageToCommand.HasCommand)
                        SendMessage();

                    else if ((messageToCommand.RequiresMod && !HasPermissions) || (messageToCommand.Type.Equals("warn") && !HasPermissions))
                        UpdateError(string.Format("I'm sorry Dave, I can't let you do the {0} command.", messageToCommand.Type));

                    else if (messageToCommand.IsValid)
                        SendCommand(messageToCommand.toDictionary());

                    else
                        UpdateError(string.Format("I don't know the {0} command.", messageToCommand.Type));
                }

                catch (InvalidOperationException ex)
                {
                    UpdateError(ex.Message);
                }
            }
        }

        /// <summary>
        /// Message sending behavior
        /// </summary>
        protected abstract void SendMessage();

        /// <summary>
        /// Command sending behavior
        /// </summary>
        protected virtual void SendCommand(IDictionary<string, object> command)
        {
            if (Error != null) this.Error = null;

            Message = null;
            this._events.GetEvent<UserCommandEvent>().Publish(command);
        }

        /// <summary>
        /// Error handling behavior
        /// </summary>
        protected virtual void UpdateError(string error)
        {
            if (_errorRemoveTimer != null)
                _errorRemoveTimer.Stop();
            this.Error = error;
            _errorRemoveTimer.Start();
        }

        /// <summary>
        /// When properties on this class change
        /// </summary>
        protected virtual void OnThisPropertyChanged(object sender, PropertyChangedEventArgs e) {}

        /// <summary>
        /// When properties change on the model
        /// </summary>
        protected virtual void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e) { }
        #endregion

        override protected void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                PropertyChanged -= OnThisPropertyChanged;
                Model.PropertyChanged -= OnModelPropertyChanged;
                _model = null;
                OnLineBreakEvent = null;
            }

            base.Dispose(IsManaged);
        }
    }
}
