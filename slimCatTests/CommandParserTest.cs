#region Copyright

// <copyright file="CommandParserTest.cs">
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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using slimCat.Utilities;

    #endregion

    [TestClass]
    public class CommandParserTest
    {
        private static CommandParser TestOf(string input, string channel = "")
        {
            return new CommandParser(input, channel);
        }

        [TestMethod]
        public void DoesNotMistakeMessageForCommand()
        {
            Assert.IsFalse(TestOf("hi there").HasCommand);
        }

        [TestMethod]
        public void RecognizesSimpleValidCommand()
        {
            var result = TestOf("/clear");
            Assert.IsTrue(result.HasCommand);
            Assert.AreEqual("clear", result.Type);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void FailsOnUnknownCommand()
        {
            TestOf("/foobar").ToDictionary();
        }

        [TestMethod]
        public void RecognizesNonCommandCommand()
        {
            Assert.IsTrue(CommandParser.HasNonCommand("/me does an action"));
        }

        [TestMethod]
        public void CanHandleOneArgument()
        {
            var result = TestOf("/priv name");
            Assert.IsTrue(result.HasCommand);
            Assert.AreEqual("priv", result.Type);

            var command = result.ToDictionary();
            Assert.AreEqual(2, command.Keys.Count);
            Assert.AreEqual("priv", command[Constants.Arguments.Type]);
            Assert.AreEqual("name", command[Constants.Arguments.Character]);
        }

        [TestMethod]
        public void CanHandleTwoArguments()
        {
            var result = TestOf("/status online foo");
            Assert.IsTrue(result.HasCommand);
            Assert.AreEqual("status", result.Type);

            var command = result.ToDictionary();
            Assert.AreEqual(3, command.Keys.Count);
            Assert.AreEqual(Constants.ClientCommands.UserStatus, command[Constants.Arguments.Type]);
            Assert.AreEqual("online", command[Constants.Arguments.Status]);
            Assert.AreEqual("foo", command[Constants.Arguments.StatusMessage]);
        }

        [TestMethod]
        public void CanHandleCommasInArguments()
        {
            var result = TestOf("/status online I'm really tired today, hope I don't fall asleep!").ToDictionary();

            Assert.AreEqual(Constants.ClientCommands.UserStatus, result[Constants.Arguments.Type]);
            Assert.AreEqual("online", result[Constants.Arguments.Status]);
            Assert.AreEqual("I'm really tired today, hope I don't fall asleep!",
                result[Constants.Arguments.StatusMessage]);
        }

        [TestMethod]
        public void CanHandleCommandWithThreeArguments()
        {
            var result = TestOf("/timeout trouble maker, 1800").ToDictionary();

            Assert.AreEqual(Constants.ClientCommands.ChannelTimeOut, result[Constants.Arguments.Type]);
            Assert.AreEqual("trouble maker", result[Constants.Arguments.Character]);

            result = TestOf("/chattimeout trouble maker, 1800, you're a naughty boy").ToDictionary();

            Assert.AreEqual(Constants.ClientCommands.AdminTimeout, result[Constants.Arguments.Type]);
            Assert.AreEqual("trouble maker", result[Constants.Arguments.Character]);
            Assert.AreEqual("you're a naughty boy", result["reason"]);
        }
    }
}