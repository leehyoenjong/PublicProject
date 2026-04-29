using NUnit.Framework;

namespace PublicFramework.Tests.Character
{
    public class DeckRepositoryTests
    {
        private FakeEventBus _events;
        private DeckConfig _config;
        private DeckRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _events = new FakeEventBus();
            _config = TestHelpers.MakeDeckConfig(maxDecks: 2, partySize: 3);
            _repo = new DeckRepository(_config, _events);
        }

        [Test]
        public void Initialize_CreatesMaxDecksWithSequentialIds()
        {
            Assert.AreEqual(2, _repo.Decks.Count);
            Assert.AreEqual("deck_1", _repo.Decks[0].DeckId);
            Assert.AreEqual("deck_2", _repo.Decks[1].DeckId);
            Assert.AreEqual(3, _repo.Decks[0].PartySize);
        }

        [Test]
        public void Get_UnknownDeck_ReturnsNull()
        {
            Assert.IsNull(_repo.Get("deck_X"));
        }

        [Test]
        public void SetMember_Success_AssignsAndPublishes()
        {
            Assert.AreEqual(DeckResult.Success, _repo.SetMember("deck_1", 0, "hero_A"));
            Assert.AreEqual("hero_A", _repo.Get("deck_1").Slots[0]);
            Assert.AreEqual(1, _events.GetPublished<DeckMemberChangedEvent>().Count);
        }

        [Test]
        public void SetMember_UnknownDeck_Fails()
        {
            Assert.AreEqual(DeckResult.DeckNotFound, _repo.SetMember("deck_X", 0, "hero_A"));
        }

        [Test]
        public void SetMember_SlotOutOfRange_Fails()
        {
            Assert.AreEqual(DeckResult.SlotOutOfRange, _repo.SetMember("deck_1", 5, "hero_A"));
            Assert.AreEqual(DeckResult.SlotOutOfRange, _repo.SetMember("deck_1", -1, "hero_A"));
        }

        [Test]
        public void SetMember_EmptyInstance_Fails()
        {
            Assert.AreEqual(DeckResult.EmptyInstance, _repo.SetMember("deck_1", 0, ""));
            Assert.AreEqual(DeckResult.EmptyInstance, _repo.SetMember("deck_1", 0, null));
        }

        [Test]
        public void SetMember_DuplicateDefault_Fails()
        {
            _repo.SetMember("deck_1", 0, "hero_A");
            Assert.AreEqual(DeckResult.DuplicateNotAllowed, _repo.SetMember("deck_1", 1, "hero_A"));
        }

        [Test]
        public void SetMember_SameSlotReAssign_SucceedsWithoutDuplicate()
        {
            _repo.SetMember("deck_1", 0, "hero_A");
            Assert.AreEqual(DeckResult.Success, _repo.SetMember("deck_1", 0, "hero_A"));
        }

        [Test]
        public void SetMember_AllowDuplicate_PermitsAcrossSlots()
        {
            var conf = TestHelpers.MakeDeckConfig(maxDecks: 1, partySize: 3, allowDuplicate: true);
            var r = new DeckRepository(conf, _events);
            r.SetMember("deck_1", 0, "hero_A");
            Assert.AreEqual(DeckResult.Success, r.SetMember("deck_1", 1, "hero_A"));
        }

        [Test]
        public void RemoveMember_EmptySlot_ReturnsMemberNotFound()
        {
            Assert.AreEqual(DeckResult.MemberNotFound, _repo.RemoveMember("deck_1", 0));
        }

        [Test]
        public void RemoveMember_OccupiedSlot_Clears_AndPublishes()
        {
            _repo.SetMember("deck_1", 0, "hero_A");
            _events.Clear();
            Assert.AreEqual(DeckResult.Success, _repo.RemoveMember("deck_1", 0));
            Assert.IsNull(_repo.Get("deck_1").Slots[0]);
            Assert.AreEqual(1, _events.GetPublished<DeckMemberChangedEvent>().Count);
        }

        [Test]
        public void RemoveMember_LeaderRemoved_AlsoClearsLeader()
        {
            _repo.SetMember("deck_1", 0, "hero_A");
            _repo.SetLeader("deck_1", "hero_A");
            _events.Clear();
            _repo.RemoveMember("deck_1", 0);
            Assert.IsNull(_repo.Get("deck_1").LeaderInstanceId);
            Assert.AreEqual(1, _events.GetPublished<DeckLeaderChangedEvent>().Count);
        }

        [Test]
        public void SetLeader_NonMember_Fails()
        {
            Assert.AreEqual(DeckResult.MemberNotFound, _repo.SetLeader("deck_1", "hero_A"));
        }

        [Test]
        public void SetLeader_Member_Assigns_AndPublishes()
        {
            _repo.SetMember("deck_1", 0, "hero_A");
            _events.Clear();
            Assert.AreEqual(DeckResult.Success, _repo.SetLeader("deck_1", "hero_A"));
            Assert.AreEqual("hero_A", _repo.Get("deck_1").LeaderInstanceId);
            Assert.AreEqual(1, _events.GetPublished<DeckLeaderChangedEvent>().Count);
        }

        [Test]
        public void SetLeader_EmptyWhenNoLeader_ReturnsMemberNotFound()
        {
            Assert.AreEqual(DeckResult.MemberNotFound, _repo.SetLeader("deck_1", null));
        }

        [Test]
        public void SetLeader_EmptyWhenLeaderExists_ClearsLeader()
        {
            _repo.SetMember("deck_1", 0, "hero_A");
            _repo.SetLeader("deck_1", "hero_A");
            _events.Clear();
            Assert.AreEqual(DeckResult.Success, _repo.SetLeader("deck_1", null));
            Assert.IsNull(_repo.Get("deck_1").LeaderInstanceId);
            Assert.AreEqual(1, _events.GetPublished<DeckLeaderChangedEvent>().Count);
        }

        [Test]
        public void Rename_Whitespace_Fails()
        {
            Assert.AreEqual(DeckResult.InvalidName, _repo.Rename("deck_1", "   "));
        }

        [Test]
        public void Rename_Success_ChangesName_AndPublishes()
        {
            Assert.AreEqual(DeckResult.Success, _repo.Rename("deck_1", "보스용"));
            Assert.AreEqual("보스용", _repo.Get("deck_1").Name);
            Assert.AreEqual(1, _events.GetPublished<DeckRenamedEvent>().Count);
        }

        [Test]
        public void Clear_EmptiesAllSlots_AndPublishes()
        {
            _repo.SetMember("deck_1", 0, "hero_A");
            _repo.SetMember("deck_1", 1, "hero_B");
            _events.Clear();
            Assert.AreEqual(DeckResult.Success, _repo.Clear("deck_1"));
            Assert.IsTrue(_repo.Get("deck_1").IsEmpty);
            Assert.AreEqual(1, _events.GetPublished<DeckClearedEvent>().Count);
        }
    }
}
