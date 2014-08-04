#region Copyright

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListManagerTests.cs">
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
using slimCat.Models;
using slimCat.Utilities;

namespace slimCatTest
{
    #region Usings
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    #endregion

    [TestClass]
    public class UtiltiesTest
    {
        [TestMethod]
        public void NameEqualsWorks()
        {
            var model = new CharacterModel { Name = "foobarbaz" };

            Assert.IsTrue(model.NameEquals("foobarbaz"));
            Assert.IsFalse(model.NameEquals("blah"));
            Assert.IsFalse(model.NameEquals("bar"));
        }

        [TestMethod]
        public void NamesContainsWorks()
        {
            var model = new CharacterModel { Name = "foobarbaz" };

            Assert.IsTrue(model.NameContains("bar"));
            Assert.IsTrue(model.NameContains("foobarbaz"));
            Assert.IsFalse(model.NameContains("blah"));
        }

        [TestMethod]
        public void StripPunctuationWorks()
        {
            Assert.AreEqual("bar!".StripPunctuationAtEnd(), "bar");
            Assert.AreEqual("bar.".StripPunctuationAtEnd(), "bar");
            Assert.AreEqual("bar,".StripPunctuationAtEnd(), "bar");

            Assert.AreEqual("it's".StripPunctuationAtEnd(), "it's");


            Assert.AreEqual("bar! foo".StripPunctuationAtEnd(), "bar! foo");
            Assert.AreEqual("bar, foo".StripPunctuationAtEnd(), "bar, foo");
            Assert.AreEqual("bar. foo".StripPunctuationAtEnd(), "bar. foo");
        }

        [TestMethod]
        public void MakeSafeFolderPathWorks()
        {
            const string character = "character name";

            Func<string, string, string> getPath = (title, id) => StaticFunctions.MakeSafeFolderPath(character, title, id); 

            Assert.IsTrue(getPath("bar", "bar").Contains("character name\\bar"));
            Assert.IsTrue(getPath("bar", "ADH-2").Contains("character name\\bar (ADH-2)"));

            Assert.IsTrue(getPath("bar\\baz\\boo", "ADH-3").Contains("\\barbazboo (ADH-3)"));
            Assert.IsTrue(getPath("bar*baz_boo", "ADH-4").Contains("\\barbaz_boo (ADH-4)"));
            Assert.IsTrue(getPath("bar'baz?boo", "ADH-5").Contains("\\bar'bazboo (ADH-5)"));
            Assert.IsTrue(getPath(".bar<baz>boo|", "ADH-6").Contains("\\barbazboo (ADH-6)"));

            Assert.IsTrue(getPath("bar\\baz/boo", "bar\\baz/boo").Contains("character name\\bar-baz-boo"));
        }

        [TestMethod]
        public void FirstMatchWorks()
        {
            var noMatch = string.Empty;
            Func<string, string, string> getMatch = (context, needle) => context.FirstMatch(needle).Item2;

            // basic logic
            Assert.AreEqual(getMatch("foo", "foo"), "foo");
            Assert.AreEqual(getMatch("foo", "."), noMatch);
            Assert.AreEqual(getMatch("foo", "o"), noMatch);

            const string simple = "this is a simple string.";
            Assert.AreEqual(getMatch(simple, "simple"), simple);
            Assert.AreEqual(getMatch(simple, "is a simple"), simple);
            Assert.AreEqual(getMatch(simple, simple), simple);

            const string punctuation = "this is a string,with! someone's punctuation stuck in it.";
            Assert.AreEqual(getMatch(punctuation, "someone"), punctuation);
            Assert.AreEqual(getMatch(punctuation, "someone's"), punctuation);
            Assert.AreEqual(getMatch(punctuation, "with"), punctuation);

            const string looong =
            "start this is a really long string is a really long. string is a really long string is a really long string needle string is a really long string is a really long string end";

            Assert.IsTrue(getMatch(looong, "really long").Contains("is a really long string"));
            Assert.IsTrue(getMatch(looong, "needle").Contains("needle"));

            Assert.IsTrue(getMatch(looong, "start").StartsWith("start"));
            Assert.IsTrue(getMatch(looong, "start").EndsWith("..."));

            Assert.IsTrue(getMatch(looong, "end").EndsWith("end"));
            Assert.IsTrue(getMatch(looong, "end").StartsWith("..."));
        }
    }
}
