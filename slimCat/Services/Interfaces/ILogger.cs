namespace Slimcat.Services
{
    using System.Collections.Generic;

    using Slimcat.Models;

    /// <summary>
    ///     The Logger interface.
    /// </summary>
    public interface ILogger
    {
        #region Public Methods and Operators

        /// <summary>
        /// Returns the last few messages from a given channel
        /// </summary>
        /// <param name="title">
        /// The Title.
        /// </param>
        /// <param name="id">
        /// The ID.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{T}"/>.
        /// </returns>
        IEnumerable<string> GetLogs(string title, string id);

        /// <summary>
        /// Logs a given message in a given channel
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="id">
        /// The ID.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        void LogMessage(string title, string id, IMessage message);

        /// <summary>
        /// Opens the log in the default text editor
        /// </summary>
        /// <param name="isFolder">
        /// The is Folder.
        /// </param>
        /// <param name="title">
        /// The Title.
        /// </param>
        /// <param name="id">
        /// The ID.
        /// </param>
        void OpenLog(bool isFolder, string title = null, string id = null);

        #endregion
    }
}