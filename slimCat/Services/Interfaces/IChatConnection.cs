#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IChatConnection.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;

    #endregion

    /// <summary>
    ///     Represents a websocket endpoint for F-Chat.
    /// </summary>
    public interface IChatConnection
    {
        #region Public Properties

        /// <summary>
        ///     Gets the current account.
        /// </summary>
        IAccount Account { get; }

        /// <summary>
        ///     Gets the current character.
        /// </summary>
        string Character { get; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Sends the message to the server.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="commandType">Type of the command.</param>
        void SendMessage(object command, string commandType);

        /// <summary>
        ///     Sends an argumentless command to the server.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        void SendMessage(string commandType);


        /// <summary>
        ///     Sends the message to the server.
        /// </summary>
        /// <param name="command">The command.</param>
        void SendMessage(IDictionary<string, object> command);


        /// <summary>
        ///     Disconnects the current socket.
        /// </summary>
        void Disconnect();

        #endregion
    }
}