#region Copyright

// <copyright file="ChannelService.cs">
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
    using Utilities.Extensions;
    using ViewModels;
    using Views;
    using Commands = Utilities.Constants.ClientCommands;

    #endregion

    public class ChannelService : DispatcherObject, IManageChannels
    {
        #region Constructors and Destructors

        public ChannelService(
            IChatState chatState,
            ILogThings logger,
            IAutomateThings automation)
        {
            try
            {
                region = chatState.RegionManager;
                container = chatState.Container;
                events = chatState.EventAggregator;
                cm = chatState.ChatModel;
                connection = chatState.Connection;
                characters = chatState.CharacterManager;
                this.logger = logger.ThrowIfNull("logger");
                this.automation = automation.ThrowIfNull("automation");

                events.GetEvent<ChatOnDisplayEvent>().Subscribe(BuildHomeChannel, ThreadOption.UIThread, true);
                events.GetEvent<RequestChangeTabEvent>().Subscribe(RequestNavigate, ThreadOption.UIThread, true);
            }
            catch (Exception ex)
            {
                ex.Source = "Message Daemon, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Fields

        private const int MaxRecentTabs = 30;

        private readonly IAutomateThings automation;

        private readonly ICharacterManager characters;

        private readonly IHandleChatConnection connection;

        private readonly IUnityContainer container;

        private readonly IEventAggregator events;

        private readonly ILogThings logger;

        private readonly IChatModel cm;

        private readonly IRegionManager region;

        private ChannelModel lastSelected;

        #endregion

        #region Public Methods and Operators

        public void AddChannel(ChannelType type, string id, string name)
        {
            Log((id != name && !string.IsNullOrEmpty(name))
                ? $"Making {type} Channel {id} \"{name}\""
                : $"Making {type} Channel {id}");

            if (type == ChannelType.PrivateMessage)
            {
                var character = characters.Find(id);

                character.GetAvatar(); // make sure we have their picture

                // model doesn't have a reference to PrivateMessage channels, build it manually
                var temp = new PmChannelModel(character);
                container.RegisterInstance(temp.Id, temp);
                container.Resolve<PmChannelViewModel>(new ParameterOverride("name", temp.Id));

                Dispatcher.Invoke(() => cm.CurrentPms.Add(temp));
                // then add it to the model's data

                ApplicationSettings.RecentCharacters.BacklogWithUpdate(id, MaxRecentTabs);
                SettingsService.SaveApplicationSettingsToXml(cm.CurrentCharacter.Name);
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
                    temp = cm.AllChannels.FirstOrDefault(param => param.Id == id);

                    if (temp == null)
                    {
                        temp = new GeneralChannelModel(id, name,
                            id == name ? ChannelType.Public : ChannelType.Private);
                        Dispatcher.Invoke(() => cm.AllChannels.Add(temp));
                    }

                    Dispatcher.Invoke(() => cm.CurrentChannels.Add(temp));

                    container.Resolve<GeneralChannelViewModel>(new ParameterOverride("name", id));

                    ApplicationSettings.RecentChannels.BacklogWithUpdate(id, MaxRecentTabs);
                    SettingsService.SaveApplicationSettingsToXml(cm.CurrentCharacter.Name);
                }

                if (!cm.CurrentChannels.Contains(temp))
                    Dispatcher.Invoke(() => cm.CurrentChannels.Add(temp));
            }
        }

        public void AddMessage(
            string message, string channelName, string poster, MessageType messageType = MessageType.Normal)
        {
            var sender =
                characters.Find(poster == Constants.Arguments.ThisCharacter ? cm.CurrentCharacter.Name : poster);

            var channel = cm.CurrentChannels.FirstByIdOrNull(channelName)
                          ?? (ChannelModel) cm.CurrentPms.FirstByIdOrNull(channelName);

            if (channel == null)
                return; // exception circumstance, swallow message
            if (characters.IsOnList(poster, ListKind.ClientIgnored, false))
            {
                // this poster is client-ignored, swallow message
                return;
            }

            if (messageType == MessageType.Ad && characters.IsOnList(poster, ListKind.NotInterested, false))
                return; // don't want these clogging up our filter or.. anything really

            Dispatcher.InvokeWithRetry(() =>
            {
                var thisMessage = new MessageModel(sender, message, messageType);

                channel.AddMessage(thisMessage, characters.IsOfInterest(poster));

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

            if (cm.CurrentChannel != null && cm.CurrentChannel.Id.Equals(id))
                return;

            name = HttpUtility.HtmlDecode(name);

            Log((id != name && !string.IsNullOrEmpty(name))
                ? $"Joining {type} Channel {id} \"{name}\""
                : $"Joining {type} Channel {id}");

            var toJoin = cm.CurrentPms.FirstByIdOrNull(id)
                         ?? (ChannelModel) cm.CurrentChannels.FirstByIdOrNull(id);

            if (toJoin == null)
            {
                AddChannel(type, id, name);

                toJoin = cm.CurrentPms.FirstByIdOrNull(id)
                         ?? (ChannelModel) cm.CurrentChannels.FirstByIdOrNull(id);
            }

            RequestNavigate(originalId);
        }

        public void RemoveChannel(string name, bool force, bool isServer)
        {
            Log("Removing Channel " + name);

            if (cm.CurrentChannels.Any(param => param.Id == name))
            {
                var temp = cm.CurrentChannels.FirstByIdOrNull(name);
                temp.Description = null;

                var index = cm.CurrentChannels.IndexOf(temp);
                RequestNavigate(cm.CurrentChannels[index - 1].Id);

                Dispatcher.Invoke(
                    (Action) (() => cm.CurrentChannels.Remove(temp)));

                if (isServer) return;

                object toSend = new {channel = name};
                connection.SendMessage(toSend, Commands.ChannelLeave);
            }
            else if (cm.CurrentPms.Any(param => param.Id == name))
            {
                var temp = cm.CurrentPms.FirstByIdOrNull(name);
                var index = cm.CurrentPms.IndexOf(temp);
                RequestNavigate(index != 0 ? cm.CurrentPms[index - 1].Id : "Home");

                cm.CurrentPms.Remove(temp);
            }
            else if (force)
            {
                object toSend = new {channel = name};
                connection.SendMessage(toSend, Commands.ChannelLeave);
            }
            else
                throw new ArgumentOutOfRangeException(nameof(name), "Could not find the channel requested to remove");
        }

        public void QuickJoinChannel(string id, string name)
        {
            name = HttpUtility.HtmlDecode(name);

            var type = (id == name) ? ChannelType.Public : ChannelType.Private;

            if (cm.CurrentChannels.FirstByIdOrNull(id) != null)
                return;

            Log((id != name && !string.IsNullOrEmpty(name))
                ? $"Quick Joining {type} Channel {id} \"{name}\""
                : $"Quick Joining {type} Channel {id}");

            var temp = new GeneralChannelModel(id, name, type);
            Dispatcher.Invoke(() =>
            {
                cm.AllChannels.Add(temp);
                cm.CurrentChannels.Add(temp);
            });

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

                Dispatcher.Invoke(() =>
                {
                    var toUpdate = cm.CurrentChannels.FirstByIdOrNull(lastSelected.Id)
                                   ?? (ChannelModel) cm.CurrentPms.FirstByIdOrNull(lastSelected.Id);

                    if (toUpdate == null)
                        lastSelected = null;
                    else
                        toUpdate.IsSelected = false;
                });
            }

            var channelModel = cm.CurrentChannels.FirstByIdOrNull(channelId)
                               ?? (ChannelModel) cm.CurrentPms.FirstByIdOrNull(channelId);

            if (channelModel == null)
                throw new ArgumentOutOfRangeException(nameof(channelId), "Cannot navigate to unknown channel");

            var pmChannelModel = channelModel as PmChannelModel;
            if (pmChannelModel != null)
            {
                pmChannelModel.ShouldViewNotes = wantsNoteView || pmChannelModel.ShouldViewNotes;
                pmChannelModel.ShouldViewProfile = wantsProfileView || pmChannelModel.ShouldViewProfile;
            }

            channelModel.IsSelected = true;
            cm.CurrentChannel = channelModel;

            if (!channelModel.Messages.Any() && !channelModel.Ads.Any())
            {
                var history = new RawChannelLogModel();

                if (!channelId.Equals("Home"))
                    history = logger.GetLogs(channelModel.Title, channelModel.Id);

                if (history.RawLogs.Any())
                {
                    Dispatcher.Invoke(() => history.RawLogs
                        .Select(item => new MessageModel(item, characters.Find, history.DateOfLog))
                        .Each(item => channelModel.AddMessage(item)
                        ));
                }
            }

            Log("Requesting " + channelModel.Id + " channel view");
            Dispatcher.Invoke(() =>
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
                    StringExtensions.EscapeSpaces(channelModel.Id));
            });

            lastSelected = channelModel;
        }

        [Conditional("DEBUG")]
        private static void Log(string text) => Logging.LogLine(text, "chan serv");

        #endregion
    }
}