// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Events.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   This event is fired when the client should attempt to acquire an API ticket
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat
{
    using System.Collections.Generic;

    using Microsoft.Practices.Prism.Events;

    using Models;

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
    ///     This event is used when the server has sent us an error or a module needs to display an error
    /// </summary>
    public class ErrorEvent : CompositePresentationEvent<string>
    {
    }

    /// <summary>
    ///     This event is used when our intial connection fails
    /// </summary>
    public class LoginFailedEvent : CompositePresentationEvent<string>
    {
    }

    /// <summary>
    ///     this event is used when the service layer is attempting a reconnect
    /// </summary>
    public class ReconnectingEvent : CompositePresentationEvent<string>
    {
    }

    /// <summary>
    ///     this event is fired when our established connection fails
    /// </summary>
    public class ConnectionClosedEvent : CompositePresentationEvent<string>
    {
    }
}