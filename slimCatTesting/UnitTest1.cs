using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace slimCatTesting
{
    [TestClass]
    public class PreProcesserTests
    {
        /// <summary>
        /// Tests if the auto-wraper can handle simple strings
        /// </summary>
        [TestMethod]
        public void TestAutoWrapSimple()
        {
            string simple = @"http://www.google.com";
            string converted = HelperConverter.PreProcessBBCode(simple);
            Assert.AreEqual<string>(@"[url=http://www.google.com]google.com[/url]", converted);
        }

        /// <summary>
        /// Tests if the auto-wrapper can handle the link at the end
        /// </summary>
        [TestMethod]
        public void TestAutoWrapAtEnd()
        {
            string atEnd = @"here is a cool link: http://www.google.com";
            string converted = HelperConverter.PreProcessBBCode(atEnd);
            Assert.AreEqual<string>(@"here is a cool link: [url=http://www.google.com]google.com[/url]", converted);
        }

        /// <summary>
        /// Tests if the auto-wrapper can handle links with an irregular format
        /// </summary>
        [TestMethod]
        public void TestAutoWrapSimpleLink()
        {
            /// first for no WWW
            string noWWW = @"fun link: http://i.imgur.com";
            string converted = HelperConverter.PreProcessBBCode(noWWW);
            Assert.AreEqual<string>(@"fun link: [url=http://i.imgur.com]imgur.com[/url]", converted);

            /// then for stuff after the link
            string stuffAfterTheLink = @"dude look: http://i.imgur.com/gallery/something";
            converted = HelperConverter.PreProcessBBCode(stuffAfterTheLink);
            Assert.AreEqual<string>(@"dude look: [url=http://i.imgur.com/gallery/something]imgur.com[/url]", converted);
        }

        /// <summary>
        /// Tests to see if the auto-wrapper can handle many links of various kinds
        /// </summary>
        [TestMethod]
        public void TestMultipleAutoWrap()
        {
            string manyLinks = @"http://foo.bar http://bar.foo http://www.foo.bar/blarg";
            string converted = HelperConverter.PreProcessBBCode(manyLinks);
            Assert.AreEqual<string>(@"[url=http://foo.bar]foo.bar[/url] [url=http://bar.foo]bar.foo[/url] [url=http://www.foo.bar/blarg]foo.bar[/url]", converted);
        }
    }

    [TestClass]
    public class ChatModelTests
    {
        Models.IChatModel testModel;
        Models.ICharacter testCharacter;

        public ChatModelTests()
        {
            testModel = new Models.ChatModel();
            testCharacter = new Models.CharacterModel()
            { Name = "Testing", Gender = Models.Gender.Female };
        }

        /// <summary>
        /// Tests if we can add a character successfully
        /// </summary>
        [TestMethod]
        public void TestAddCharacter()
        {
            testModel.AddCharacter(testCharacter);

            Assert.IsTrue(testModel.OnlineCharacters.Contains(testCharacter));
        }

        /// <summary>
        /// Tests if we can remove a character successfully
        /// </summary>
        [TestMethod]
        public void TestRemoveCharacter()
        {
            testModel.RemoveCharacter(testCharacter.Name);

            Assert.IsFalse(testModel.OnlineCharacters.Contains(testCharacter));
        }

        /// <summary>
        /// Tests if getting a character is reliable
        /// </summary>
        [TestMethod]
        public void TestGetCharacter()
        {
            testModel.AddCharacter(testCharacter);
            Models.ICharacter testing = testModel.FindCharacter(testCharacter.Name);

            Assert.AreEqual<Models.ICharacter>(testing, testCharacter, "Character retrieval not reliable");
        }
    }

    [TestClass]
    public class CommandInterpreterTests
    {
        string currentChannel = "TestingChannel";
        string currentCommand;

        [TestMethod]
        public void TestStatusCommand()
        {
            currentCommand = "/status away Dinner!";
            CommandParser test = new CommandParser(currentCommand, currentChannel);

            IDictionary<string, object> converted = test.toDictionary();
            Assert.AreEqual(converted["type"] as string, "STA");
            Assert.AreEqual(converted["statusmsg"] as string, "Dinner!");
            Assert.AreEqual(converted["status"] as string, "away");
        }

        [TestMethod]
        public void TestSimpleCommand()
        {
            currentCommand = "/kick Derper";
            CommandParser test = new CommandParser(currentCommand, currentChannel);

            IDictionary<string, object> converted = test.toDictionary();
            Assert.AreEqual(converted["type"] as string, "CKU");
            Assert.AreEqual(converted["channel"] as string, currentChannel);
            Assert.AreEqual(converted["character"] as string, "Derper");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBadCommandParameter()
        {
            currentCommand = "/status erp derp";
            CommandParser test=  new CommandParser(currentCommand, currentChannel);

            IDictionary<string, object> converted = test.toDictionary();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestWrongCommandForcedThrough()
        {
            currentCommand = "/arrgsh nnnmo";
            CommandParser test = new CommandParser(currentCommand, currentChannel);

            IDictionary<string, object> converted = test.toDictionary();
        }

        [TestMethod]
        public void NoArgs()
        {
            currentCommand = "/away";
            CommandParser test = new CommandParser(currentCommand, currentChannel);

            IDictionary<string, object> converted = test.toDictionary();
        }

        [TestMethod]
        public void TestBadCommands()
        {
            currentCommand = "/bubbleballs Ag";
            CommandParser test = new CommandParser(currentCommand, currentChannel);

            Assert.IsFalse(test.IsValid);

            currentCommand = "this is a test of a command later /status away don't parse this";
            test = new CommandParser(currentCommand, currentChannel);

            Assert.IsFalse(test.HasCommand);
        }
    }
}
