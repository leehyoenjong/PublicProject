using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Monster
{
    public class MonsterSystemTests
    {
        private FakeEventBus _eventBus;
        private FakeTimeProvider _timeProvider;
        private FakeRandomProvider _random;
        private FakeDropContext _dropContext;
        private MonsterSystem _system;

        private MonsterInfo _slime;
        private MonsterInfo _boss;
        private DropTableData _slimeDrop;
        private DropTableData _bossDrop;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new FakeEventBus();
            _timeProvider = new FakeTimeProvider(new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc));
            _random = new FakeRandomProvider();
            _dropContext = new FakeDropContext { PlayerLevel = 30 };

            _slime = TestHelpers.MakeMonsterInfo("mon_slime",
                type: MonsterType.Normal,
                level: 1,
                expReward: 10,
                goldReward: 5,
                dropTableMID: "drop_slime",
                hitReactionId: "hit_basic");

            _boss = TestHelpers.MakeMonsterInfo("mon_dragon",
                type: MonsterType.Boss,
                level: 30,
                expReward: 5000,
                goldReward: 3000,
                dropTableMID: "drop_dragon",
                onSpawnEvents: new[] { "Intro:boss", "Cinematic:dragon" },
                onDeathEvents: new[] { "StageClear" },
                hitReactionId: "hit_boss");

            _slimeDrop = TestHelpers.MakeDropTableData("drop_slime",
                TestHelpers.MakeDropEntry(itemMID: 10001, weight: 100, minCount: 5, maxCount: 5));

            _bossDrop = TestHelpers.MakeDropTableData("drop_dragon",
                TestHelpers.MakeDropEntry(itemMID: 10001, weight: 100, minCount: 5000, maxCount: 5000),
                TestHelpers.MakeDropEntry(itemMID: 40001, weight: 100, minCount: 1, maxCount: 1));

            _system = new MonsterSystem(
                dropResolver: new DefaultDropTableResolver(),
                random: _random,
                bestiary: new Bestiary(),
                eventBus: _eventBus,
                timeProvider: _timeProvider);

            _system.Initialize(
                new List<IMonsterInfo> { _slime, _boss },
                new List<IDropTable> { _slimeDrop, _bossDrop },
                eventCatalog: null);
        }

        [Test]
        public void GetInfo_KnownMID_ReturnsInstance()
        {
            Assert.AreSame(_slime, _system.GetInfo("mon_slime"));
        }

        [Test]
        public void GetInfo_Unknown_ReturnsNull()
        {
            Assert.IsNull(_system.GetInfo("mon_unknown"));
        }

        [Test]
        public void Spawn_Known_CreatesInstance_PublishesEvent()
        {
            var inst = _system.Spawn("mon_slime", "slime_001", null, new Vector3(1, 0, 0));

            Assert.IsNotNull(inst);
            Assert.AreEqual("slime_001", inst.InstanceId);
            Assert.AreEqual(1, inst.Level);
            Assert.AreSame(_slime, inst.Info);
            Assert.AreEqual(1, _eventBus.GetPublished<MonsterSpawnedEvent>().Count);
        }

        [Test]
        public void Spawn_TriggersOnSpawnHooks()
        {
            _system.Spawn("mon_dragon", "dragon_001", null, Vector3.zero);

            var spawned = _eventBus.GetPublished<MonsterSpawnedEvent>()[0];
            Assert.AreEqual(2, spawned.TriggeredHookIds.Count);
            Assert.Contains("Intro:boss", new List<string>(spawned.TriggeredHookIds));
            Assert.Contains("Cinematic:dragon", new List<string>(spawned.TriggeredHookIds));

            var hookEvents = _eventBus.GetPublished<MonsterHookTriggeredEvent>();
            Assert.AreEqual(2, hookEvents.Count);
            Assert.AreEqual(MonsterEventKind.Spawn, hookEvents[0].Kind);
        }

        [Test]
        public void Spawn_Duplicate_ReturnsNull()
        {
            _system.Spawn("mon_slime", "slime_001", null, Vector3.zero);
            var second = _system.Spawn("mon_slime", "slime_001", null, Vector3.zero);
            Assert.IsNull(second);
        }

        [Test]
        public void Defeat_Known_GrantsRewardsAndDrops()
        {
            _system.Spawn("mon_dragon", "dragon_001", null, new Vector3(5, 0, 0));
            _random.Enqueue(0); _random.Enqueue(0);  // 두 항목 모두 drop

            DefeatResult result = _system.Defeat("dragon_001", "player_1", _dropContext);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("mon_dragon", result.MonsterMID);
            Assert.AreEqual("player_1", result.KillerInstanceId);
            Assert.AreEqual(5000, result.ExpReward);
            Assert.AreEqual(3000, result.GoldReward);
            Assert.AreEqual(2, result.Drops.Count);
            Assert.AreEqual(new Vector3(5, 0, 0), result.Position);

            Assert.AreEqual(1, _eventBus.GetPublished<MonsterDefeatedEvent>().Count);
        }

        [Test]
        public void Defeat_TriggersOnDeathHooks()
        {
            _system.Spawn("mon_dragon", "dragon_001", null, Vector3.zero);
            _random.Enqueue(0); _random.Enqueue(0);

            DefeatResult result = _system.Defeat("dragon_001", null, _dropContext);

            Assert.AreEqual(1, result.TriggeredHookIds.Count);
            Assert.AreEqual("StageClear", result.TriggeredHookIds[0]);
        }

        [Test]
        public void Defeat_FirstTime_RegistersBestiary_PublishesFirstSeen()
        {
            _system.Spawn("mon_slime", "slime_001", null, Vector3.zero);
            _random.Enqueue(0);

            DefeatResult result = _system.Defeat("slime_001", null, _dropContext);

            Assert.IsTrue(result.IsFirstSeen);
            Assert.IsTrue(_system.Bestiary.IsEntered("mon_slime"));
            Assert.AreEqual(1, _eventBus.GetPublished<MonsterFirstSeenEvent>().Count);
        }

        [Test]
        public void Defeat_SameMonsterTwice_FirstSeenOnce()
        {
            _system.Spawn("mon_slime", "slime_001", null, Vector3.zero);
            _random.Enqueue(0);
            _system.Defeat("slime_001", null, _dropContext);

            _system.Spawn("mon_slime", "slime_002", null, Vector3.zero);
            _random.Enqueue(0);
            DefeatResult result2 = _system.Defeat("slime_002", null, _dropContext);

            Assert.IsFalse(result2.IsFirstSeen);
            Assert.AreEqual(1, _eventBus.GetPublished<MonsterFirstSeenEvent>().Count);
        }

        [Test]
        public void Defeat_RemovesInstance()
        {
            _system.Spawn("mon_slime", "slime_001", null, Vector3.zero);
            _random.Enqueue(0);
            _system.Defeat("slime_001", null, _dropContext);

            Assert.IsNull(_system.Get("slime_001"));
        }

        [Test]
        public void Defeat_Unknown_ReturnsFailure()
        {
            DefeatResult result = _system.Defeat("ghost_999", null, _dropContext);
            Assert.IsFalse(result.Success);
        }

        [Test]
        public void ApplyHit_Known_PublishesHitEvent()
        {
            _system.Spawn("mon_slime", "slime_001", null, Vector3.zero);

            bool ok = _system.ApplyHit("slime_001", 25);

            Assert.IsTrue(ok);
            var hits = _eventBus.GetPublished<MonsterHitEvent>();
            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual(25, hits[0].Damage);
            Assert.AreEqual("hit_basic", hits[0].ReactionId);
        }

        [Test]
        public void ApplyHit_Unknown_ReturnsFalse()
        {
            Assert.IsFalse(_system.ApplyHit("ghost", 10));
        }

        [Test]
        public void ApplyHit_NonPositiveDamage_ReturnsFalse()
        {
            _system.Spawn("mon_slime", "slime_001", null, Vector3.zero);
            Assert.IsFalse(_system.ApplyHit("slime_001", 0));
            Assert.IsFalse(_system.ApplyHit("slime_001", -5));
        }

        [Test]
        public void GetEventEntry_WithCatalog_FindsEntry()
        {
            var catalog = TestHelpers.MakeMonsterEventCatalog(
                TestHelpers.MakeMonsterEventCatalogEntry("StageClear", MonsterEventKind.Death),
                TestHelpers.MakeMonsterEventCatalogEntry("Intro:boss", MonsterEventKind.Spawn)
            );
            _system.Initialize(
                new List<IMonsterInfo> { _slime, _boss },
                new List<IDropTable> { _slimeDrop, _bossDrop },
                catalog);

            var entry = _system.GetEventEntry("StageClear");
            Assert.IsNotNull(entry);
            Assert.AreEqual(MonsterEventKind.Death, entry.Kind);

            Assert.IsNull(_system.GetEventEntry("not_in_catalog"));
        }
    }
}
