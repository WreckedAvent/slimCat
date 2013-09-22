namespace Slimcat.Models
{
    using System;

    /// <summary>
    ///     A model for storing data related to character reports
    /// </summary>
    public sealed class ReportModel : IDisposable
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets the call id.
        /// </summary>
        public string CallId { get; set; }

        /// <summary>
        ///     Gets or sets the complaint.
        /// </summary>
        public string Complaint { get; set; }

        /// <summary>
        ///     Gets or sets the log id.
        /// </summary>
        public int? LogId { get; set; }

        /// <summary>
        ///     Gets or sets the reported.
        /// </summary>
        public string Reported { get; set; }

        /// <summary>
        ///     Gets or sets the reporter.
        /// </summary>
        public ICharacter Reporter { get; set; }

        /// <summary>
        ///     Gets or sets the tab.
        /// </summary>
        public string Tab { get; set; }

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
            if (!isManaged)
            {
                return;
            }

            this.Complaint = null;
            this.Reporter = null;
        }

        #endregion
    }
}