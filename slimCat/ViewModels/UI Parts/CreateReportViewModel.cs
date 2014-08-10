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

    using Libraries;
    using Microsoft.Practices.Prism.Events;
    using Models;
    using System.Windows.Input;
    using Utilities;

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

        public CreateReportViewModel(IEventAggregator eventagg, IChatModel cm)
        {
            events = eventagg;
            this.cm = cm;
        }

        #endregion

        #region Public Properties

        public ICommand CloseCommand
        {
            get { return cancel ?? (cancel = new RelayCommand(param => IsOpen = !isOpen)); }
        }

        public string Complaint
        {
            get { return complaint; }

            set
            {
                complaint = value;
                OnPropertyChanged("Complaint");
            }
        }

        public string CurrentTab
        {
            get { return cm.CurrentChannel.Id == "Home" ? "None" : cm.CurrentChannel.Id; }
        }

        public bool IsOpen
        {
            get { return isOpen; }

            set
            {
                if (isOpen == value) return;

                Logging.LogLine((value ? "Opening" : "Closing") + " modal", "create report vm");
                isOpen = value;

                if (!value)
                    Complaint = null;

                OnPropertyChanged("IsOpen");
            }
        }

        public ICommand SendReportCommand
        {
            get { return send ?? (send = new RelayCommand(OnSendReport)); }
        }

        public bool ShouldUploadLogs
        {
            get { return shouldUploadLogs; }

            set
            {
                shouldUploadLogs = value;
                OnPropertyChanged("ShouldUploadLogs");
            }
        }

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
            events.SendUserCommand("report", new[] {target, complaint}, CurrentTab);

            IsOpen = !IsOpen;
            target = null;
            Complaint = null;
        }

        #endregion
    }
}