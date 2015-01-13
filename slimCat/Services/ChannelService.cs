#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelService.cs">
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
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using System.Windows.Threading;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Models;
    using Utilities;
    using ViewModels;
    using Views;
    using Commands = Utilities.Constants.ClientCommands;

    #endregion

    /// <summary>
    ///     The message daemon is the service layer responsible for managing what the user sees and the commands the user
    ///     sends.
    /// </summary>
    public class ChannelService : DispatcherObject, IChannelService
    {
        #region Fields

        private const int MaxRecentTabs = 30;

        private readonly IAutomationService automation;

        private readonly ICharacterManager characterManager;

        private readonly IChatConnection connection;

        private readonly IUnityContainer container;

        private readonly IEventAggregator events;

        private readonly ILoggingService logger;

        private readonly IChatModel model;

        private readonly IRegionManager region;

        private ChannelModel lastSelected;

        #endregion

        #region Constructors and Destructors

        public ChannelService(
            IRegionManager regman,
            IUnityContainer contain,
            IEventAggregator events,
            IChatModel model,
            IChatConnection connection,
            ICharacterManager manager,
            ILoggingService logger,
            IAutomationService automation)
        {
            try
            {
                region = regman.ThrowIfNull("regman");
                container = contain.ThrowIfNull("contain");
                this.events = events.ThrowIfNull("events");
                this.model = model.ThrowIfNull("model");
                this.connection = connection.ThrowIfNull("connection");
                this.logger = logger.ThrowIfNull("logger");
                this.automation = automation.ThrowIfNull("automation");
                characterManager = manager.ThrowIfNull("characterManager");

                this.events.GetEvent<ChatOnDisplayEvent>().Subscribe(BuildHomeChannel, ThreadOption.UIThread, true);
                this.events.GetEvent<RequestChangeTabEvent>().Subscribe(RequestNavigate, ThreadOption.UIThread, true);
            }
            catch (Exception ex)
            {
                ex.Source = "Message Daemon, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Methods and Operators

        public void AddChannel(ChannelType type, string id, string name)
        {
            Log((id != name && !string.IsNullOrEmpty(name))
                ? "Making {0} Channel {1} \"{2}\"".FormatWith(type, id, name)
                : "Making {0} Channel {1}".FormatWith(type, id));

            if (type == ChannelType.PrivateMessage)
            {
                var character = characterManager.Find(id);

                character.GetAvatar(); // make sure we have their picture

                // model doesn't have a reference to PrivateMessage channels, build it manually
                var temp = new PmChannelModel(character);
                container.RegisterInstance(temp.Id, temp);
                container.Resolve<PmChannelViewModel>(new ParameterOverride("name", temp.Id));

                Dispatcher.Invoke((Action) (() => model.CurrentPms.Add(temp)));
                // then add it to the model's data               

                ApplicationSettings.RecentCharacters.BacklogWithUpdate(id, MaxRecentTabs);
                SettingsService.SaveApplicationSettingsToXml(model.CurrentCharacter.Name);
            }
            else
            {
                GeneralChannelModel temp;
                if (type == ChannelType.Utility)
                {
                    // our model won't have a reference to home, so we build it manually
                    temp = new GeneralChannelModel(id, ChannelType.Utility);
                    container.RegisterInstance(id, temp);
                    container.Resolve<HomeChannelViewModel>(new ParameterOverride("name", id));
                }
                else
                {
                    // our model should have a reference to other channels though
                    temp = model.AllChannels.FirstOrDefault(param => param.Id == id);

                    if (temp == null)
                    {
                        temp = new GeneralChannelModel(id, name, id == name ? ChannelType.Public : ChannelType.Private);
                        Dispatcher.Invoke((Action) (() => model.AllChannels.Add(temp)));
                    }

                    Dispatcher.Invoke((Action) (() => model.CurrentChannels.Add(temp)));

                    container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", id));

                    ApplicationSettings.RecentChannels.BacklogWithUpdate(id, MaxRecentTabs);
                    SettingsService.SaveApplicationSettingsToXml(model.CurrentCharacter.Name);
                }

                if (!model.CurrentChannels.Contains(temp))
                    Dispatcher.Invoke((Action) (() => model.CurrentChannels.Add(temp)));
            }
        }

        public void AddMessage(
            string message, string channelName, string poster, MessageType messageType = MessageType.Normal)
        {
            var sender =
                characterManager.Find(poster == Constants.Arguments.ThisCharacter ? model.CurrentCharacter.Name : poster);

            var channel = model.CurrentChannels.FirstByIdOrNull(channelName)
                          ?? (ChannelModel) model.CurrentPms.FirstByIdOrNull(channelName);

            if (channel == null)
                return; // exception circumstance, swallow message

            if (messageType == MessageType.Ad && characterManager.IsOnList(poster, ListKind.NotInterested, false))
                return; // don't want these clogging up our filter or.. anything really

            Dispatcher.Invoke(
                (Action) delegate
                {
                    var thisMessage = new MessageModel(sender, message, messageType);

                    channel.AddMessage(thisMessage, characterManager.IsOfInterest(poster));

                    if (channel.Settings.LoggingEnabled && ApplicationSettings.AllowLogging)
                    {
                        // check if the user wants logging for this channel
                        logger.LogMessage(channel.Title, channel.Id, thisMessage);
                    }

                    if (poster == Constants.Arguments.ThisCharacter)
                        return;

                    // don't push events for our own messages
                    if (channel is GeneralChannelModel)
                    {
                        events.GetEvent<NewMessageEvent>()
                            .Publish(
                                new Dictionary<string, object>
                                {
                                    {Constants.Arguments.Message, thisMessage},
                                    {Constants.Arguments.Channel, channel}
                                });
                    }
                    else
                        events.GetEvent<NewPmEvent>().Publish(thisMessage);
                });
        }

        public void JoinChannel(ChannelType type, string id, string name = "")
        {
            var originalId = id;
            if (id.EndsWith("/notes"))
            {
                id = id.Substring(0, id.Length - "/notes".Length);
            }

            if (id.EndsWith("/profile"))
            {
                id = id.Substring(0, id.Length - "/profile".Length);
            }

            if (model.CurrentChannel != null && model.CurrentChannel.Id.Equals(id))
                return;

            name = HttpUtility.HtmlDecode(name);
            IEnumerable<string> history = new List<string>();

            Log((id != name && !string.IsNullOrEmpty(name))
                ? "Joining {0} Channel {1} \"{2}\"".FormatWith(type, id, name)
                : "Joining {0} Channel {1}".FormatWith(type, id));

            if (!id.Equals("Home"))
                history = logger.GetLogs(string.IsNullOrWhiteSpace(name) ? id : name, id);

            var toJoin = model.CurrentPms.FirstByIdOrNull(id)
                         ?? (ChannelModel) model.CurrentChannels.FirstByIdOrNull(id);

            if (toJoin == null)
            {
                AddChannel(type, id, name);

                toJoin = model.CurrentPms.FirstByIdOrNull(id)
                         ?? (ChannelModel) model.CurrentChannels.FirstByIdOrNull(id);
            }

            if (history.Any()
                && history.Count() > 1
                && toJoin.Messages.Count == 0
                && toJoin.Ads.Count == 0)
            {
                Dispatcher.BeginInvoke(
                    (Action) (() => history.Select(item => new MessageModel(item)).Each(
                        item =>
                        {
                            if (item.Type != MessageType.Normal)
                                toJoin.Ads.Add(item);
                            else
                                toJoin.Messages.Add(item);
                        })));
            }

            RequestNavigate(originalId);
        }

        public void RemoveChannel(string name, bool force, bool isServer)
        {
            Log("Removing Channel " + name);

            if (model.CurrentChannels.Any(param => param.Id == name))
            {
                var temp = model.CurrentChannels.FirstByIdOrNull(name);
                temp.Description = null;

                var index = model.CurrentChannels.IndexOf(temp);
                RequestNavigate(model.CurrentChannels[index - 1].Id);

                Dispatcher.Invoke(
                    (Action) (() => model.CurrentChannels.Remove(temp)));

                if (isServer) return;

                object toSend = new {channel = name};
                connection.SendMessage(toSend, Commands.ChannelLeave);
            }
            else if (model.CurrentPms.Any(param => param.Id == name))
            {
                var temp = model.CurrentPms.FirstByIdOrNull(name);
                var index = model.CurrentPms.IndexOf(temp);
                RequestNavigate(index != 0 ? model.CurrentPms[index - 1].Id : "Home");

                model.CurrentPms.Remove(temp);
            }
            else if (force)
            {
                object toSend = new {channel = name};
                connection.SendMessage(toSend, Commands.ChannelLeave);
            }
            else
                throw new ArgumentOutOfRangeException("name", "Could not find the channel requested to remove");
        }

        public void QuickJoinChannel(string id, string name)
        {
            name = HttpUtility.HtmlDecode(name);

            var type = (id != name) ? ChannelType.Public : ChannelType.Private;

            Log((id != name && !string.IsNullOrEmpty(name))
                ? "Quick Joining {0} Channel {1} \"{2}\"".FormatWith(type, id, name)
                : "Quick Joining {0} Channel {1}".FormatWith(type, id));

            var temp = new GeneralChannelModel(id, name, id == name ? ChannelType.Public : ChannelType.Private);
            Dispatcher.Invoke((Action) (() =>
            {
                model.AllChannels.Add(temp);
                model.CurrentChannels.Add(temp);
            }));

            container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", id));
        }

        public void RemoveChannel(string name)
        {
            RemoveChannel(name, false, false);
        }

        #endregion

        #region Methods

        private void BuildHomeChannel(bool? payload)
        {
            events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(BuildHomeChannel);

            // we shouldn't need to know about this anymore
            JoinChannel(ChannelType.Utility, "Home");
        }

        private void RequestNavigate(string channelId)
        {
            automation.UserDidAction();

            Log("Requested " + channelId);

            var wantsNoteView = false;
            var wantsProfileView = false;
            if (channelId.EndsWith("/notes"))
            {
                channelId = channelId.Substring(0, channelId.Length - "/notes".Length);
                wantsNoteView = true;
            }
            if (channelId.EndsWith("/profile"))
            {
                channelId = channelId.Substring(0, channelId.Length - "/profile".Length);
                wantsProfileView = true;
            }

            if (lastSelected != null)
            {
                if (lastSelected.Id.Equals(channelId, StringComparison.OrdinalIgnoreCase))
                    return;

                Dispatcher.Invoke(
                    (Action) delegate
                    {
                        var toUpdate = model.CurrentChannels.FirstByIdOrNull(lastSelected.Id)
                                       ??
                                       (ChannelModel) model.CurrentPms.FirstByIdOrNull(lastSelected.Id);

                        if (toUpdate == null)
                            lastSelected = null;
                        else
                            toUpdate.IsSelected = false;
                    });
            }

            var channelModel = model.CurrentChannels.FirstByIdOrNull(channelId)
                               ?? (ChannelModel) model.CurrentPms.FirstByIdOrNull(channelId);

            if (channelModel == null)
                throw new ArgumentOutOfRangeException("channelId", "Cannot navigate to unknown channel");

            var pmChannelModel = channelModel as PmChannelModel;
            if (pmChannelModel != null)
            {
                pmChannelModel.ShouldViewNotes = wantsNoteView || pmChannelModel.ShouldViewNotes;
                pmChannelModel.ShouldViewProfile = wantsProfileView || pmChannelModel.ShouldViewProfile;
            }

            channelModel.IsSelected = true;
            model.CurrentChannel = channelModel;

            if (!channelModel.Messages.Any() && !channelModel.Ads.Any())
            {
                IEnumerable<string> history = new List<string>();

                if (!channelId.Equals("Home"))
                    history = logger.GetLogs(channelModel.Title, channelModel.Id);

                if (history.Any() && history.Count() > 1)
                {
                    Dispatcher.Invoke((Action) (() =>
                        history
                            .Select(item => new MessageModel(item))
                            .Each(item => channelModel.AddMessage(item)
                            )));
                }
            }

            Log("Requesting " + channelModel.Id + " channel view");
            Dispatcher.Invoke(
                (Action) delegate
                {
                    foreach (var r in region.Regions[ChatWrapperView.ConversationRegion].Views)
                    {
                        var view = r as DisposableView;
                        if (view != null)
                        {
                            var toDispose = view;
                            toDispose.Dispose();
                            region.Regions[ChatWrapperView.ConversationRegion].Remove(toDispose);
                        }
                        else
                            region.Regions[ChatWrapperView.ConversationRegion].Remove(r);
                    }

                    region.Regions[ChatWrapperView.ConversationRegion].RequestNavigate(
                        HelperConverter.EscapeSpaces(channelModel.Id));
                });

            lastSelected = channelModel;
        }

        [Conditional("DEBUG")]
        private void Log(string text)
        {
            Logging.LogLine(text, "chan serv");
        }

        #endregion
    }
}