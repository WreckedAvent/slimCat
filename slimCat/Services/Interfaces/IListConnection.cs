namespace Slimcat.Services
{
    using System.Collections.Generic;

    using Models;

    /// <summary>
    ///     Used for connectivity to F-list
    /// </summary>
    public interface IListConnection
    {
        #region Public Methods and Operators

        /// <summary>
        /// Uploads a lot to F-list.net f.e reporting a user
        /// </summary>
        /// <param name="report">
        /// relevant data about the report
        /// </param>
        /// <param name="log">
        /// the log to upload
        /// </param>
        /// <returns>
        /// an int corresonding to the logid the server assigned
        /// </returns>
        int UploadLog(ReportModel report, IEnumerable<IMessage> log);

        /// <summary>
        /// Gets an F-list API ticket
        /// </summary>
        /// <param name="sendUpdate">
        /// The send Update.
        /// </param>
        void GetTicket(bool sendUpdate);

        #endregion
    }
}