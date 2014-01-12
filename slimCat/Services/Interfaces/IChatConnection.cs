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

namespace Slimcat.Services
{
    #region Usings

    using System.Collections.Generic;
    using Models;

    #endregion

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
        ///     The send message.
        /// </summary>
        /// <param name="command">
        ///     The command.
        /// </param>
        /// <param name="commandType">
        ///     The command_type.
        /// </param>
        void SendMessage(object command, string commandType);

        /// <summary>
        ///     The send message.
        /// </summary>
        /// <param name="commandType">
        ///     The command type.
        /// </param>
        void SendMessage(string commandType);

        /// <summary>
        ///     The send message.
        /// </summary>
        /// <param name="command">
        ///     The command.
        /// </param>
        void SendMessage(IDictionary<string, object> command);

        #endregion
    }
}