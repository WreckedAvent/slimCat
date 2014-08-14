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
        private void OnIgnoreUpdatesRequested(IDictionary<string, object> command)
        {
            var args = command.Get(Constants.Arguments.Character);

            var isAdd = !characterManager.IsOnList(args, ListKind.IgnoreUpdates);
            if (isAdd)
                characterManager.Add(args, ListKind.IgnoreUpdates);
            else
                characterManager.Remove(args, ListKind.IgnoreUpdates);
        }
    }
}
