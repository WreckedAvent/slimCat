namespace Slimcat.Services
{
    using System.Collections.Generic;

    using Slimcat.Models;

    /// <summary>
    ///     The ChatConnection interface.
    /// </summary>
    public interface IChatConnection
    {
        #region Public Properties

        /// <summary>
        ///     Gets the account.
        /// </summary>
        IAccount Account { get; }

        /// <summary>
        ///     Gets the character.
        /// </summary>
        string Character { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        /// <param name="command_type">
        /// The command_type.
        /// </param>
        void SendMessage(object command, string command_type);

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="commandType">
        /// The command type.
        /// </param>
        void SendMessage(string commandType);

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        void SendMessage(IDictionary<string, object> command);

        #endregion
    }
}