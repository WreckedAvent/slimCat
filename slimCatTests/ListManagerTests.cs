#region Copyright

// <copyright file="ListManagerTests.cs">
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

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using slimCat;
    using slimCat.Models;
    using slimCat.Utilities;
    using SimpleJson;

    #endregion

    [TestClass]
    public class ListManagerTests
    {
        private const string CurrentCharacter = "current character";
        private readonly ICharacter bookmarkCharacter;
        private readonly IList<string> bookmarks;
        private readonly IEventAggregator eventAggregator;
        private readonly ICharacter friendCharacter;
        private readonly ICharacter interestedCharacter;
        private readonly ICharacterManager characters;
        private readonly ICharacter otherCharacter;

        public ListManagerTests()
        {
            bookmarkCharacter = new CharacterModel {Name = "Character One"};
            friendCharacter = new CharacterModel {Name = "Character Two"};
            otherCharacter = new CharacterModel {Name = "Character Three"};
            interestedCharacter = new CharacterModel {Name = "Character Four"};

            bookmarks = new[] {bookmarkCharacter.Name};
            var allFriends = new Dictionary<string, IList<string>> {{friendCharacter.Name, new[] {CurrentCharacter}}};

            var account = Mock.Of<IAccount>(acc =>
                acc.AllFriends == allFriends
                && acc.Bookmarks == bookmarks
                && acc.Characters == new ObservableCollection<string> {CurrentCharacter});

            eventAggregator = new EventAggregator();
            characters = new GlobalCharacterManager(account, eventAggregator);
        }

        private void SignOnAllTestCharacters()
        {
            bookmarkCharacter.Status = StatusType.Online;
            friendCharacter.Status = StatusType.Online;
            otherCharacter.Status = StatusType.Online;

            interestedCharacter.Status = StatusType.Looking;
            interestedCharacter.StatusMessage = "Looking for you!";

            characters.SignOn(bookmarkCharacter);
            characters.SignOn(friendCharacter);
            characters.SignOn(otherCharacter);
            characters.SignOn(interestedCharacter);
        }

        private void SignOffAllTestCharacters()
        {
            characters.SignOff(bookmarkCharacter.Name);
            characters.SignOff(friendCharacter.Name);
            characters.SignOff(otherCharacter.Name);
            characters.SignOff(interestedCharacter.Name);
        }

        [TestInitialize]
        public void Initialize()
        {
            eventAggregator.GetEvent<CharacterSelectedLoginEvent>().Publish("testing character");
        }

        [TestMethod]
        public void ManagerInitializesCorrectly()
        {
            ShouldBeOnOfflineListOf(ListKind.Bookmark, bookmarkCharacter);
            ShouldBeOnOfflineListOf(ListKind.Friend, friendCharacter);
        }

        [TestMethod]
        public void ManagerFiltersOfflineCorrectly()
        {
            ShouldBeOnOfflineListOf(ListKind.Bookmark, bookmarkCharacter);
            ShouldBeOffline(bookmarkCharacter);
            ShouldNotBeOnList(ListKind.Bookmark, bookmarkCharacter);
        }

        [TestMethod]
        public void ManagerSignsOnCorrectly()
        {
            ShouldBeOnOfflineListOf(ListKind.Bookmark, bookmarkCharacter);
            ShouldBeOffline(bookmarkCharacter);

            SignOnAllTestCharacters();

            ShouldBeOnList(ListKind.Bookmark, bookmarkCharacter);
            ShouldBeOnline(bookmarkCharacter);
        }

        [TestMethod]
        public void ManagerSignsOffCorrectly()
        {
            SignOnAllTestCharacters();
            ShouldBeOnline(otherCharacter, friendCharacter, bookmarkCharacter);

            SignOffAllTestCharacters();
            ShouldBeOffline(otherCharacter, friendCharacter, bookmarkCharacter);
        }

        [TestMethod]
        public void IsOfInterestWorksCorrectly()
        {
            Assert.IsTrue(characters.IsOfInterest(bookmarkCharacter.Name, false));
            Assert.IsTrue(characters.IsOfInterest(friendCharacter.Name, false));
            Assert.IsFalse(characters.IsOfInterest(otherCharacter.Name, false));

            Assert.IsFalse(characters.IsOfInterest(bookmarkCharacter.Name));
            Assert.IsFalse(characters.IsOfInterest(friendCharacter.Name));
            Assert.IsFalse(characters.IsOfInterest(otherCharacter.Name));

            SignOnAllTestCharacters();

            Assert.IsTrue(characters.IsOfInterest(bookmarkCharacter.Name));
            Assert.IsTrue(characters.IsOfInterest(friendCharacter.Name));
            Assert.IsFalse(characters.IsOfInterest(otherCharacter.Name));

            Assert.IsTrue(characters.IsOfInterest(bookmarkCharacter.Name, false));
            Assert.IsTrue(characters.IsOfInterest(friendCharacter.Name, false));
            Assert.IsFalse(characters.IsOfInterest(otherCharacter.Name));
        }

        [TestMethod]
        public void CanAddInterestedMark()
        {
            SignOnAllTestCharacters();
            characters.Add(interestedCharacter.Name, ListKind.Interested, true);
            ShouldBeOnList(ListKind.Interested, interestedCharacter);
        }

        [TestMethod]
        public void CanAddNotInterestedMark()
        {
            SignOnAllTestCharacters();
            characters.Add(interestedCharacter.Name, ListKind.NotInterested, true);
            ShouldBeOnList(ListKind.NotInterested, interestedCharacter);
        }

        [TestMethod]
        public void CanSetList()
        {
            characters.Set(new[] {interestedCharacter.Name}, ListKind.Interested);
            ShouldBeOnOfflineListOf(ListKind.Interested, interestedCharacter);

            SignOnAllTestCharacters();
            ShouldBeOnList(ListKind.Interested, interestedCharacter);
        }

        [TestMethod]
        public void CanClearList()
        {
            CanSetList();

            characters.Set(new JsonArray(), ListKind.Interested);

            ShouldNotBeOnList(ListKind.Interested, interestedCharacter);
            ShouldNotBeOnOfflineListOf(ListKind.Interested, interestedCharacter);
        }

        [TestMethod]
        public void CanRemoveFromList()
        {
            CanAddInterestedMark();

            characters.Remove(interestedCharacter.Name, ListKind.Interested, true);

            ShouldNotBeOnList(ListKind.Interested, interestedCharacter);
            ShouldNotBeOnOfflineListOf(ListKind.Interested, interestedCharacter);

            CanAddNotInterestedMark();

            characters.Remove(interestedCharacter.Name, ListKind.NotInterested, true);

            ShouldNotBeOnList(ListKind.NotInterested, interestedCharacter);
            ShouldNotBeOnOfflineListOf(ListKind.NotInterested, interestedCharacter);
        }

        [TestMethod]
        public void CanFindCharacter()
        {
            SignOnAllTestCharacters();

            var result = characters.Find(interestedCharacter.Name);
            Assert.IsTrue(result.NameEquals(interestedCharacter.Name));
            Assert.IsTrue(result.Status == StatusType.Looking);
            Assert.IsTrue(result.StatusMessage.Equals("Looking for you!"));

            result = characters.Find("Someone not online");
            Assert.IsTrue(result.NameEquals("Someone not online"));
            Assert.IsFalse(result.Status == StatusType.Online);
            Assert.IsFalse(result.StatusMessage.Any());
        }

        [TestMethod]
        public void CharacterCountIsAccurate()
        {
            SignOnAllTestCharacters();
            var correctCount = 4;
            Assert.IsTrue(characters.CharacterCount == correctCount);

            characters.SignOff(interestedCharacter.Name);
            correctCount--;
            Assert.IsTrue(characters.CharacterCount == correctCount);

            characters.SignOn(otherCharacter);
            Assert.IsTrue(characters.CharacterCount == correctCount);

            characters.SignOff(interestedCharacter.Name);
            Assert.IsTrue(characters.CharacterCount == correctCount);

            characters.SignOn(interestedCharacter);
            correctCount++;
            Assert.IsTrue(characters.CharacterCount == correctCount);
        }

        [TestMethod]
        public void GetNamesIsAccurate()
        {
            SignOnAllTestCharacters();

            var result = characters.GetNames(ListKind.Bookmark);

            Assert.IsTrue(result.Count == 1);
            Assert.IsTrue(result.Contains(bookmarkCharacter.Name));
        }

        [TestMethod]
        public void GetAllCharactersIsAccurate()
        {
            CanSetList();

            var result = characters.GetCharacters(ListKind.Interested);

            Assert.IsTrue(result.Count == 1);
            Assert.IsTrue(result.Contains(interestedCharacter));
            Assert.IsTrue(result.First().StatusMessage == interestedCharacter.StatusMessage);
        }

        [TestMethod]
        public void CannotBeOnBothInterestedAndNot()
        {
            CanAddInterestedMark();
            CanAddNotInterestedMark();

            ShouldNotBeOnList(ListKind.Interested, interestedCharacter);

            characters.Add(interestedCharacter.Name, ListKind.Interested, true);

            ShouldNotBeOnList(ListKind.NotInterested, interestedCharacter);
            ShouldBeOnList(ListKind.Interested, interestedCharacter);
        }

        [TestMethod]
        public void InappropriateListReturnsNull()
        {
            var result = characters.GetNames(ListKind.Banned, false);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void InappropriateListDoesNotCrash()
        {
            var result = characters.IsOnList("", ListKind.Banned, false);
            Assert.IsFalse(result);
        }

        #region Helpers

        private void ShouldBeOffline(params ICharacter[] characters)
        {
            ShouldBe(ListKind.Online, false, true, characters);
            Assert.IsFalse(this.characters.Characters.Intersect(characters).Any());
        }

        private void ShouldBeOnline(params ICharacter[] characters)
        {
            ShouldBe(ListKind.Online, true, true, characters);
            Assert.IsTrue(this.characters.Characters.Intersect(characters).Count() == characters.Count());
        }

        private void ShouldBeOnList(ListKind list, params ICharacter[] characters)
        {
            ShouldBe(list, true, true, characters);
        }

        private void ShouldNotBeOnList(ListKind list, params ICharacter[] characters)
        {
            ShouldBe(list, false, true, characters);
        }

        private void ShouldBeOnOfflineListOf(ListKind list, params ICharacter[] characters)
        {
            ShouldBe(list, true, false, characters);
        }

        private void ShouldNotBeOnOfflineListOf(ListKind list, params ICharacter[] characters)
        {
            ShouldBe(list, false, false, characters);
        }

        private void ShouldBe(ListKind listKind, bool on, bool offlineOnly, params ICharacter[] characters)
        {
            if (on)
            {
                characters.Each(x => Assert.IsTrue(this.characters.IsOnList(x.Name, listKind, offlineOnly)));
                return;
            }

            characters.Each(x => Assert.IsFalse(this.characters.IsOnList(x.Name, listKind, offlineOnly)));
        }

        #endregion
    }
}