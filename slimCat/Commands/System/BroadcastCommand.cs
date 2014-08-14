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

namespace slimCat.Models
{
    using System.Linq;

    public class BroadcastEventArgs : CharacterUpdateEventArgs
    {
        public string Message { get; set; }

        public override string ToString()
        {
            return "broadcasted " + Message + (char.IsPunctuation(Message.Last()) ? string.Empty : ".");
        }

    }
}

namespace slimCat.Services
{
    using Models;
    using System;
    using System.Collections.Generic;
    using Utilities;

    public partial class ServerCommandService
    {
        private void BroadcastCommand(IDictionary<string, object> command)
        {
            var message = command.Get(Constants.Arguments.Message);

            if (!command.ContainsKey(Constants.Arguments.Character))
            {
                ErrorCommand(command);
                return;
            }

            var posterName = command.Get(Constants.Arguments.Character);
            var poster = CharacterManager.Find(posterName);

            // message should be in the format:
            // [b]Broadcast from username:[/b] message
            // but this is redundant with slimCat, so cut out the first bit
            var indexOfClosingTag = message.IndexOf("[/b]", StringComparison.OrdinalIgnoreCase);

            if (indexOfClosingTag != -1)
                message = message.Substring(indexOfClosingTag + "[/b] ".Length);

            Events.NewCharacterUpdate(poster, new BroadcastEventArgs { Message = message });
        }
    }
}
