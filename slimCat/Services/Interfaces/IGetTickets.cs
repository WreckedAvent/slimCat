﻿#region Copyright

// <copyright file="IGetTickets.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
// 
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>

#endregion

namespace slimCat.Services
{
    #region Usings

    using Models;

    #endregion

    /// <summary>
    ///     Represents a service for getting our api tickets.
    /// </summary>
    public interface IGetTickets
    {
        /// <summary>
        ///     Gets the ticket.
        /// </summary>
        string Ticket { get; }

        /// <summary>
        ///     Gets the account.
        /// </summary>
        IAccount Account { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the next ticket retrieval should force a new one.
        /// </summary>
        bool ShouldGetNewTicket { get; set; }

        /// <summary>
        ///     Sets the credentials for getting a new ticket.
        /// </summary>
        void SetCredentials(string user, string pass);
    }
}