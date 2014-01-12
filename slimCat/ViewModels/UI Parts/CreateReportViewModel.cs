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

namespace Slimcat.ViewModels
{
    using System.Collections.Generic;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;

    using Libraries;
    using Models;

    /// <summary>
    ///     The create report view model.
    /// </summary>
    public sealed class CreateReportViewModel : SysProp
    {
        #region Fields

        private RelayCommand cancel;

        private IChatModel cm;

        private string complaint;

        private IEventAggregator events;

        private bool isOpen;

        private RelayCommand send;

        private bool shouldUploadLogs;

        private string target;

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
            this.events = eventagg;
            this.cm = cm;
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
                return this.cancel ?? (this.cancel = new RelayCommand(param => this.IsOpen = !this.isOpen));
            }
        }

        /// <summary>
        ///     Gets or sets the complaint.
        /// </summary>
        public string Complaint
        {
            get
            {
                return this.complaint;
            }

            set
            {
                this.complaint = value;
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
                return this.cm.CurrentChannel.Id == "Home" ? "None" : this.cm.CurrentChannel.Id;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is open.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return this.isOpen;
            }

            set
            {
                this.isOpen = value;
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
                return this.send ?? (this.send = new RelayCommand(this.OnSendReport));
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether should upload logs.
        /// </summary>
        public bool ShouldUploadLogs
        {
            get
            {
                return this.shouldUploadLogs;
            }

            set
            {
                this.shouldUploadLogs = value;
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
                return this.target;
            }

            set
            {
                this.target = value;
                this.OnPropertyChanged("Target");
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                this.cm = null;
                this.complaint = null;
                this.events = null;
            }

            base.Dispose(isManaged);
        }

        private void OnSendReport(object args)
        {
            var command =
                CommandDefinitions.CreateCommand(
                    "report", new List<string> { this.Target, this.Complaint }, this.CurrentTab).ToDictionary();

            this.events.GetEvent<UserCommandEvent>().Publish(command);
            this.IsOpen = !this.IsOpen;
            this.target = null;
            this.Complaint = null;
        }

        #endregion
    }
}