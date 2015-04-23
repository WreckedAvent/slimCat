#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Events.cs">
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

namespace slimCat
{
    #region Usings

    using System.Collections.Generic;
    using Microsoft.Practices.Prism.Events;
    using Models;

    #endregion

    /// <summary>
    ///     This event is fired when the client should attempt to acquire an API ticket
    /// </summary>
    public class LoginEvent : CompositePresentationEvent<bool>
    {
    }

    /// <summary>
    ///     This event is fired when the client has finished attempting to get the API ticket
    /// </summary>
    public class LoginCompleteEvent : CompositePresentationEvent<bool>
    {
    }

    /// <summary>
    ///     This event is fired when the user has selected the character to login with
    /// </summary>
    public class CharacterSelectedLoginEvent : CompositePresentationEvent<string>
    {
    }

    /// <summary>
    ///     This event is fired when the server sends us a command
    /// </summary>
    public class ChatCommandEvent : CompositePresentationEvent<IDictionary<string, object>>
    {
    }

    /// <summary>
    ///     This event is fired when we are connected and the chat wrapper is displayed
    /// </summary>
    public class ChatOnDisplayEvent : CompositePresentationEvent<bool?>
    {
    }

    /// <summary>
    ///     This even is fired when we are confirmed logged into the server.
    /// </summary>
    public class LoginAuthenticatedEvent : CompositePresentationEvent<bool?>
    {
    }

    /// <summary>
    ///     This event is fired when we want to switch our active conversation tab
    /// </summary>
    public class RequestChangeTabEvent : CompositePresentationEvent<string>
    {
    }

    /// <summary>
    ///     This event is fired when the user enters in a valid command
    /// </summary>
    public class UserCommandEvent : CompositePresentationEvent<IDictionary<string, object>>
    {
    }

    /// <summary>
    ///     This event is fired when we have a new Pm in a non-focused channel
    /// </summary>
    public class NewPmEvent : CompositePresentationEvent<IMessage>
    {
    }

    /// <summary>
    ///     This event is fired when we have a new message in a non-focused channel
    /// </summary>
    public class NewMessageEvent : CompositePresentationEvent<IDictionary<string, object>>
    {
    }

    /// <summary>
    ///     This event is fired when we need to alert the UI about an update, such as a user's status change
    /// </summary>
    public class NewUpdateEvent : CompositePresentationEvent<NotificationModel>
    {
    }


    /// <summary>
    ///     This event is fired when the user has a tab flash or reads the last flashing tab.
    /// </summary>
    public class UnreadUpdatesEvent: CompositePresentationEvent<bool>
    {
    }

    /// <summary>
    ///     This event is used when the server has sent us an error or a module needs to display an error
    /// </summary>
    public class ErrorEvent : CompositePresentationEvent<string>
    {
    }

    /// <summary>
    ///     This event is used when our initial connection fails
    /// </summary>
    public class LoginFailedEvent : CompositePresentationEvent<string>
    {
    }

    /// <summary>
    ///     this event is used when the service layer is attempting a reconnect
    /// </summary>
    public class ReconnectingEvent : CompositePresentationEvent<int>
    {
    }

    /// <summary>
    ///     this event is fired when our established connection fails
    /// </summary>
    public class ConnectionClosedEvent : CompositePresentationEvent<bool>
    {
    }

    public class ChatSearchResultEvent : CompositePresentationEvent<bool>
    {
    }
}