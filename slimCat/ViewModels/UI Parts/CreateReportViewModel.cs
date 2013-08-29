using lib;
using Microsoft.Practices.Prism.Events;
using Models;
using slimCat;
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
using System.Windows.Input;

namespace ViewModels
{

    public sealed class CreateReportViewModel : SysProp, IDisposable
    {
        #region fields
        private bool _isOpen = false;

        private IEventAggregator _events;
        private IChatModel _cm;
        private string _complaint;
        private bool _shouldUploadLogs;
        private string _target;
        #endregion

        #region Constructors
        public CreateReportViewModel(IEventAggregator eventagg, IChatModel cm)
        {
            _events = eventagg;
            _cm = cm;
        }
        #endregion

        #region properties
        public bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                _isOpen = value;
                if (!value)
                    Complaint = null;
                OnPropertyChanged("IsOpen");
            }
        }

        public string Complaint
        {
            get { return _complaint; }
            set
            {
                _complaint = value;
                OnPropertyChanged("Complaint");
            }
        }

        public bool ShouldUploadLogs
        {
            get { return _shouldUploadLogs; }
            set
            {
                _shouldUploadLogs = value;
                OnPropertyChanged("ShouldUploadLogs");
            }
        }

        public string CurrentTab
        {
            get
            {
                return _cm.SelectedChannel.ID == "Home" ? "None" : _cm.SelectedChannel.ID;
            }
        }

        public string Target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
                OnPropertyChanged("Target");
            }
        }
        #endregion

        #region commands
        private RelayCommand _cancel;
        public ICommand CloseCommand
        {
            get
            {
                if (_cancel == null)
                    _cancel = new RelayCommand(param => IsOpen = !_isOpen);
                return _cancel;
            }
        }

        private RelayCommand _send;
        public ICommand SendReportCommand
        {
            get
            {
                if (_send == null)
                    _send = new RelayCommand(OnSendReport);
                return _send;
            }
        }

        private void OnSendReport(object args)
        {
            var command = CommandDefinitions
                .CreateCommand("report", new List<string>() { Target, Complaint }, CurrentTab)
                .toDictionary();

            _events.GetEvent<UserCommandEvent>().Publish(command);
            IsOpen = !IsOpen;
            _target = null;
            Complaint = null;
        }
        #endregion

        #region methods
        private void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                _cm = null;
                _complaint = null;
                _events = null;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion
    }
}
