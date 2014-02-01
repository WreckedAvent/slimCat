namespace slimCatTest
{
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
    using SimpleJson;
    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Services;
    using Slimcat.Utilities;

    [TestClass]
    public class CommandInterceptorTest
    {
        #region Fields
        private readonly Mock<IChatModel> chatModel;
        private readonly Mock<IChatConnection> chatConnection;
        private readonly Mock<IChannelManager> channelManager;
        private readonly Mock<ICharacterManager> characterManager;
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region Constructor
        public CommandInterceptorTest()
        {
            // TODO: maybe create a real container to handle this dependency injection
            chatModel = new Mock<IChatModel>();
            chatConnection = new Mock<IChatConnection>();
            channelManager = new Mock<IChannelManager>();
            characterManager = new Mock<ICharacterManager>();

            chatModel.SetupGet(x => x.CurrentPms).Returns(new ObservableCollection<PmChannelModel>());
            chatModel.SetupGet(x => x.CurrentChannels).Returns(new ObservableCollection<GeneralChannelModel>());

            var contain = Mock.Of<IUnityContainer>();
            var regman = Mock.Of<IRegionManager>();

            eventAggregator = new EventAggregator();

            // we shouldn't reference this in tests
            new CommandInterceptor(
                chatModel.Object,
                chatConnection.Object,
                channelManager.Object,
                contain,
                regman,
                eventAggregator,
                characterManager.Object);
        }
        #endregion

        #region Helpers
        private void MockCommand(IDictionary<string, object> command)
        {
            eventAggregator.GetEvent<ChatCommandEvent>().Publish(command);
        }

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
            return new CharacterModel { Name = name };
        }

        internal static IDictionary<string, object> WithIdentity(string id)
        {
            return new Dictionary<string, object>{{"identity", id}};
        } 

        private static void AllowProcessingTime()
        {
            // this is all async, so wait a bit to let the other thread do what it is doing
            // this is imperfect, but best we can do without coupling to a specific interceptor
            Thread.Sleep(25);
        }

        private static Tuple<CharacterUpdateModel, T> ShouldBeCharacterUpdate<T>(NotificationModel model)
            where T : CharacterUpdateModel.CharacterUpdateEventArgs
        {
            var x = model as CharacterUpdateModel;
            Assert.IsNotNull(x);

            var y = x.Arguments as T;
            Assert.IsNotNull(y);

            return new Tuple<CharacterUpdateModel, T>(x, y);
        }

        private static Tuple<ChannelUpdateModel, T> ShouldBeChannelUpdate<T>(NotificationModel model)
            where T : ChannelUpdateModel.ChannelUpdateEventArgs
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

        [TestClass]
        public class MessageCommandTests : CommandInterceptorTest
        {
            #region Fields
            private const string Character = "testing character";
            private const string Message = "testing message";
            #endregion

            #region Helpers
            private void IgnoreIncomingCharacter()
            {
                characterManager.Setup(
                    x => x.IsOnList(Character, ListKind.Ignored, true)).Returns(true);
            }
            #endregion

            #region PRI
            [TestMethod]
            public void NewPrivateMessageWorks()
            {
                channelManager.Setup(
                    x => x.AddMessage(Message, Character, Character, MessageType.Normal));

                MockCommand(
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Command, "PRI"),
                    WithArgument(Constants.Arguments.Message, Message));

                characterManager.VerifyAll();
                channelManager.VerifyAll();
            }

            [TestMethod]
            public void PrivateMethodFromExistingPmWorks()
            {
                var currentModel = new PmChannelModel(new CharacterModel { Name = Character }) { TypingStatus = TypingStatus.Typing };

                characterManager.Setup(
                    x => x.IsOnList(Character, ListKind.Ignored, true)).Returns(false);

                channelManager.Setup(
                    x => x.AddMessage(Message, Character, Character, MessageType.Normal));

                chatModel.SetupGet(x => x.CurrentPms)
                    .Returns(new ObservableCollection<PmChannelModel> { currentModel });

                MockCommand(
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Command, "PRI"),
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
                            {Constants.Arguments.Action, "notify"},
                            {Constants.Arguments.Character, Character},
                            {Constants.Arguments.Type, "IGN"}
                        }));

                MockCommand(
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Command, "PRI"),
                    WithArgument(Constants.Arguments.Message, Message));

                characterManager.VerifyAll();
                channelManager.Verify(
                    x => x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), MessageType.Normal), Times.Never);
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
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message),
                    WithArgument(Constants.Arguments.Command, "MSG"));

                channelManager.VerifyAll();
            }

            [TestMethod]
            public void ChannelMessagesFromIgnoredUserAreBlocked()
            {
                const string channel = "testing channel";

                IgnoreIncomingCharacter();

                MockCommand(
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message),
                    WithArgument(Constants.Arguments.Command, "MSG"));

                channelManager.Verify(
                    x => x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageType>()), Times.Never);
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
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message),
                    WithArgument(Constants.Arguments.Command, "LRP"));

                channelManager.VerifyAll();
            }

            [TestMethod]
            public void ChannelAdsFromIgnoredUserAreBlocked()
            {
                const string channel = "testing channel";

                IgnoreIncomingCharacter();

                MockCommand(
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message),
                    WithArgument(Constants.Arguments.Command, "LRP"));

                channelManager.Verify(
                    x => x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageType>()), Times.Never);
            }
            #endregion

            #region BRO

            [TestMethod]
            public void BroadcastWorks()
            {
                characterManager.Setup(x => x.Find(Character)).Returns(new CharacterModel { Name = Character });

                ShouldCreateUpdate(x =>
                    {
                        var result = ShouldBeCharacterUpdate<CharacterUpdateModel.BroadcastEventArgs>(x);
                        Assert.IsTrue(result.Item1.TargetCharacter.Name.Equals(Character));
                        Assert.IsTrue(result.Item2.Message.Equals(Message));
                    });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "BRO"),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message));


                characterManager.VerifyAll();
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
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message),
                    WithArgument(Constants.Arguments.Command, "RLL"));

                channelManager.VerifyAll();
            }

            [TestMethod]
            public void ChannelRollsFromIgnoredUserAreBlocked()
            {
                const string channel = "testing channel";

                IgnoreIncomingCharacter();

                MockCommand(
                    WithArgument(Constants.Arguments.Channel, channel),
                    WithArgument(Constants.Arguments.Character, Character),
                    WithArgument(Constants.Arguments.Message, Message),
                    WithArgument(Constants.Arguments.Command, "RLL"));

                channelManager.Verify(
                    x => x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageType>()), Times.Never);
            }
            #endregion
        }

        [TestClass]
        public class ChannelCommandTests : CommandInterceptorTest
        {
            #region Fields
            private const string ChannelName = "testing channel";
            private readonly GeneralChannelModel channelModel;

            const string first = "testing character one";
            const string second = "testing character two";
            #endregion

            #region Constructor
            public ChannelCommandTests()
            {
                channelModel = new GeneralChannelModel(ChannelName, ChannelType.Public);

                chatModel.SetupGet(x => x.AllChannels)
                    .Returns(new ObservableCollection<GeneralChannelModel> {channelModel});

                chatModel.SetupGet(x => x.CurrentChannels)
                    .Returns(new ObservableCollection<GeneralChannelModel> { channelModel });

                chatModel.Setup(x => x.FindChannel(ChannelName, null)).Returns(channelModel);
            }
            #endregion

            #region Helpers
            private void SetupLists()
            {
                characterManager
                    .Setup(x => x.Find(first))
                    .Returns(new CharacterModel { Name = first });

                characterManager
                    .Setup(x => x.Find(second))
                    .Returns(new CharacterModel { Name = second });
            }

            private void JoinCurrentChannel(string name)
            {
                channelModel.CharacterManager.SignOn(CharacterWithName(name));
            }
            #endregion

            #region ICH
            [TestMethod]
            public void ChannelInitializeWorks()
            {
                SetupLists();

                var users = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> {{"identity", first}},
                        new Dictionary<string, object> {{"identity", second}}
                    };

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "ICH"),
                    WithArgument(Constants.Arguments.MultipleUsers,  users),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Mode, ChannelMode.Both.ToString()));


                Thread.Sleep(250);
                characterManager.VerifyAll();

                Assert.IsTrue(channelModel.CharacterManager.Characters.Count == 2); 
                Assert.IsTrue(channelModel.Mode == ChannelMode.Both);
            }
            #endregion

            #region CDS
            [TestMethod]
            public void ChannelDescriptionInitializeWorks()
            {
                const string description = "testing description";

                ShouldNotCreateUpdate();

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "CDS"),
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
                        var result = ShouldBeChannelUpdate<ChannelUpdateModel.ChannelDescriptionChangedEventArgs>(x);
                        Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                    });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "CDS"),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument("description", description));

                Assert.IsTrue(channelModel.Description.Equals(description));
            }
            #endregion

            #region COL
            [TestMethod]
            public void ChannelModeratorListWorks()
            {
                SetupLists();

                var moderators = new JsonArray {first, second};

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "COL"),
                    WithArgument("oplist", moderators),
                    WithArgument(Constants.Arguments.Channel, ChannelName));

                Thread.Sleep(50);
                Assert.IsTrue(channelModel.CharacterManager.IsOnList(first, ListKind.Moderator, false));
                Assert.IsTrue(channelModel.CharacterManager.IsOnList(second, ListKind.Moderator, false));
            }
            #endregion

            #region RMO

            [TestMethod]
            public void ChannelModeChangeWorks()
            {
                ShouldCreateUpdate(x =>
                    {
                        var result = ShouldBeChannelUpdate<ChannelUpdateModel.ChannelModeUpdateEventArgs>(x);
                        Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                        Assert.IsTrue(result.Item2.NewMode == ChannelMode.Ads);
                    });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "RMO"),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Mode, ChannelMode.Ads.ToString()));

                chatModel.VerifyGet(x => x.CurrentChannels);
                Assert.IsTrue(channelModel.Mode == ChannelMode.Ads);
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
                    var result = ShouldBeChannelUpdate<ChannelUpdateModel.ChannelDisciplineEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                    Assert.IsTrue(result.Item2.IsBan == false);
                    Assert.IsTrue(result.Item2.Kicker == op);
                    Assert.IsTrue(result.Item2.Kicked == kicked);
                });

                SetCurrentCharacterTo("foobar");
                JoinCurrentChannel(kicked);

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "CKU"),
                    WithArgument("operator", op),
                    WithArgument(Constants.Arguments.Character, kicked),
                    WithArgument(Constants.Arguments.Channel, channelModel.Id));

                chatModel.VerifyGet(x => x.CurrentCharacter);
                chatModel.Verify(x => x.FindChannel(ChannelName, null), Times.Exactly(1));
                Assert.IsFalse(channelModel.CharacterManager.IsOnList(kicked, ListKind.Online));
            }

            [TestMethod]
            public void ChannelKickCurrentCharacterWorks()
            {
                const string op = "testing kicker";
                const string kicked = "testing kickee";

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeChannelUpdate<ChannelUpdateModel.ChannelDisciplineEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                    Assert.IsTrue(result.Item2.IsBan == false);
                    Assert.IsTrue(result.Item2.Kicker == op);
                    Assert.IsTrue(result.Item2.Kicked == kicked);
                });

                SetCurrentCharacterTo(kicked);
                JoinCurrentChannel(kicked);
                channelManager.Setup(x => x.RemoveChannel(ChannelName));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "CKU"),
                    WithArgument("operator", op),
                    WithArgument(Constants.Arguments.Character, kicked),
                    WithArgument(Constants.Arguments.Channel, channelModel.Id));

                chatModel.VerifyGet(x => x.CurrentCharacter);
                chatModel.Verify(x => x.FindChannel(ChannelName, null), Times.Exactly(1));
                channelManager.VerifyAll();
            }

            [TestMethod]
            public void ChannelBanWorks()
            {
                const string op = "testing kicker";
                const string kicked = "testing kickee";

                ShouldCreateUpdate(x =>
                    {
                        var result = ShouldBeChannelUpdate<ChannelUpdateModel.ChannelDisciplineEventArgs>(x);
                        Assert.IsTrue(result.Item1.TargetChannel.Equals(channelModel));
                        Assert.IsTrue(result.Item2.IsBan);
                        Assert.IsTrue(result.Item2.Kicker == op);
                        Assert.IsTrue(result.Item2.Kicked == kicked);
                    });

                SetCurrentCharacterTo("foobar");
                channelModel.CharacterManager.SignOn(CharacterWithName(kicked));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "CBU"),
                    WithArgument("operator", op),
                    WithArgument(Constants.Arguments.Character, kicked),
                    WithArgument(Constants.Arguments.Channel, channelModel.Id));

                chatModel.VerifyGet(x => x.CurrentCharacter);
                chatModel.Verify(x => x.FindChannel(ChannelName, null), Times.Exactly(1));
                Assert.IsFalse(channelModel.CharacterManager.IsOnList(kicked, ListKind.Online));
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
                    var result = ShouldBeCharacterUpdate<CharacterUpdateModel.PromoteDemoteEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetCharacter.Name.Equals(promotee));
                    Assert.IsTrue(result.Item2.IsPromote);
                    Assert.IsTrue(result.Item2.TargetChannel.Equals(ChannelName));
                });

                characterManager.Setup(x => x.Find(promotee)).Returns(CharacterWithName(promotee));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "COA"),
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
                    var result = ShouldBeCharacterUpdate<CharacterUpdateModel.PromoteDemoteEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetCharacter.Name.Equals(promotee));
                    Assert.IsFalse(result.Item2.IsPromote);
                    Assert.IsTrue(result.Item2.TargetChannel.Equals(ChannelName));
                });

                characterManager.Setup(x => x.Find(promotee)).Returns(CharacterWithName(promotee));

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "COR"),
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

                LogInCharacter(joinerName);

                ShouldCreateUpdate(x =>
                {
                    var result = ShouldBeCharacterUpdate<CharacterUpdateModel.JoinLeaveEventArgs>(x);
                    Assert.IsTrue(result.Item1.TargetCharacter.Equals(joiner));
                    Assert.IsTrue(result.Item2.TargetChannel.Equals(ChannelName));
                    Assert.IsTrue(result.Item2.Joined);
                });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "JCH"),
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

                chatModel.SetupGet(x => x.CurrentChannels).Returns(new ObservableCollection<GeneralChannelModel>());
                channelManager.Setup(x => x.JoinChannel(ChannelType.Public, ChannelName, ChannelName));
                
                ShouldNotCreateUpdate();

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "JCH"),
                    WithArgument(Constants.Arguments.Character, WithIdentity(joinerName)),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Title, ChannelName));

                chatModel.SetupGet(x => x.CurrentChannels);
                channelManager.VerifyAll();
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
                        var result = ShouldBeCharacterUpdate<CharacterUpdateModel.JoinLeaveEventArgs>(x);
                        Assert.IsTrue(result.Item1.TargetCharacter.Name.Equals(leaverName));
                        Assert.IsTrue(result.Item2.TargetChannel.Equals(ChannelName));
                        Assert.IsFalse(result.Item2.Joined);
                    });

                MockCommand(
                    WithArgument(Constants.Arguments.Command, "LCH"),
                    WithArgument(Constants.Arguments.Channel, ChannelName),
                    WithArgument(Constants.Arguments.Character, leaverName));

                characterManager.VerifyAll();
            }
            #endregion
        }
    }
}
