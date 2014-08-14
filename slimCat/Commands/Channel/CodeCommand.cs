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
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using Utilities;

    public partial class UserCommandService
    {
        private void OnChannelCodeRequested(IDictionary<string, object> command)
        {
            if (model.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
            {
                events.GetEvent<ErrorEvent>().Publish("Home channel does not have a code.");
                return;
            }

            var toCopy = "[session={0}]{1}[/session]".FormatWith(
                model.CurrentChannel.Title,
                model.CurrentChannel.Id);

            Clipboard.SetData(DataFormats.Text, toCopy);
            events.GetEvent<ErrorEvent>().Publish("Channel's code copied to clipboard.");
        }
    }
}
