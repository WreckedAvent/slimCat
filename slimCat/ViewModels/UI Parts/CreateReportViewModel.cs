// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateReportViewModel.cs" company="Justin Kadrovach">
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
//   The create report view model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;

    using lib;

    using Microsoft.Practices.Prism.Events;

    using Models;

    using slimCat;

    /// <summary>
    ///     The create report view model.
    /// </summary>
    public sealed class CreateReportViewModel : SysProp, IDisposable
    {
        #region Fields

        private RelayCommand _cancel;

        private IChatModel _cm;

        private string _complaint;

        private IEventAggregator _events;

        private bool _isOpen;

        private RelayCommand _send;

        private bool _shouldUploadLogs;

        private string _target;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateReportViewModel"/> class.
        /// </summary>
        /// <param name="eventagg">
        /// The eventagg.
        /// </param>
        /// <param name="cm">
        /// The cm.
        /// </param>
        public CreateReportViewModel(IEventAggregator eventagg, IChatModel cm)
        {
            this._events = eventagg;
            this._cm = cm;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the close command.
        /// </summary>
        public ICommand CloseCommand
        {
            get
            {
                if (this._cancel == null)
                {
                    this._cancel = new RelayCommand(param => this.IsOpen = !this._isOpen);
                }

                return this._cancel;
            }
        }

        /// <summary>
        ///     Gets or sets the complaint.
        /// </summary>
        public string Complaint
        {
            get
            {
                return this._complaint;
            }

            set
            {
                this._complaint = value;
                this.OnPropertyChanged("Complaint");
            }
        }

        /// <summary>
        ///     Gets the current tab.
        /// </summary>
        public string CurrentTab
        {
            get
            {
                return this._cm.SelectedChannel.ID == "Home" ? "None" : this._cm.SelectedChannel.ID;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is open.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return this._isOpen;
            }

            set
            {
                this._isOpen = value;
                if (!value)
                {
                    this.Complaint = null;
                }

                this.OnPropertyChanged("IsOpen");
            }
        }

        /// <summary>
        ///     Gets the send report command.
        /// </summary>
        public ICommand SendReportCommand
        {
            get
            {
                if (this._send == null)
                {
                    this._send = new RelayCommand(this.OnSendReport);
                }

                return this._send;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether should upload logs.
        /// </summary>
        public bool ShouldUploadLogs
        {
            get
            {
                return this._shouldUploadLogs;
            }

            set
            {
                this._shouldUploadLogs = value;
                this.OnPropertyChanged("ShouldUploadLogs");
            }
        }

        /// <summary>
        ///     Gets or sets the target.
        /// </summary>
        public string Target
        {
            get
            {
                return this._target;
            }

            set
            {
                this._target = value;
                this.OnPropertyChanged("Target");
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        #endregion

        #region Methods

        private void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this._cm = null;
                this._complaint = null;
                this._events = null;
            }
        }

        private void OnSendReport(object args)
        {
            IDictionary<string, object> command =
                CommandDefinitions.CreateCommand(
                    "report", new List<string> { this.Target, this.Complaint }, this.CurrentTab).toDictionary();

            this._events.GetEvent<UserCommandEvent>().Publish(command);
            this.IsOpen = !this.IsOpen;
            this._target = null;
            this.Complaint = null;
        }

        #endregion
    }
}