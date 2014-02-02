#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateReportViewModel.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.ViewModels
{
    #region Usings

    using System.Collections.Generic;
    using System.Windows.Input;
    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;

    #endregion

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
        ///     Initializes a new instance of the <see cref="CreateReportViewModel" /> class.
        /// </summary>
        /// <param name="eventagg">
        ///     The eventagg.
        /// </param>
        /// <param name="cm">
        ///     The cm.
        /// </param>
        public CreateReportViewModel(IEventAggregator eventagg, IChatModel cm)
        {
            events = eventagg;
            this.cm = cm;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the close command.
        /// </summary>
        public ICommand CloseCommand
        {
            get { return cancel ?? (cancel = new RelayCommand(param => IsOpen = !isOpen)); }
        }

        /// <summary>
        ///     Gets or sets the complaint.
        /// </summary>
        public string Complaint
        {
            get { return complaint; }

            set
            {
                complaint = value;
                OnPropertyChanged("Complaint");
            }
        }

        /// <summary>
        ///     Gets the current tab.
        /// </summary>
        public string CurrentTab
        {
            get { return cm.CurrentChannel.Id == "Home" ? "None" : cm.CurrentChannel.Id; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is open.
        /// </summary>
        public bool IsOpen
        {
            get { return isOpen; }

            set
            {
                isOpen = value;
                if (!value)
                    Complaint = null;

                OnPropertyChanged("IsOpen");
            }
        }

        /// <summary>
        ///     Gets the send report command.
        /// </summary>
        public ICommand SendReportCommand
        {
            get { return send ?? (send = new RelayCommand(OnSendReport)); }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether should upload logs.
        /// </summary>
        public bool ShouldUploadLogs
        {
            get { return shouldUploadLogs; }

            set
            {
                shouldUploadLogs = value;
                OnPropertyChanged("ShouldUploadLogs");
            }
        }

        /// <summary>
        ///     Gets or sets the target.
        /// </summary>
        public string Target
        {
            get { return target; }

            set
            {
                target = value;
                OnPropertyChanged("Target");
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool isManaged)
        {
            if (isManaged)
            {
                cm = null;
                complaint = null;
                events = null;
            }

            base.Dispose(isManaged);
        }

        private void OnSendReport(object args)
        {
            var command =
                CommandDefinitions.CreateCommand(
                    "report", new List<string> {Target, Complaint}, CurrentTab).ToDictionary();

            events.GetEvent<UserCommandEvent>().Publish(command);
            IsOpen = !IsOpen;
            target = null;
            Complaint = null;
        }

        #endregion
    }
}