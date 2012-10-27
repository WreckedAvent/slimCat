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
    public abstract class ChannelViewModelBase : ViewModelBase, IDisposable
    {
        #region Fields
        private string _message = "";
        private static string _error;
        private IChatModel _cm;
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

        public IChatModel CM
        {
            get { return _cm; }
        }

        public bool HasError { get { return !string.IsNullOrWhiteSpace(Error); } }
        #endregion

        #region Constructors
        public ChannelViewModelBase(IUnityContainer contain, IRegionManager regman,
                                IEventAggregator events)
            : base(contain, regman, events)
        {
            _cm = _container.Resolve<IChatModel>();
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

        private RelayCommand _priv;
        public ICommand RequestPMCommand
        {
            get
            {
                if (_priv == null)
                    _priv = new RelayCommand(RequestPMEvent, CanRequestPM);

                return _priv;
            }
        }

        private void RequestPMEvent(object args)
        {
            string name = args as string;

            if (_cm.CurrentPMs.Any(
                param => param.ID.Equals((string)args, StringComparison.OrdinalIgnoreCase)))
            {
                _events.GetEvent<RequestChangeTabEvent>().Publish(name);
                return;
            }

            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand("priv", new [] { name })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
        }

        private bool CanRequestPM(object args)
        {
            string characterName = args as string;
            if (characterName == null) return false;

            return (_cm.SelectedCharacter.Name != characterName && _cm.SelectedChannel.ID != characterName);
        }

        private RelayCommand _ign;
        public ICommand IgnoreCommand
        {
            get
            {
                if (_ign == null)
                    _ign = new RelayCommand(AddIgnoreEvent,
                        args => { return !CM.Ignored.Contains(args as string) 
                            && CM.SelectedCharacter.Name != args as string; });
                return _ign;
            }
        }

        private RelayCommand _uign;
        public ICommand UnignoreCommand
        {
            get
            {
                if (_uign == null)
                    _uign = new RelayCommand(RemoveIgnoreEvent, args => { return CM.Ignored.Contains(args as string); });
                return _uign;
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

        private RelayCommand _join;
        public ICommand JoinChannelCommand
        {
            get
            {
                if (_join == null)
                    _join = new RelayCommand(RequestChannelJoinEvent, CanJoinChannel);
                return _join;
            }
        }

        protected void RequestChannelJoinEvent(object args)
        {
            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand("join", new List<string> { args as string })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
        }

        protected bool CanJoinChannel(object args)
        {
            return !CM
                .CurrentChannels
                .Any(param => param.ID.Equals((string)args, StringComparison.OrdinalIgnoreCase));
        }

        private void AddIgnoreEvent(object args) { IgnoreEvent(args); }
        private void RemoveIgnoreEvent(object args) { IgnoreEvent(args, true); }

        private void IgnoreEvent(object args, bool remove = false)
        {
            string name = args as string;

            IDictionary<string, object> command = CommandDefinitions
                .CreateCommand((remove ? "unignore" : "ignore"), new List<string>() { name })
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
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

                    else if (messageToCommand.RequiresMod && !HasPermissions())
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
        /// Checks if the person can send moderator commands
        /// </summary>
        protected virtual bool HasPermissions()
        {
            return (_cm.OnlineGlobalMods.Any(mod => mod.Name == _cm.SelectedCharacter.Name));
        }

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

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                PropertyChanged -= OnThisPropertyChanged;
                Model.PropertyChanged -= OnModelPropertyChanged;
            }
        }
    }
}
