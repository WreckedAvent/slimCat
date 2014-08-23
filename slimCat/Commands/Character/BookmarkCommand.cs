#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BroadcastCommand.cs">
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
    using Models;
    using System.Collections.Generic;
    using Utilities;

    public partial class UserCommandService
    {
        // implementation is in f-list connection, these just prevent us from sending junk to chat server
        // HACK: we have to assume success due to an RTB not being sent on the backend
        private void OnBookmarkAddRequested(IDictionary<string, object> command)
        {
            DoListAction(command.Get(Constants.Arguments.Name), ListKind.Bookmark, true);
        }

        private void OnBookmarkRemoveRequested(IDictionary<string, object> command)
        {
            DoListAction(command.Get(Constants.Arguments.Name), ListKind.Bookmark, false);
        }
    }
}
