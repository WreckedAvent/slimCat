#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommandDefinitions.cs">
//    Copyright (c) 2013, Justin Kadrovach, All rights reserved.
//   
//    This source is subject to the Simplified BSD License.
//    Please see the License.txt file for more information.
//    All other rights reserved.
//    
//    THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
//    KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//    IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//    PARTICULAR PURPOSE.
// </copyright>
//  --------------------------------------------------------------------------------------------------------------------

#endregion

using System;
using System.Collections.Generic;
using slimCat.Models;
using slimCat.Utilities;

namespace slimCatTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UserCommandTest
    {
        private const string type = Constants.Arguments.Type;
        private static IDictionary<string, object> GetCommand(string name, IList<string> args = null, string channel = null)
        {
            return CommandDefinitions.CreateCommand(name, args, channel).ToDictionary();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void UnknownCommandsThrowExceptions()
        {
            GetCommand("zzz");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MissingArgumentsThrowExceptions()
        {
            GetCommand("priv");
        }

        [TestMethod]
        public void CorrectCommandWithNoArgumentsWorks()
        {
            var cmnd = GetCommand("close");
            Assert.AreEqual(1, cmnd.Keys.Count);
            Assert.IsTrue(cmnd[type].Equals("close"));
        }

        [TestMethod]
        public void CorrectCommandWithOneArgumentWorks()
        {
            var cmnd = GetCommand("priv", new[] { "foo" });

            Assert.AreEqual(2, cmnd.Keys.Count);
            Assert.AreEqual("priv", cmnd[type]);
            Assert.AreEqual("foo", cmnd["character"]);
        }

        [TestMethod]
        public void CorrectCommandAliasWorks()
        {
            var cmnd = GetCommand("pm", new[] {"foo"});

            Assert.AreEqual(2, cmnd.Keys.Count);
            Assert.AreEqual("priv", cmnd[type]);
            Assert.AreEqual("foo", cmnd["character"]);
        }

        [TestMethod]
        public void CommandWithOverrideWorks()
        {
            var cmnd = GetCommand("online", new []{"foobar"});

            Assert.AreEqual(3, cmnd.Keys.Count);
            Assert.AreEqual("foobar", cmnd["statusmsg"]);
            Assert.AreEqual("STA", cmnd[type]);
        }

        [TestMethod]
        public void CommandWithOnlyOverrideWorks()
        {
            var cmd = GetCommand("bottle", null, "foobar");

            Assert.AreEqual(3, cmd.Keys.Count);
            Assert.AreEqual("RLL", cmd[type]);
            Assert.AreEqual("foobar", cmd["channel"]);
            Assert.AreEqual("bottle", cmd["dice"]);
        }
    }
}
