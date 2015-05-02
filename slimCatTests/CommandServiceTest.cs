﻿#region Copyright

// <copyright file="CommandServiceTest.cs">
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

namespace slimCatTest
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using slimCat;
    using slimCat.Models;
    using slimCat.Services;
    using slimCat.Utilities;
    using SimpleJson;
    using Commands = slimCat.Utilities.Constants.ServerCommands;

    #endregion

    [TestClass]
    public class CommandServiceTest
    {
        #region Constructor

        public CommandServiceTest()
        {
            // TODO: maybe create a real container to handle this dependency injection
            chatModel = new Mock<IChatModel>();
            chatConnection = new Mock<IHandleChatConnection>();
            channelManager = new Mock<IManageChannels>();
            characterManager = new Mock<ICharacterManager>();

            chatModel.SetupGet(x => x.CurrentPms).Returns(new ObservableCollection<PmChannelModel>());
            chatModel.SetupGet(x => x.CurrentChannels).Returns(new ObservableCollection<GeneralChannelModel>());

            var contain = Mock.Of<IUnityContainer>();
            var regman = Mock.Of<IRegionManager>();
            var auto = Mock.Of<IAutomateThings>();
            var notes = Mock.Of<IManageNotes>();
            var friendRequest = Mock.Of<IFriendRequestService>();
            var account = Mock.Of<IAccount>();

            eventAggregator = new EventAggregator();
            var state = new ChatState(
                contain,
                regman,
                eventAggregator,
                chatModel.Object,
                characterManager.Object,
                chatConnection.Object,
                account);

            // we shouldn't reference this in tests
            new ServerCommandService(
                state,
                auto,
                notes,
                channelManager.Object,
                friendRequest);
        }

        #endregion

        [TestClass]
        public class ChannelCommandTests : CommandServiceTest
        {
            #region Constructor

            public ChannelCommandTests()
            {
                channelModel = new GeneralChannelModel(ChannelName, ChannelType.Public);

                chatModel.SetupGet(x => x.AllChannels)
                    .Returns(new ObservableCollection<GeneralChannelModel> {channelModel});

                chatModel.SetupGet(x => x.CurrentChannels)
                    .Returns(new ObservableCollection<GeneralChannelModel> {channelModel});

                chatModel.Setup(x => x.FindChannel(ChannelName, null)).Returns(channelModel);
            }

            #endregion

            #region ICH

            [TestMethod]
            public void ChannelInitializeWorks()
            {
                SetupLists();

                var usersData = new List<IDictionary<string, object>>
                {
                    WithIdentity(First),
                    WithIdentity(Second)
                };

                var users = new JsonArray();
                users.AddRange(usersData);

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelInitialize),
                    WithArgument(Constants.Arguments.MultipleUsers, users),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Mode, ChannelMode.Both.ToString()));


                Thread.Sleep(250);
                characterManager.VerifyAll();

                Assert.IsTrue(channelModel.CharacterManager.Characters.Count == 2);
                Assert.IsTrue(channelModel.Mode == ChannelMode.Both);
            }

            #endregion

            #region COL

            [TestMethod]
            public void ChannelModeratorListWorks()
            {
                SetupLists();

                var moderators = new JsonArray {First, Second};

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelModerators),
                    WithArgument("oplist", moderators),
                    WithArgument(Constants.Arguments.Channel, ChannelName));

                Thread.Sleep(50);
                Assert.IsTrue(channelModel.CharacterManager.IsOnList(First, ListKind.Moderator, false));
                Assert.IsTrue(channelModel.CharacterManager.IsOnList(Second, ListKind.Moderator, false));
            }

            #endregion

            #region RMO

            [TestMethod]
            public void ChannelModeChangeWorks()
            {
                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeChannelUpdate<ChannelModeUpdateEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                    Assert.IsTrue(result.Item2.NewMode == ChannelMode.Ads);
                });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelMode),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Mode, ChannelMode.Ads.ToString()));

                chatModel.VerifyGet(x => x.CurrentChannels);
                Assert.IsTrue(channelModel.Mode == ChannelMode.Ads);
            }

            #endregion

            #region LCH

            [TestMethod]
            public void LeaveChannelWorks()
            {
                const string leaverName = "testing leaving character";
                var leaver = CharacterWithName(leaverName);

                channelModel.CharacterManager.SignOn(leaver);
                LogInCharacter(leaverName);
                SetCurrentCharacterTo("foobar");

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeCharacterUpdate<JoinLeaveEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetCharacter.Name.Equals(leaverName));
                    Assert.IsTrue(result.Item2.TargetChannel.Equals(ChannelName));
                    Assert.IsFalse(result.Item2.Joined);
                });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelLeave),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Character, leaverName));

                characterManager.VerifyAll();
                Assert.IsFalse(channelModel.CharacterManager.IsOnList(leaverName, ListKind.Online));
            }

            #endregion

            #region Fields

            private const string ChannelName = "testing channel";

            private const string First = "testing character one";
            private const string Second = "testing character two";
            private readonly GeneralChannelModel channelModel;

            #endregion

            #region Helpers

            private void SetupLists()
            {
                characterManager
                    .Setup(x => x.Find(First))
                    .Returns(new CharacterModel {Name = First});

                characterManager
                    .Setup(x => x.Find(Second))
                    .Returns(new CharacterModel {Name = Second});
            }

            private void JoinCurrentChannel(string name)
            {
                channelModel.CharacterManager.SignOn(CharacterWithName(name));
            }

            #endregion

            #region CDS

            [TestMethod]
            public void ChannelDescriptionInitializeWorks()
            {
                const string description = "testing description";

                ShouldNotCreateUpdate();

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelDescription),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument("description", description));

                Assert.IsTrue(channelModel.Description.Equals(description));
            }

            [TestMethod]
            public void ChangeChannelDescriptionWorks()
            {
                const string description = "testing description";
                channelModel.Description = "some other description";

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeChannelUpdate<ChannelDescriptionChangedEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelDescription),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument("description", description));

                Assert.IsTrue(channelModel.Description.Equals(description));
            }

            #endregion

            #region CKU / CBU

            [TestMethod]
            public void ChannelKickWorks()
            {
                const string op = "testing kicker";
                const string kicked = "testing kickee";

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeChannelUpdate<ChannelDisciplineEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                    Assert.IsTrue(result.Item2.IsBan == false);
                    Assert.IsTrue(result.Item2.Kicker == op);
                    Assert.IsTrue(result.Item2.Kicked == kicked);
                });

                SetCurrentCharacterTo("foobar");
                JoinCurrentChannel(kicked);

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelKick),
                    WithArgument("operator", op),
                    WithArgument(Constants.Arguments.Character, kicked),
                    WithArgument(Constants.Arguments.Channel, channelModel.Id));

                chatModel.VerifyGet(x => x.CurrentCharacter);
                chatModel.Verify(x => x.CurrentChannels, Times.Exactly(1));
                Assert.IsFalse(channelModel.CharacterManager.IsOnList(kicked, ListKind.Online));
            }

            [TestMethod]
            public void ChannelKickCurrentCharacterWorks()
            {
                const string op = "testing kicker";
                const string kicked = "testing kickee";

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeChannelUpdate<ChannelDisciplineEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                    Assert.IsTrue(result.Item2.IsBan == false);
                    Assert.IsTrue(result.Item2.Kicker == op);
                    Assert.IsTrue(result.Item2.Kicked == kicked);
                });

                SetCurrentCharacterTo(kicked);
                JoinCurrentChannel(kicked);
                channelManager.Setup(x => x.RemoveChannel(ChannelName, false, false));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelKick),
                    WithArgument("operator", op),
                    WithArgument(Constants.Arguments.Character, kicked),
                    WithArgument(Constants.Arguments.Channel, channelModel.Id));

                chatModel.VerifyGet(x => x.CurrentCharacter);
                chatModel.Verify(x => x.CurrentChannels, Times.Exactly(1));
                channelManager.VerifyAll();
            }

            [TestMethod]
            public void ChannelBanWorks()
            {
                const string op = "testing kicker";
                const string kicked = "testing kickee";

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeChannelUpdate<ChannelDisciplineEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                    Assert.IsTrue(result.Item2.IsBan);
                    Assert.IsTrue(result.Item2.Kicker == op);
                    Assert.IsTrue(result.Item2.Kicked == kicked);
                });

                SetCurrentCharacterTo("foobar");
                channelModel.CharacterManager.SignOn(CharacterWithName(kicked));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelBan),
                    WithArgument("operator", op),
                    WithArgument(Constants.Arguments.Character, kicked),
                    WithArgument(Constants.Arguments.Channel, channelModel.Id));

                chatModel.VerifyGet(x => x.CurrentCharacter);
                chatModel.VerifyGet(x => x.CurrentChannels, Times.Exactly(1));
                Assert.IsFalse(channelModel.CharacterManager.IsOnList(kicked, ListKind.Online));
                Assert.IsTrue(channelModel.CharacterManager.IsOnList(kicked, ListKind.Banned));
            }

            #endregion

            #region COA / COR

            [TestMethod]
            public void ChannelPromoteWorks()
            {
                const string promotee = "testing character";
                channelModel.CharacterManager.SignOn(CharacterWithName(promotee));

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeCharacterUpdate<PromoteDemoteEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetCharacter.Name.Equals(promotee));
                    Assert.IsTrue(result.Item2.IsPromote);
                    Assert.IsTrue(result.Item2.TargetChannel.Equals(ChannelName));
                });

                characterManager.Setup(x => x.Find(promotee)).Returns(CharacterWithName(promotee));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelPromote),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Character, promotee));

                Assert.IsTrue(channelModel.CharacterManager.IsOnList(promotee, ListKind.Moderator));
            }

            [TestMethod]
            public void ChannelDemoteWorks()
            {
                const string promotee = "testing character";
                channelModel.CharacterManager.SignOn(CharacterWithName(promotee));
                channelModel.CharacterManager.Add(promotee, ListKind.Moderator);

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeCharacterUpdate<PromoteDemoteEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetCharacter.Name.Equals(promotee));
                    Assert.IsFalse(result.Item2.IsPromote);
                    Assert.IsTrue(result.Item2.TargetChannel.Equals(ChannelName));
                });

                characterManager.Setup(x => x.Find(promotee)).Returns(CharacterWithName(promotee));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelDemote),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Character, promotee));

                Assert.IsFalse(channelModel.CharacterManager.IsOnList(promotee, ListKind.Moderator));
            }

            #endregion

            #region JCH

            [TestMethod]
            public void ChannelJoinWorks()
            {
                const string joinerName = "testing joiner character";
                var joiner = CharacterWithName(joinerName);

                chatModel.Setup(x => x.CurrentCharacter).Returns(new CharacterModel {Name = "Foo bar"});

                LogInCharacter(joinerName);

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeCharacterUpdate<JoinLeaveEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetCharacter.Equals(joiner));
                    Assert.IsTrue(result.Item2.TargetChannel.Equals(ChannelName));
                    Assert.IsTrue(result.Item2.Joined);
                });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelJoin),
                    WithArgument(Constants.Arguments.Character, WithIdentity(joinerName)),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Title, ChannelName));

                characterManager.VerifyAll();
                Assert.IsTrue(channelModel.CharacterManager.IsOnList(joinerName, ListKind.Online));
            }

            [TestMethod]
            public void OurChannelJoinWorks()
            {
                const string joinerName = "testing joiner character";

                chatModel.Setup(x => x.CurrentCharacter).Returns(new CharacterModel {Name = joinerName});
                chatModel.SetupGet(x => x.CurrentChannels).Returns(new ObservableCollection<GeneralChannelModel>());
                channelManager.Setup(x => x.JoinChannel(ChannelType.Public, ChannelName, ChannelName));

                ShouldNotCreateUpdate();

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelJoin),
                    WithArgument(Constants.Arguments.Character, WithIdentity(joinerName)),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Title, ChannelName));

                chatModel.SetupGet(x => x.CurrentChannels);
                channelManager.VerifyAll();
            }

            #endregion
        }

        [TestClass]
        public class MessageCommandTests : CommandServiceTest
        {
            #region Helpers

            private void IgnoreIncomingCharacter()
            {
                characterManager.Setup(
                    x => x.IsOnList(Character, ListKind.Ignored, true)).Returns(true);
            }

            #endregion

            #region BRO

            [TestMethod]
            public void BroadcastWorks()
            {
                characterManager.Setup(x => x.Find(Character)).Returns(new CharacterModel {Name = Character});

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeCharacterUpdate<BroadcastEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetCharacter.Name.Equals(Character));
                    Assert.IsTrue(result.Item2.Message.Equals(Message));
                });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.AdminBroadcast),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));


                characterManager.VerifyAll();
            }

            #endregion

            #region Fields

            private const string Character = "testing character";
            private const string Message = "testing message";

            #endregion

            #region PRI

            [TestMethod]
            public void NewPrivateMessageWorks()
            {
                channelManager.Setup(
                    x => x.AddMessage(Message, Character, Character, MessageType.Normal));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.UserMessage),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                characterManager.VerifyAll();
                channelManager.VerifyAll();
            }

            [TestMethod]
            public void PrivateMethodFromExistingPmWorks()
            {
                var currentModel = new PmChannelModel(new CharacterModel {Name = Character})
                {
                    TypingStatus = TypingStatus.Typing
                };

                characterManager.Setup(
                    x => x.IsOnList(Character, ListKind.Ignored, true)).Returns(false);

                channelManager.Setup(
                    x => x.AddMessage(Message, Character, Character, MessageType.Normal));

                chatModel.SetupGet(x => x.CurrentPms)
                    .Returns(new ObservableCollection<PmChannelModel> {currentModel});

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.UserMessage),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                characterManager.VerifyAll();
                channelManager.VerifyAll();
                chatModel.VerifyGet(x => x.CurrentPms);

                Assert.IsTrue(currentModel.TypingStatus == TypingStatus.Clear);
            }

            [TestMethod]
            public void PmsFromIgnoredUserAreBlocked()
            {
                IgnoreIncomingCharacter();

                chatConnection.Setup(
                    x => x.SendMessage(new Dictionary<string, object>
                    {
                        {Constants.Arguments.Action, Constants.Arguments.ActionNotify},
                        {Constants.Arguments.Character, Character},
                        {Constants.Arguments.Type, Commands.UserIgnore}
                    }));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.UserMessage),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                characterManager.VerifyAll();
                channelManager.Verify(
                    x => x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), MessageType.Normal),
                    Times.Never);
                chatConnection.VerifyAll();
            }

            #endregion

            #region MSG

            [TestMethod]
            public void ChannelMessagesWork()
            {
                const string channel = "testing channel";

                channelManager.Setup(
                    x => x.AddMessage(Message, channel, Character, MessageType.Normal));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelMessage),
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                channelManager.VerifyAll();
            }

            [TestMethod]
            public void ChannelMessagesFromIgnoredUserAreBlocked()
            {
                const string channel = "testing channel";

                IgnoreIncomingCharacter();

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelMessage),
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                channelManager.Verify(
                    x =>
                        x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageType>()),
                    Times.Never);
            }

            #endregion

            #region LRP

            [TestMethod]
            public void ChannelAdsWork()
            {
                const string channel = "testing channel";

                channelManager.Setup(
                    x => x.AddMessage(Message, channel, Character, MessageType.Ad));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelAd),
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                channelManager.VerifyAll();
            }

            [TestMethod]
            public void ChannelAdsFromIgnoredUserAreBlocked()
            {
                const string channel = "testing channel";

                IgnoreIncomingCharacter();

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelAd),
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                channelManager.Verify(
                    x =>
                        x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageType>()),
                    Times.Never);
            }

            #endregion

            #region RLL

            [TestMethod]
            public void ChannelRollsWork()
            {
                const string channel = "testing channel";

                channelManager.Setup(
                    x => x.AddMessage(Message, channel, Character, MessageType.Roll));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelRoll),
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                channelManager.VerifyAll();
            }

            [TestMethod]
            public void ChannelRollsFromIgnoredUserAreBlocked()
            {
                const string channel = "testing channel";

                IgnoreIncomingCharacter();

                MockCommand(
                    WithArgument(Constants.Arguments.Command, Commands.ChannelRoll),
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));

                channelManager.Verify(
                    x =>
                        x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageType>()),
                    Times.Never);
            }

            #endregion
        }

        #region Fields

        private readonly Mock<IManageChannels> channelManager;
        private readonly Mock<ICharacterManager> characterManager;
        private readonly Mock<IHandleChatConnection> chatConnection;
        private readonly Mock<IChatModel> chatModel;
        private readonly IEventAggregator eventAggregator;

        #endregion

        #region Helpers

        internal void MockCommand(params KeyValuePair<string, object>[] command)
        {
            eventAggregator.GetEvent<ChatCommandEvent>().Publish(CommandLike(command));

            AllowProcessingTime();
        }

        private static IDictionary<string, object> CommandLike(params KeyValuePair<string, object>[] command)
        {
            return command.ToDictionary(x => x.Key, y => y.Value);
        }

        internal static KeyValuePair<string, object> WithArgument(string argument, object value)
        {
            return new KeyValuePair<string, object>(argument, value);
        }

        internal static ICharacter CharacterWithName(string name)
        {
            return new CharacterModel {Name = name};
        }

        internal static IDictionary<string, object> WithIdentity(string id)
        {
            return new Dictionary<string, object> {{Constants.Arguments.Identity, id}};
        }

        private static void AllowProcessingTime()
        {
            // this is all async, so wait a bit to let the other thread do what it is doing
            // this is imperfect, but best we can do without coupling to a specific interceptor
            Thread.Sleep(25);
        }

        private static Tuple<CharacterUpdateModel, T> ShouldBeCharacterUpdate<T>(NotificationModel model)
            where T : CharacterUpdateEventArgs
        {
            var x = model as CharacterUpdateModel;
            Assert.IsNotNull(x);

            var y = x.Arguments as T;
            Assert.IsNotNull(y);

            return new Tuple<CharacterUpdateModel, T>(x, y);
        }

        private static Tuple<ChannelUpdateModel, T> ShouldBeChannelUpdate<T>(NotificationModel model)
            where T : ChannelUpdateEventArgs
        {
            var x = model as ChannelUpdateModel;
            Assert.IsNotNull(x);

            var y = x.Arguments as T;
            Assert.IsNotNull(y);

            return new Tuple<ChannelUpdateModel, T>(x, y);
        }

        internal void ShouldCreateUpdate(Action<NotificationModel> action)
        {
            eventAggregator.GetEvent<NewUpdateEvent>().Subscribe(action);
        }

        internal void ShouldNotCreateUpdate()
        {
            ShouldCreateUpdate(x => Assert.Fail("Should not have generated update"));
        }

        internal void SetCurrentCharacterTo(string name)
        {
            chatModel.SetupGet(x => x.CurrentCharacter).Returns(CharacterWithName(name));
        }

        internal void LogInCharacter(string name)
        {
            characterManager.Setup(x => x.Find(name)).Returns(CharacterWithName(name));
        }

        #endregion
    }
}