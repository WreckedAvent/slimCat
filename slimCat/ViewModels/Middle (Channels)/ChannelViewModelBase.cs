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
