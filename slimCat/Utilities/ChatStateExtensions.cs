#region Copyright

// <copyright file="ChatStateExtensions.cs">
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

#region Usings

using slimCat.Models;
using slimCat.Services;
using slimCat.Utilities;

#endregion

internal static class ChatStateExtensions
{
    public static ChannelSettingsModel GetChannelSettingsById(this IChatState chatState, string id)
    {
        var channel = chatState.GetChannelById(id);

        return channel?.Settings;
    }

    public static GeneralChannelModel GetChannelById(this IChatState chatState, string id)
        => chatState.ChatModel.AllChannels.FirstByIdOrNull(id);

    public static bool IsInteresting(this IChatState chatState, string character, bool onlineOnly = false)
        => chatState.CharacterManager.IsOfInterest(character, onlineOnly) || chatState.ChatModel.CurrentPms.FirstByIdOrNull(character) != null;
}