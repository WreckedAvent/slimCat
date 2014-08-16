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
    using Models;
    using System.Collections.Generic;
    using System.Linq;
    using Utilities;

    public partial class UserCommandService
    {
        private static void ClearChannel(ChannelModel channel)
        {
            foreach (var item in channel.Messages)
                item.Dispose();

            foreach (var item in channel.Ads)
                item.Dispose();

            channel.Messages.Clear();
            channel.Ads.Clear();
        }

        private void OnClearAllRequested(IDictionary<string, object> command)
        {
            model.CurrentChannels
                .Cast<ChannelModel>()
                .Union(model.CurrentPms)
                .Each(ClearChannel);
        }

        private void OnClearRequested(IDictionary<string, object> command)
        {
            var targetName = command.Get(Constants.Arguments.Channel);
            var target = model.CurrentChannels
                .Cast<ChannelModel>()
                .Union(model.CurrentPms)
                .FirstOrDefault(x => x.Id.Equals(targetName, StringComparison.OrdinalIgnoreCase));

            ClearChannel(target);
        }
    }
}
