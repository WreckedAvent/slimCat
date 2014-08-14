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
    using SimpleJson;
    using System.Collections.Generic;
    using Utilities;

    public partial class ServerCommandService
    {
        private void ChannelInitializedCommand(IDictionary<string, object> command)
        {
            var channel = FindChannel(command);
            var mode = command.Get(Constants.Arguments.Mode).ToEnum<ChannelMode>();

            if (channel == null)
            {
                RequeueCommand(command);
                return;
            }

            channel.Mode = mode;
            var users = (JsonArray)command[Constants.Arguments.MultipleUsers];
            foreach (IDictionary<string, object> character in users)
            {
                var name = character.Get(Constants.Arguments.Identity);

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                channel.CharacterManager.SignOn(CharacterManager.Find(name));
            }
        }
    }
}
