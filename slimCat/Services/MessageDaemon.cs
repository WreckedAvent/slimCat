// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageDaemon.cs" company="Justin Kadrovach">
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
//   The message daemon is the service layer responsible for managing what the user sees and the commands the user sends.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Threading;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Utilities;
    using Slimcat.ViewModels;
    using Slimcat.Views;

    /// <summary>
    ///     The message daemon is the service layer responsible for managing what the user sees and the commands the user sends.
    /// </summary>
    public class MessageDaemon : DispatcherObject, IChannelManager
    {
        #region Fields

        private readonly IListConnection api;

        private readonly IChatConnection connection;

        private readonly IUnityContainer container;

        private readonly IEventAggregator events;

        private readonly IChatModel model;

        private readonly IRegionManager region;

        private ChannelModel lastSelected;

        private ILogger logger;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDaemon"/> class.
        /// </summary>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="connection">
        /// The connection.
        /// </param>
        /// <param name="api">
        /// The api.
        /// </param>
        public MessageDaemon(
            IRegionManager regman, 
            IUnityContainer contain, 
            IEventAggregator events, 
            IChatModel model, 
            IChatConnection connection, 
            IListConnection api)
        {
            try
            {
                this.region = regman.ThrowIfNull("regman");
                this.container = contain.ThrowIfNull("contain");
                this.events = events.ThrowIfNull("events");
                this.model = model.ThrowIfNull("model");
                this.connection = connection.ThrowIfNull("connection");
                this.api = api.ThrowIfNull("api");

                this.model.SelectedChannelChanged += (s, e) => this.RequestNavigate(this.model.CurrentChannel.Id);

                this.events.GetEvent<ChatOnDisplayEvent>()
                    .Subscribe(this.BuildHomeChannel, ThreadOption.UIThread, true);
                this.events.GetEvent<RequestChangeTabEvent>()
                    .Subscribe(this.RequestNavigate, ThreadOption.UIThread, true);
                this.events.GetEvent<UserCommandEvent>().Subscribe(this.CommandRecieved, ThreadOption.UIThread, true);
            }
            catch (Exception ex)
            {
                ex.Source = "Message Daemon, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The add channel.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="ID">
        /// The id.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public void AddChannel(ChannelType type, string ID, string name)
        {
            if (type == ChannelType.PrivateMessage)
            {
                this.model.FindCharacter(ID).GetAvatar(); // make sure we have their picture

                // model doesn't have a reference to PrivateMessage channels, build it manually
                var temp = new PMChannelModel(this.model.FindCharacter(ID));
                this.container.RegisterInstance(temp.Id, temp);
                var pmChan = this.container.Resolve<PMChannelViewModel>(new ParameterOverride("name", temp.Id));

                this.Dispatcher.Invoke((Action)(() => this.model.CurrentPMs.Add(temp)));

                // then add it to the model's data
            }
            else
            {
                GeneralChannelModel temp;
                if (type == ChannelType.Utility)
                {
                    // our model won't have a reference to home, so we build it manually
                    temp = new GeneralChannelModel(ID, ChannelType.Utility);
                    this.container.RegisterInstance(ID, temp);
                    this.container.Resolve<UtilityChannelViewModel>(new ParameterOverride("name", ID));
                }
                else
                {
                    // our model should have a reference to other channels though
                    try
                    {
                        temp = this.model.AllChannels.First(param => param.Id == ID);
                    }
                    catch
                    {
                        temp = new GeneralChannelModel(ID, ChannelType.InviteOnly) { Title = name };
                        this.Dispatcher.Invoke((Action)(() => this.model.CurrentChannels.Add(temp)));
                    }

                    this.container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", ID));
                }

                if (!this.model.CurrentChannels.Contains(temp))
                {
                    this.Dispatcher.Invoke((Action)(() => this.model.CurrentChannels.Add(temp)));
                }
            }
        }

        /// <summary>
        /// The add message.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="channelName">
        /// The channel name.
        /// </param>
        /// <param name="poster">
        /// The poster.
        /// </param>
        /// <param name="messageType">
        /// The message type.
        /// </param>
        public void AddMessage(
            string message, string channelName, string poster, MessageType messageType = MessageType.Normal)
        {
            var sender = poster != "_thisCharacter"
                                    ? this.model.FindCharacter(poster)
                                    : this.model.FindCharacter(this.model.CurrentCharacter.Name);
            this.InstanceLogger();

            var channel = this.model.CurrentChannels.FirstByIdOrDefault(channelName)
                                   ?? (ChannelModel)this.model.CurrentPMs.FirstByIdOrDefault(channelName);

            if (channel == null)
            {
                return; // exception circumstance, swallow message
            }

            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        var thisMessage = new MessageModel(sender, message, messageType);

                        channel.AddMessage(thisMessage, this.model.IsOfInterest(sender.Name));

                        if (channel.Settings.LoggingEnabled && ApplicationSettings.AllowLogging)
                        {
                            // check if the user wants logging for this channel
                            this.logger.LogMessage(channel.Title, channel.Id, thisMessage);
                        }

                        if (poster == "_thisCharacter")
                        {
                            return;
                        }

                        // don't push events for our own messages
                        if (channel is GeneralChannelModel)
                        {
                            this.events.GetEvent<NewMessageEvent>()
                                .Publish(
                                    new Dictionary<string, object>
                                        {
                                            { "message", thisMessage }, 
                                            { "channel", channel }
                                        });
                        }
                        else
                        {
                            this.events.GetEvent<NewPMEvent>().Publish(thisMessage);
                        }
                    });
        }

        /// <summary>
        /// The command recieved.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        public void CommandRecieved(IDictionary<string, object> command)
        {
            var type = command["type"] as string;

            try
            {
                switch (type)
                {
                    case "priv":
                        {
                            var args = command["character"] as string;
                            if (args.Equals(this.model.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                this.events.GetEvent<ErrorEvent>().Publish("Hmmm... talking to yourself?");
                            }
                            else
                            {
                                // orderby ensures that our search string won't produce a premature
                                ICharacter guess =
                                    this.model.OnlineCharacters.OrderBy(character => character.Name)
                                        .FirstOrDefault(
                                            character => character.Name.ToLower().StartsWith(args.ToLower()));

                                this.JoinChannel(ChannelType.PrivateMessage, guess == null ? args : guess.Name);
                            }

                            return;
                        }

                        

                    case "PRI":
                        {
                            this.AddMessage(
                                command["message"] as string, command["recipient"] as string, "_thisCharacter");
                            break;
                        }

                        

                        #region Send Channel Message command

                    case "MSG":
                        {
                            this.AddMessage(
                                command["message"] as string, command["channel"] as string, "_thisCharacter");
                            break;
                        }

                        #endregion

                        #region Send Channel Ad command

                    case "LRP":
                        {
                            this.AddMessage(
                                command["message"] as string, 
                                command["channel"] as string, 
                                "_thisCharacter", 
                                MessageType.Ad);
                            break;
                        }

                        #endregion

                        #region Status Command

                    case "STA":
                        {
                            var statusmsg = command["statusmsg"] as string;
                            var status = (StatusType)Enum.Parse(typeof(StatusType), command["status"] as string, true);

                            this.model.CurrentCharacter.Status = status;
                            this.model.CurrentCharacter.StatusMessage = statusmsg;
                            break;
                        }

                        #endregion

                        #region Close Channel command

                    case "close":
                        {
                            var args = (string)command["channel"];
                            this.RemoveChannel(args);
                            return;
                        }

                        #endregion

                        #region Join Channel Command

                    case "join":
                        {
                            var args = (string)command["channel"];

                            if (this.model.CurrentChannels.FirstByIdOrDefault(args) != null)
                            {
                                this.RequestNavigate(args);
                                return;
                            }

                            // orderby ensures that our search string won't produce a premature
                            var guess =
                                this.model.AllChannels.OrderBy(channel => channel.Title)
                                    .FirstOrDefault(channel => channel.Title.ToLower().StartsWith(args.ToLower()));
                            if (guess != null)
                            {
                                var toSend = new { channel = guess.Id };
                                this.connection.SendMessage(toSend, "JCH");
                            }
                            else
                            {
                                var toSend = new { channel = args };
                                this.connection.SendMessage(toSend, "JCH");
                            }

                            return;
                        }

                        #endregion

                        #region Un/Ignore Command

                    case "IGN":
                        {
                            var args = command["character"] as string;

                            if ((string)command["action"] == "add")
                            {
                                this.model.Ignored.Add(args);
                            }
                            else if ((string)command["action"] == "delete")
                            {
                                this.model.Ignored.Remove(args);
                            }
                            else
                            {
                                break;
                            }

                            this.events.GetEvent<NewUpdateEvent>()
                                .Publish(
                                    new CharacterUpdateModel(
                                        this.model.FindCharacter(args), 
                                        new CharacterUpdateModel.ListChangedEventArgs
                                            {
                                                IsAdded =
                                                    this.model.Ignored
                                                        .Contains(args), 
                                                ListArgument =
                                                    CharacterUpdateModel
                                                    .ListChangedEventArgs
                                                    .ListType.Ignored
                                            }));
                            break;
                        }

                        #endregion

                        #region Clear Commands

                    case "clear":
                        {
                            foreach (var item in this.model.CurrentChannel.Messages)
                            {
                                item.Dispose();
                            }

                            foreach (var item in this.model.CurrentChannel.Ads)
                            {
                                item.Dispose();
                            }

                            this.model.CurrentChannel.History.Clear();
                            this.model.CurrentChannel.Messages.Clear();
                            this.model.CurrentChannel.Ads.Clear();
                            return;
                        }

                    case "clearall":
                        {
                            foreach (var channel in this.model.CurrentChannels)
                            {
                                foreach (var item in channel.Messages)
                                {
                                    item.Dispose();
                                }

                                foreach (var item in channel.Ads)
                                {
                                    item.Dispose();
                                }

                                channel.History.Clear();
                                channel.Messages.Clear();
                                channel.Ads.Clear();
                            }

                            foreach (var pm in this.model.CurrentPMs)
                            {
                                foreach (var item in pm.Messages)
                                {
                                    item.Dispose();
                                }

                                pm.History.Clear();
                                pm.Messages.Clear();
                            }

                            return;
                        }

                        #endregion

                        #region Logger Commands

                    case "_logger_new_line":
                        {
                            this.InstanceLogger();
                            this.logger.LogSpecial(
                                this.model.CurrentChannel.Title, 
                                this.model.CurrentChannel.Id, 
                                SpecialLogMessageKind.LineBreak, 
                                string.Empty);

                            this.events.GetEvent<ErrorEvent>().Publish("Logged a new line.");
                            return;
                        }

                    case "_logger_new_header":
                        {
                            this.InstanceLogger();
                            this.logger.LogSpecial(
                                this.model.CurrentChannel.Title, 
                                this.model.CurrentChannel.Id, 
                                SpecialLogMessageKind.Header, 
                                command["title"] as string);

                            this.events.GetEvent<ErrorEvent>()
                                .Publish("Logged a header of \'" + command["title"] + "\'");
                            return;
                        }

                    case "_logger_new_section":
                        {
                            this.InstanceLogger();
                            this.logger.LogSpecial(
                                this.model.CurrentChannel.Title, 
                                this.model.CurrentChannel.Id, 
                                SpecialLogMessageKind.Section, 
                                command["title"] as string);

                            this.events.GetEvent<ErrorEvent>()
                                .Publish("Logged a section of \'" + command["title"] + "\'");
                            return;
                        }

                    case "_logger_open_log":
                        {
                            this.InstanceLogger();

                            this.logger.OpenLog(
                                false, this.model.CurrentChannel.Title, this.model.CurrentChannel.Id);
                            return;
                        }

                    case "_logger_open_folder":
                        {
                            this.InstanceLogger();
                            if (command.ContainsKey("channel"))
                            {
                                var toOpen = command["channel"] as string;
                                if (!string.IsNullOrWhiteSpace(toOpen))
                                {
                                    this.logger.OpenLog(true, toOpen, toOpen);
                                }
                            }
                            else
                            {
                                this.logger.OpenLog(
                                    true, this.model.CurrentChannel.Title, this.model.CurrentChannel.Id);
                            }

                            return;
                        }

                        #endregion

                        #region Code Command

                    case "code":
                        {
                            if (this.model.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
                            {
                                this.events.GetEvent<ErrorEvent>().Publish("Home channel does not have a code.");
                                return;
                            }

                            string toCopy = string.Format(
                                "[session={0}]{1}[/session]", 
                                this.model.CurrentChannel.Title, 
                                this.model.CurrentChannel.Id);
                            Clipboard.SetData(DataFormats.Text, toCopy);
                            this.events.GetEvent<ErrorEvent>().Publish("Channel's code copied to clipboard.");
                            return;
                        }

                        #endregion

                        #region Notification Snap To

                    case "_snap_to_last_update":
                        {
                            string target = null;
                            string kind = null;

                            Action showMyDamnWindow = () =>
                                {
                                    Application.Current.MainWindow.Show();
                                    if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                                    {
                                        Application.Current.MainWindow.WindowState = WindowState.Normal;
                                    }

                                    Application.Current.MainWindow.Focus();
                                };

                            if (command.ContainsKey("target"))
                            {
                                target = command["target"] as string;
                            }

                            if (command.ContainsKey("kind"))
                            {
                                kind = command["kind"] as string;
                            }

                            // first off, see if we have a target defined. If we do, then let's see if it's one of our current channels
                            if (target != null)
                            {
                                if (target.StartsWith("http://"))
                                {
                                    // if our target is a command to get the latest link-able thing, let's grab that
                                    Process.Start(target);
                                    return;
                                }

                                if (kind != null && kind.Equals("report"))
                                {
                                    command.Clear();
                                    command["name"] = target;

                                    goto case "handlereport";
                                }

                                var guess = this.model.CurrentPMs.FirstByIdOrDefault(target);
                                if (guess != null)
                                {
                                    this.RequestNavigate(target); // join the PM tab
                                    this.Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }

                                var secondGuess =
                                    this.model.CurrentChannels.FirstByIdOrDefault(target);

                                if (secondGuess != null)
                                {
                                    this.RequestNavigate(target);

                                    // if our second guess is accurate, join the channel
                                    this.Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }
                            }

                            var latest = this.model.Notifications.LastOrDefault();

                            // if we got to this point our notification is doesn't involve an active tab
                            if (latest != null)
                            {
                                var with = latest as CharacterUpdateModel;
                                if (with != null)
                                {
                                    // so tell our system to join the PM Tab
                                    var doStuffWith = with;
                                    this.JoinChannel(ChannelType.PrivateMessage, doStuffWith.TargetCharacter.Name);

                                    this.Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }

                                var stuffWith = latest as ChannelUpdateModel;
                                if (stuffWith != null)
                                {
                                    // or the channel tab
                                    // I'm not really sure how we can get a notification on a channel we're not in,
                                    // but there's no reason to crash if that is the case
                                    var doStuffWith = stuffWith;
                                    var channel =
                                        this.model.AllChannels.FirstByIdOrDefault(doStuffWith.ChannelID);

                                    if (channel == null)
                                    {
                                        // assume it's an invite
                                        var toSend = new { channel = doStuffWith.ChannelID };
                                        this.connection.SendMessage(toSend, "JCH");

                                        // tell the server to jump on that shit
                                        this.Dispatcher.Invoke(showMyDamnWindow);
                                        return;
                                    }

                                    var chanType = channel.Type;
                                    this.JoinChannel(chanType, doStuffWith.ChannelID);
                                    this.Dispatcher.Invoke(showMyDamnWindow);
                                    return;
                                }
                            }

                            return;
                        }

                        #endregion

                        #region Comic invite error message

                    case "CIU":
                        {
                            if (command.ContainsKey("character")
                                && (command["character"] as string).Equals(
                                    this.model.CurrentCharacter.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                this.events.GetEvent<ErrorEvent>()
                                    .Publish("For the record, no, inviting yourself does not a party make.");
                                return;
                            }

                            break;
                        }

                        #endregion

                        #region Comic who message

                    case "who":
                        {
                            this.events.GetEvent<ErrorEvent>()
                                .Publish(
                                    "Server, server, across the sea,\nWho is connected, most to thee?\nWhy, "
                                    + this.model.CurrentCharacter.Name + " is!");
                            return;
                        }

                        #endregion

                        #region GetDescription command

                    case "getdescription":
                        {
                            if (this.model.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
                            {
                                this.events.GetEvent<ErrorEvent>()
                                    .Publish("Poor home channel, with no description to speak of...");
                                return;
                            }

                            if (this.model.CurrentChannel is GeneralChannelModel)
                            {
                                Clipboard.SetData(
                                    DataFormats.Text, (this.model.CurrentChannel as GeneralChannelModel).Description);
                                this.events.GetEvent<ErrorEvent>()
                                    .Publish("Channel's description copied to clipboard.");
                            }
                            else
                            {
                                this.events.GetEvent<ErrorEvent>().Publish("Hey! That's not a channel.");
                            }

                            return;
                        }

                        #endregion

                        #region Interesting/Uninteresting command

                    case "interesting":
                        {
                            var args = command["character"] as string;
                            bool isAdd = !this.model.Interested.Contains(args);

                            this.model.ToggleInterestedMark(args);

                            this.events.GetEvent<NewUpdateEvent>()
                                .Publish(
                                    new CharacterUpdateModel(
                                        this.model.FindCharacter(args), 
                                        new CharacterUpdateModel.ListChangedEventArgs
                                            {
                                                IsAdded = isAdd, 
                                                ListArgument =
                                                    CharacterUpdateModel
                                                    .ListChangedEventArgs
                                                    .ListType.Interested
                                            }));
                            return;
                        }

                    case "notinteresting":
                        {
                            var args = command["character"] as string;

                            bool isAdd = true;

                            if (this.model.NotInterested.Contains(args))
                            {
                                isAdd = false;
                            }

                            this.model.ToggleNotInterestedMark(args);

                            this.events.GetEvent<NewUpdateEvent>()
                                .Publish(
                                    new CharacterUpdateModel(
                                        this.model.FindCharacter(args), 
                                        new CharacterUpdateModel.ListChangedEventArgs
                                            {
                                                IsAdded = isAdd, 
                                                ListArgument =
                                                    CharacterUpdateModel
                                                    .ListChangedEventArgs
                                                    .ListType.NotInterested
                                            }));
                            return;
                        }

                        #endregion

                        #region Report command

                    case "SFC":
                        {
                            if (!command.ContainsKey("report"))
                            {
                                command.Add("report", string.Empty);
                            }

                            int logId = -1; // no log

                            // report format: "Current Tab/Channel: <channel> | Reporting User: <reported user> | <report body>
                            string reportText = string.Format(
                                "Current Tab/Channel: {0} | Reporting User: {1} | {2}", 
                                command["channel"] as string, 
                                command["name"] as string, 
                                command["report"] as string);

                            // upload log
                            var channelText = command["channel"] as string;
                            if (!string.IsNullOrWhiteSpace(channelText) && !channelText.Equals("None"))
                            {
                                // we could just use _model.SelectedChannel, but the user might change tabs immediately after reporting, creating a race condition
                                ChannelModel channel;
                                if (channelText == command["name"] as string)
                                {
                                    channel = this.model.CurrentPMs.FirstByIdOrDefault(channelText);
                                }
                                else
                                {
                                    channel = this.model.CurrentChannels.FirstByIdOrDefault(channelText);
                                }

                                if (channel != null)
                                {
                                    var report = new ReportModel
                                                     {
                                                         Reporter = this.model.CurrentCharacter, 
                                                         Reported = command["name"] as string, 
                                                         Complaint = command["report"] as string, 
                                                         Tab = channelText
                                                     };

                                    logId = this.api.UploadLog(report, channel.Messages);
                                }
                            }

                            command.Remove("name");
                            command["report"] = reportText;
                            command["logid"] = logId;

                            if (!command.ContainsKey("action"))
                            {
                                command["action"] = "report";
                            }

                            break;
                        }

                        #endregion

                        #region Temp ignore/unignore

                    case "tempignore":
                    case "tempunignore":
                        {
                            string character = (command["character"] as string).ToLower().Trim();
                            bool add = type == "tempignore";

                            if (add && !this.model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                            {
                                this.model.Ignored.Add(character);
                            }

                            if (!add && this.model.Ignored.Contains(character, StringComparer.OrdinalIgnoreCase))
                            {
                                this.model.Ignored.Remove(character);
                            }

                            return;
                        }

                        #endregion

                        #region Latest report

                    case "handlelatest":
                        {
                            command.Clear();
                            var latest = (from n in this.model.Notifications
                                                           where
                                                               n is CharacterUpdateModel
                                                               && (n as CharacterUpdateModel).Arguments is
                                                                  CharacterUpdateModel.ReportFiledEventArgs
                                                           select n as CharacterUpdateModel).FirstOrDefault();

                            if (latest == null)
                            {
                                return;
                            }

                            var args = latest.Arguments as CharacterUpdateModel.ReportFiledEventArgs;

                            command.Add("type", "SFC");
                            command.Add("callid", args.CallId);
                            command.Add("action", "confirm");

                            this.JoinChannel(ChannelType.PrivateMessage, latest.TargetCharacter.Name);

                            int logId = -1;
                            if (command.ContainsKey("logid"))
                            {
                                int.TryParse(command["logid"] as string, out logId);
                            }

                            if (logId != -1)
                            {
                                Process.Start(Constants.UrlConstants.ReadLog + logId);
                            }

                            break;
                        }

                    case "handlereport":
                        {
                            if (command.ContainsKey("name"))
                            {
                                var target = this.model.FindCharacter(command["name"] as string);

                                if (!target.HasReport)
                                {
                                    this.events.GetEvent<ErrorEvent>()
                                        .Publish("Cannot find report for specified character!");
                                    return;
                                }

                                command["type"] = "SFC";
                                command.Add("callid", target.LastReport.CallId);
                                if (!command.ContainsKey("action"))
                                {
                                    command["action"] = "confirm";
                                }

                                this.JoinChannel(ChannelType.PrivateMessage, target.Name);

                                var logId = -1;
                                if (command.ContainsKey("logid"))
                                {
                                    int.TryParse(command["logid"] as string, out logId);
                                }

                                if (logId != -1)
                                {
                                    Process.Start(Constants.UrlConstants.ReadLog + logId);
                                }

                                break;
                            }
                            goto case "handlelatest";
                        }

                        #endregion
                }

                this.connection.SendMessage(command);
            }
            catch (Exception ex)
            {
                ex.Source = "Message Daemon, command received";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        /// The join channel.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public void JoinChannel(ChannelType type, string id, string name = "")
        {
            this.InstanceLogger();

            IEnumerable<string> history = new List<string>();
            if (!id.Equals("Home"))
            {
                history = this.logger.GetLogs(string.IsNullOrWhiteSpace(name) ? id : name, id).ToList();
            }

            var toJoin = this.model.CurrentPMs.FirstByIdOrDefault(id)
                         ?? (ChannelModel)this.model.CurrentChannels.FirstByIdOrDefault(id);

            if (toJoin == null)
            {
                this.AddChannel(type, id, name);

                toJoin = this.model.CurrentPMs.FirstByIdOrDefault(id)
                         ?? (ChannelModel)this.model.CurrentChannels.FirstByIdOrDefault(id);
            }

            if (history.Any())
            {
                this.Dispatcher.BeginInvoke(
                    (Action)delegate
                        {
                            toJoin.History.Clear();
                            foreach (var item in history)
                            {
                                toJoin.History.Add(item);
                            }
                        });
            }

            this.RequestNavigate(id);
        }

        /// <summary>
        /// The remove channel.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        public void RemoveChannel(string name)
        {
            this.RequestNavigate("Home");

            if (this.model.CurrentChannels.Any(param => param.Id == name))
            {
                var temp = this.model.CurrentChannels.First(param => param.Id == name);

                var vm = this.container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", temp.Id));
                vm.Dispose();

                this.Dispatcher.Invoke(
                    (Action)delegate
                        {
                            this.model.CurrentChannels.Remove(temp);
                            temp.Dispose();
                        });

                object toSend = new { channel = name };
                this.connection.SendMessage(toSend, "LCH");
            }
            else if (this.model.CurrentPMs.Any(param => param.Id == name))
            {
                var temp = this.model.CurrentPMs.First(param => param.Id == name);

                var vm = this.container.Resolve<PMChannelViewModel>(new ParameterOverride("name", temp.Id));
                vm.Dispose();

                this.model.CurrentPMs.Remove(temp);
                temp.Dispose();
            }
            else
            {
                throw new ArgumentOutOfRangeException("name", "Could not find the channel requested to remove");
            }
        }

        /// <summary>
        /// The request navigate.
        /// </summary>
        /// <param name="channelId">
        /// The channel id.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        private void RequestNavigate(string channelId)
        {
            if (this.lastSelected != null)
            {
                if (this.lastSelected.Id.Equals(channelId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                this.Dispatcher.Invoke(
                    (Action)delegate
                        {
                            var toUpdate = this.model.CurrentChannels.FirstByIdOrDefault(this.lastSelected.Id)
                                           ?? (ChannelModel)this.model.CurrentPMs.FirstByIdOrDefault(this.lastSelected.Id);

                            if (toUpdate == null)
                            {
                                throw new ArgumentOutOfRangeException("channelId", "Cannot update unknown channel");
                            }

                            toUpdate.IsSelected = false;
                        });
            }

            var channelModel = this.model.CurrentChannels.FirstByIdOrDefault(channelId)
                               ?? (ChannelModel)this.model.CurrentPMs.FirstByIdOrDefault(channelId);

            if (channelModel == null)
            {
                throw new ArgumentOutOfRangeException("channelId", "Cannot navigate to unknown channel");
            }

            channelModel.IsSelected = true;
            this.model.CurrentChannel = channelModel;

            this.Dispatcher.Invoke(
                (Action)delegate
                    {
                        foreach (var r in this.region.Regions[ChatWrapperView.ConversationRegion].Views)
                        {
                            var view = r as DisposableView;
                            if (view != null)
                            {
                                var toDispose = view;
                                toDispose.Dispose();
                                this.region.Regions[ChatWrapperView.ConversationRegion].Remove(toDispose);
                            }
                            else
                            {
                                this.region.Regions[ChatWrapperView.ConversationRegion].Remove(r);
                            }
                        }

                        this.region.Regions[ChatWrapperView.ConversationRegion].RequestNavigate(
                            HelperConverter.EscapeSpaces(channelModel.Id));
                    });

            this.lastSelected = channelModel;
        }

        #endregion

        #region Methods

        private void BuildHomeChannel(bool? payload)
        {
            this.events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(this.BuildHomeChannel);

            // we shouldn't need to know about this anymore
            this.JoinChannel(ChannelType.Utility, "Home");
        }

        // ensure that our logger has a proper instance
        private void InstanceLogger()
        {
            if (this.logger == null)
            {
                this.logger = new LoggingDaemon(this.model.CurrentCharacter.Name);
            }
        }

        #endregion
    }
}