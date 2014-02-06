#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TicketProvider.cs">
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

    using System;
    using System.Threading.Tasks;
    using Models;

    #endregion

    internal class TicketProvider : ITicketProvider
    {
        public Task<string> GetTicketAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IAccount> GetAccountAsync()
        {
            throw new NotImplementedException();
        }
    }
}