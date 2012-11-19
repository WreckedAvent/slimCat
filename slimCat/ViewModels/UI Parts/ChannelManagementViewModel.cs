using lib;
using Microsoft.Practices.Prism.Events;
using Models;
using slimCat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ViewModels
{
    public class ChannelManagementViewModel : SysProp, IDisposable
    {
        #region Fields
        GeneralChannelModel _model;
        IEventAggregator _events;
        string _motd = "";
        bool _isOpen = false;
        public string[] modeTypes;
        #endregion

        #region Constructors
        public ChannelManagementViewModel(IEventAggregator eventagg, GeneralChannelModel model)
        {
            _model = model;
            _motd = _model.MOTD;
            _events = eventagg;
            _model.PropertyChanged += UpdateDescription;

            modeTypes = new string[] { "ads", "chat", "both" };
        }
        #endregion

        #region Properties
        public string MOTD
        {
            get { return _motd; }
            set
            {
                _motd = value;
                OnPropertyChanged("MOTD");
            }
        }

        public bool IsManaging
        {
            get { return _isOpen; }
            set 
            { 
                _isOpen = value;
                OnPropertyChanged("IsManaging");

                if (!value && MOTD != _model.MOTD)
                    UpdateDescription();
            }
        }

        public string ToggleRoomTypeString
        {
            get
            {
                if (_model.Type == ChannelType.closed)
                    return "Open this channel";
                if (_model.Type == ChannelType.priv)
                    return "Close this channel";
                else
                    return "Cannot close channel";
            }
        }

        public string RoomTypeString
        {
            get
            {
                if (_model.Type == ChannelType.closed)
                    return "Closed Private Channel";
                if (_model.Type == ChannelType.priv)
                    return "Open Private Channel";
                else
                    return "Public Channel";
            }
        }

        public string ToggleRoomToolTip
        {
            get
            {
                if (_model.Type == ChannelType.closed)
                    return "The room is currently closed and requires an invite to join. Click to declare the room open, which will allow anyone to join it.";
                if (_model.Type == ChannelType.priv)
                    return "The room is currently open and does not require an invite to join. Click to declare the room closed, which will only allow those with an invite to join it.";
                else
                    return "The room is currently a public room and cannot be closed.";
            }
        }

        public string RoomModeString
        {
            get
            {
                if (_model.Mode == ChannelMode.ads)
                    return "allows only ads.";
                if (_model.Mode == ChannelMode.chat)
                    return "allows only chatting.";
                return "allows ads and chatting.";
            }
        }

        public ChannelMode RoomModeType
        {
            get { return _model.Mode; }
            set
            {
                if (_model.Mode != value)
                {
                    _model.Mode = value;
                    OnRoomModeChanged(null);
                }
            }
        }

        public string[] ModeTypes { get { return modeTypes; } }
        #endregion

        #region Commands
        RelayCommand _toggleType;
        public ICommand ToggleRoomTypeCommand
        {
            get
            {
                if (_toggleType == null)
                    _toggleType = new RelayCommand(OnToggleRoomType, CanToggleRoomType);
                return _toggleType;
            }
        }

        RelayCommand _open;
        public ICommand OpenChannelManagementCommand
        {
            get
            {
                if (_open == null)
                    _open = new RelayCommand(args => IsManaging = !IsManaging);
                return _open;
            }
        }
        #endregion

        #region Methods
        private void UpdateDescription(object sender = null, PropertyChangedEventArgs e = null)
        {
            if (e != null) // if its our property changed sending this
            {
                if (e.PropertyName == "MOTD")
                {
                    _motd = _model.MOTD;
                    OnPropertyChanged("MOTD");
                }

                else if (e.PropertyName == "Type")
                {
                    OnPropertyChanged("ToggleRoomTip");
                    OnPropertyChanged("ToggleRoomTypeString");
                    OnPropertyChanged("RoomTypeString");
                }

                else if (e.PropertyName == "Mode")
                {
                    OnPropertyChanged("RoomModeString");
                }
            }
            else // if its us updating it
            {
                _events.GetEvent<UserCommandEvent>().Publish(
                    CommandDefinitions.CreateCommand("setdescription", new [] {_motd}, _model.ID).toDictionary()
                    );
            }
        }
        private void OnRoomModeChanged(object args)
        {
            _events.GetEvent<UserCommandEvent>().Publish(
                CommandDefinitions.CreateCommand("setmode", new[] { _model.Mode.ToString() }, _model.ID)
                .toDictionary());
        }
        private void OnToggleRoomType(object args)
        {
            if (_model.Type == ChannelType.closed)
            {
                _events.GetEvent<UserCommandEvent>().Publish(
                    CommandDefinitions.CreateCommand("openroom", new List<string>(), _model.ID).toDictionary()
                    );
            }
            else
                _events.GetEvent<UserCommandEvent>().Publish(
                    CommandDefinitions.CreateCommand("closeroom", null, _model.ID).toDictionary()
                    );
        }
        private bool CanToggleRoomType(object args)
        {
            return _model.Type != ChannelType.pub;
        }
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool IsManaged)
        {
            if (IsManaged)
            {
                _model.PropertyChanged -= UpdateDescription;
                _events = null;
                _model = null;
            }
        }
        #endregion
    }
}
