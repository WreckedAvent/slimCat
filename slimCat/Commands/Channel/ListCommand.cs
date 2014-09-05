#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListCommand.cs">
//     Copyright (c) 2013, Justin Kadrovach, All rights reserved.
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
// --------------------------------------------------------------------------------------------------------------------

#endregion

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Web;
    using Models;
    using SimpleJson;
    using Utilities;

    #endregion

    public partial class ServerCommandService
    {
        private void ChannelListCommand(IDictionary<string, object> command, bool isPublic)
        {
            var arr = (JsonArray) command[Constants.Arguments.MultipleChannels];
            lock (ChatModel.AllChannels)
            {
                foreach (IDictionary<string, object> channel in arr)
                {
                    var name = channel.Get(Constants.Arguments.Name);
                    string title = null;
                    if (!isPublic)
                        title = HttpUtility.HtmlDecode(channel.Get(Constants.Arguments.Title));

                    var mode = ChannelMode.Both;
                    if (isPublic)
                        mode = channel.Get(Constants.Arguments.Mode).ToEnum<ChannelMode>();

                    var number = (long) channel[Constants.Arguments.MultipleCharacters];
                    if (number < 0)
                        number = 0;

                    var model = new GeneralChannelModel(name, isPublic ? ChannelType.Public : ChannelType.Private,
                        (int) number, mode)
                    {
                        Title = isPublic ? name : title
                    };

                    Dispatcher.Invoke((Action) (() =>
                    {
                        var current = ChatModel.AllChannels.FirstByIdOrNull(name);
                        if (current == null)
                        {
                            ChatModel.AllChannels.Add(model);
                            return;
                        }

                        current.Mode = mode;
                        current.UserCount = (int) number;
                    }));
                }
            }
        }

        private void PrivateChannelListCommand(IDictionary<string, object> command)
        {
            ChannelListCommand(command, false);
        }


        private void PublicChannelListCommand(IDictionary<string, object> command)
        {
            ChannelListCommand(command, true);
        }
    }
}