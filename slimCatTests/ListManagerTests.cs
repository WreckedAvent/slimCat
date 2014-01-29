namespace slimCatTest
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Practices.Prism.Events;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using SimpleJson;
    using Slimcat;
    using Slimcat.Models;
    using Slimcat.Utilities;

    [TestClass]
    public class ListManagerTests
    {
        private readonly ICharacter bookmarkCharacter;
        private readonly ICharacter friendCharacter;
        private readonly ICharacter otherCharacter;
        private readonly ICharacter interestedCharacter;

        private readonly ICharacterManager manager;
        private readonly IEventAggregator eventAggregator;
        private readonly IList<string> bookmarks; 

        public ListManagerTests()
        {
            bookmarkCharacter = new CharacterModel { Name = "Character One" };
            friendCharacter = new CharacterModel { Name = "Character Two" };
            otherCharacter = new CharacterModel { Name = "Character Three" };
            interestedCharacter = new CharacterModel {Name = "Character Four"};

            bookmarks = new[] { bookmarkCharacter.Name };
            var allFriends = new Dictionary<string, IList<string>> {{friendCharacter.Name, null}};

            var account = Mock.Of<IAccount>(acc =>
                acc.AllFriends == allFriends
                && acc.Bookmarks == bookmarks);

            eventAggregator = new EventAggregator();
            manager = new GlobalCharacterManager(account, eventAggregator);
        }

        private void SignOnAllTestCharacters()
        {
            bookmarkCharacter.Status = StatusType.Online;
            friendCharacter.Status = StatusType.Online;
            otherCharacter.Status = StatusType.Online;

            interestedCharacter.Status = StatusType.Looking;
            interestedCharacter.StatusMessage = "Looking for you!";

            manager.SignOn(bookmarkCharacter);
            manager.SignOn(friendCharacter);
            manager.SignOn(otherCharacter);
            manager.SignOn(interestedCharacter);
        }

        private void SignOffAllTestCharacters()
        {
            manager.SignOff(bookmarkCharacter.Name);
            manager.SignOff(friendCharacter.Name);
            manager.SignOff(otherCharacter.Name);
            manager.SignOff(interestedCharacter.Name);
        }

        [TestInitialize]
        public void Initialize()
        {
            eventAggregator.GetEvent<CharacterSelectedLoginEvent>().Publish(null);
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
            Assert.IsTrue(manager.IsOfInterest(bookmarkCharacter.Name, false));
            Assert.IsTrue(manager.IsOfInterest(friendCharacter.Name, false));
            Assert.IsFalse(manager.IsOfInterest(otherCharacter.Name, false));

            Assert.IsFalse(manager.IsOfInterest(bookmarkCharacter.Name));
            Assert.IsFalse(manager.IsOfInterest(friendCharacter.Name));
            Assert.IsFalse(manager.IsOfInterest(otherCharacter.Name));

            SignOnAllTestCharacters();

            Assert.IsTrue(manager.IsOfInterest(bookmarkCharacter.Name));
            Assert.IsTrue(manager.IsOfInterest(friendCharacter.Name));
            Assert.IsFalse(manager.IsOfInterest(otherCharacter.Name));

            Assert.IsTrue(manager.IsOfInterest(bookmarkCharacter.Name, false));
            Assert.IsTrue(manager.IsOfInterest(friendCharacter.Name, false));
            Assert.IsFalse(manager.IsOfInterest(otherCharacter.Name));
        }

        [TestMethod]
        public void CanAddInterestedMark()
        {
            SignOnAllTestCharacters();
            manager.Add(interestedCharacter.Name, ListKind.Interested);
            ShouldBeOnList(ListKind.Interested, interestedCharacter);
        }

        [TestMethod]
        public void CanAddNotInterestedMark()
        {
            SignOnAllTestCharacters();
            manager.Add(interestedCharacter.Name, ListKind.NotInterested);
            ShouldBeOnList(ListKind.NotInterested, interestedCharacter);
        }

        [TestMethod]
        public void CanSetList()
        {
            manager.Set(new[] { interestedCharacter.Name }, ListKind.Interested);
            ShouldBeOnOfflineListOf(ListKind.Interested, interestedCharacter);

            SignOnAllTestCharacters();
            ShouldBeOnList(ListKind.Interested, interestedCharacter);
        }

        [TestMethod]
        public void CanClearList()
        {
            CanSetList();

            manager.Set(new JsonArray(), ListKind.Interested);

            ShouldNotBeOnList(ListKind.Interested, interestedCharacter);
            ShouldNotBeOnOfflineListOf(ListKind.Interested, interestedCharacter);
        }

        [TestMethod]
        public void CanRemoveFromList()
        {
            CanAddInterestedMark();

            manager.Remove(interestedCharacter.Name, ListKind.Interested);

            ShouldNotBeOnList(ListKind.Interested, interestedCharacter);
            ShouldNotBeOnOfflineListOf(ListKind.Interested, interestedCharacter);

            CanAddNotInterestedMark();

            manager.Remove(interestedCharacter.Name, ListKind.NotInterested);

            ShouldNotBeOnList(ListKind.NotInterested, interestedCharacter);
            ShouldNotBeOnOfflineListOf(ListKind.NotInterested, interestedCharacter);
        }

        [TestMethod]
        public void CanFindCharacter()
        {
            SignOnAllTestCharacters();

            var result = manager.Find(interestedCharacter.Name);
            Assert.IsTrue(result.NameEquals(interestedCharacter.Name));
            Assert.IsTrue(result.Status == StatusType.Looking);
            Assert.IsTrue(result.StatusMessage.Equals("Looking for you!"));

            result = manager.Find("Someone not online");
            Assert.IsTrue(result.NameEquals("Someone not online"));
            Assert.IsFalse(result.Status == StatusType.Online);
            Assert.IsFalse(result.StatusMessage.Any());
        }

        [TestMethod]
        public void CharacterCountIsAccurate()
        {
            SignOnAllTestCharacters();
            var correctCount = 4;
            Assert.IsTrue(manager.CharacterCount == correctCount);

            manager.SignOff(interestedCharacter.Name);
            correctCount--;
            Assert.IsTrue(manager.CharacterCount == correctCount);

            manager.SignOn(otherCharacter);
            Assert.IsTrue(manager.CharacterCount == correctCount);

            manager.SignOff(interestedCharacter.Name);
            Assert.IsTrue(manager.CharacterCount == correctCount);

            manager.SignOn(interestedCharacter);
            correctCount++;
            Assert.IsTrue(manager.CharacterCount == correctCount);
        }

        [TestMethod]
        public void GetNamesIsAccurate()
        {
            SignOnAllTestCharacters();

            var result = manager.GetNames(ListKind.Bookmark);

            Assert.IsTrue(result.Count == 1);
            Assert.IsTrue(result.Contains(bookmarkCharacter.Name));
        }

        [TestMethod]
        public void GetAllCharactersIsAccurate()
        {
            CanSetList();

            var result = manager.GetCharacters(ListKind.Interested);

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

            manager.Add(interestedCharacter.Name, ListKind.Interested);

            ShouldNotBeOnList(ListKind.NotInterested, interestedCharacter);
            ShouldBeOnList(ListKind.Interested, interestedCharacter);
        }

        [TestMethod]
        public void InappropriateListReturnsNull()
        {
            var result = manager.GetNames(ListKind.Banned, false);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void InappropriateListDoesNotCrash()
        {
            var result = manager.IsOnList("", ListKind.Banned, false);
            Assert.IsFalse(result);
        }

        #region Helpers
        private void ShouldBeOffline(params ICharacter[] characters)
        {
            ShouldBe(ListKind.Online, false, true, characters);
            Assert.IsFalse(manager.Characters.Intersect(characters).Any());
        }

        private void ShouldBeOnline(params ICharacter[] characters)
        {
            ShouldBe(ListKind.Online, true, true, characters);
            Assert.IsTrue(manager.Characters.Intersect(characters).Count() == characters.Count());
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
                characters.Each(x => Assert.IsTrue(manager.IsOnList(x.Name, listKind, offlineOnly)));
                return;
            }

            characters.Each(x => Assert.IsFalse(manager.IsOnList(x.Name, listKind, offlineOnly)));
        }
        #endregion
    }
}
