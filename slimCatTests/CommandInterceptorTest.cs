namespace slimCatTest
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Services;
    using Slimcat.Utilities;

    /// <remarks>
    /// Only tests if commands are intercepted and put into the system correctly
    /// </remarks>
    [TestClass]
    public class CommandInterceptorTest
    {
        #region Fields
        private readonly Mock<IChatModel> chatMock;
        private readonly Mock<IChatConnection> chatConnectionMock;
        private readonly Mock<IChannelManager> channelManagerMock;
        private readonly Mock<ICharacterManager> characterManagerMock;
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region Constructor
        public CommandInterceptorTest()
        {
            // TODO: maybe create a real container to handle this dependency injection
            chatMock = new Mock<IChatModel>();
            chatConnectionMock = new Mock<IChatConnection>();
            channelManagerMock = new Mock<IChannelManager>();
            characterManagerMock = new Mock<ICharacterManager>();

            chatMock.SetupGet(x => x.CurrentPms).Returns(new ObservableCollection<PmChannelModel>());
            chatMock.SetupGet(x => x.CurrentChannels).Returns(new ObservableCollection<GeneralChannelModel>());

            var contain = Mock.Of<IUnityContainer>();
            var regman = Mock.Of<IRegionManager>();

            eventAggregator = new EventAggregator();

            // we shouldn't reference this in tests
            new CommandInterceptor(
                chatMock.Object,
                chatConnectionMock.Object,
                channelManagerMock.Object,
                contain,
                regman,
                eventAggregator,
                characterManagerMock.Object);
        }
        #endregion

        #region Helpers
        private void MockCommand(IDictionary<string, object> command)
        {
            eventAggregator.GetEvent<ChatCommandEvent>().Publish(command);
        }

        private void MockCommand(params KeyValuePair<string, object>[] command)
        {
            eventAggregator.GetEvent<ChatCommandEvent>().Publish(CommandLike(command));

            AllowProcessingTime();
        }

        private static IDictionary<string, object> CommandLike(params KeyValuePair<string, object>[] command)
        {
            return command.ToDictionary(x => x.Key, y => y.Value);
        }

        private static KeyValuePair<string, object> WithArgument(string argument, string value)
        {
            return new KeyValuePair<string, object>(argument, value);
        }

        private void AllowProcessingTime()
        {
            // this is all async, so wait a bit to let the other thread do what it is doing
            // this is imperfect, but best we can do without coupling to a specific interceptor
            Thread.Sleep(20);
        }
        #endregion

        #region PRI
        [TestMethod]
        public void NewPrivateMessageWorks()
        {
            const string character = "testing";
            const string message = "testing";

            characterManagerMock.Setup(
                x => x.IsOnList(character, ListKind.Ignored, true)).Returns(false);

            channelManagerMock.Setup(
                x => x.AddMessage(message, character, character, MessageType.Normal));

            MockCommand(
                WithArgument(Constants.Arguments.Character, character),
                WithArgument(Constants.Arguments.Command, "PRI"),
                WithArgument(Constants.Arguments.Message, message));

            characterManagerMock.VerifyAll();
            channelManagerMock.VerifyAll();
        }

        [TestMethod]
        public void PrivateMethodFromExistingPmWorks()
        {
            const string character = "testing character";
            const string message = "testing message";
            var currentModel = new PmChannelModel(new CharacterModel {Name = character}){TypingStatus = TypingStatus.Typing};

            characterManagerMock.Setup(
                x => x.IsOnList(character, ListKind.Ignored, true)).Returns(false);

            channelManagerMock.Setup(
                x => x.AddMessage(message, character, character, MessageType.Normal));

            chatMock.SetupGet(x => x.CurrentPms)
                .Returns(new ObservableCollection<PmChannelModel> { currentModel });

            MockCommand(
                WithArgument(Constants.Arguments.Character, character),
                WithArgument(Constants.Arguments.Command, "PRI"),
                WithArgument(Constants.Arguments.Message, message));

            characterManagerMock.VerifyAll();
            channelManagerMock.VerifyAll();
            chatMock.VerifyGet(x => x.CurrentPms);

            Assert.IsTrue(currentModel.TypingStatus == TypingStatus.Clear);
        }

        [TestMethod]
        public void PmsFromIgnoredUserAreBlocked()
        {
            const string character = "testing character";
            const string message = "testing message";

            characterManagerMock.Setup(
                x => x.IsOnList(character, ListKind.Ignored, true)).Returns(true);

            chatConnectionMock.Setup(
                x => x.SendMessage(new Dictionary<string, object>
                        {
                            {Constants.Arguments.Action, "notify"},
                            {Constants.Arguments.Character, character},
                            {Constants.Arguments.Type, "IGN"}
                        }));

            MockCommand(
                WithArgument(Constants.Arguments.Character, character),
                WithArgument(Constants.Arguments.Command, "PRI"),
                WithArgument(Constants.Arguments.Message, message));

            characterManagerMock.VerifyAll();
            channelManagerMock.Verify(
                x => x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), MessageType.Normal), Times.Never);
            chatConnectionMock.VerifyAll();
        }
        #endregion

        #region MSG
        [TestMethod]
        public void ChannelMessagesWork()
        {
            const string character = "testing character";
            const string message = "testing message";
            const string channel = "testing channel";

            channelManagerMock.Setup(
                x => x.AddMessage(message, channel, character, MessageType.Normal));

            MockCommand(
                WithArgument(Constants.Arguments.Channel, channel),
                WithArgument(Constants.Arguments.Character, character),
                WithArgument(Constants.Arguments.Message, message),
                WithArgument(Constants.Arguments.Command, "MSG"));

            channelManagerMock.VerifyAll();
        }

        [TestMethod]
        public void ChannelMessagesFromIgnoredUserAreBlocked()
        {
            const string character = "testing character";
            const string message = "testing message";
            const string channel = "testing channel";

            characterManagerMock.Setup(
                x => x.IsOnList(character, ListKind.Ignored, true)).Returns(true);

            MockCommand(
                WithArgument(Constants.Arguments.Channel, channel),
                WithArgument(Constants.Arguments.Character, character),
                WithArgument(Constants.Arguments.Message, message),
                WithArgument(Constants.Arguments.Command, "MSG"));

            channelManagerMock.Verify(
                x => x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageType>()), Times.Never);
        }
        #endregion

        #region LRP
        [TestMethod]
        public void ChannelAdsWork()
        {
            const string character = "testing character";
            const string message = "testing message";
            const string channel = "testing channel";

            channelManagerMock.Setup(
                x => x.AddMessage(message, channel, character, MessageType.Ad));

            MockCommand(
                WithArgument(Constants.Arguments.Channel, channel),
                WithArgument(Constants.Arguments.Character, character),
                WithArgument(Constants.Arguments.Message, message),
                WithArgument(Constants.Arguments.Command, "LRP"));

            channelManagerMock.VerifyAll();
        }

        [TestMethod]
        public void ChannelAdsFromIgnoredUserAreBlocked()
        {
            const string character = "testing character";
            const string message = "testing message";
            const string channel = "testing channel";

            characterManagerMock.Setup(
                x => x.IsOnList(character, ListKind.Ignored, true)).Returns(true);

            MockCommand(
                WithArgument(Constants.Arguments.Channel, channel),
                WithArgument(Constants.Arguments.Character, character),
                WithArgument(Constants.Arguments.Message, message),
                WithArgument(Constants.Arguments.Command, "LRP"));

            channelManagerMock.Verify(
                x => x.AddMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageType>()), Times.Never);
        }
        #endregion

        #region BRO

        [TestMethod]
        public void BroadcastWorks()
        {
            const string character = "testing character";
            const string message = "testing broadcast";

            characterManagerMock.Setup(x => x.Find(character)).Returns(new CharacterModel {Name = character});
            
            var token = eventAggregator.GetEvent<NewUpdateEvent>().Subscribe(x =>
            {
                var characterUpdate = x as CharacterUpdateModel;
                Assert.IsNotNull(characterUpdate);

                Assert.IsTrue(characterUpdate.TargetCharacter.Name.Equals(character));

                var broadcastMessage = characterUpdate.Arguments as CharacterUpdateModel.BroadcastEventArgs;
                Assert.IsNotNull(broadcastMessage);

                Assert.IsTrue(broadcastMessage.Message.Equals(message));
            });

            MockCommand(
                WithArgument(Constants.Arguments.Command, "BRO"),
                WithArgument(Constants.Arguments.Character, character),
                WithArgument(Constants.Arguments.Message, message));

 
            characterManagerMock.VerifyAll();
            eventAggregator.GetEvent<NewUpdateEvent>().Unsubscribe(token);
        }
        #endregion
    }
}
