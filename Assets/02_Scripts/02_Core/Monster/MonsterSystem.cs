using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터 도메인 런타임 진입점. MonsterInfo/DropTable/EventCatalog 룩업, 인스턴스 관리,
    /// 스폰/처치/피격 처리, 도감 통합. 드롭 해석은 IDropTableResolver 에 위임.
    /// </summary>
    public class MonsterSystem : IMonsterSystem
    {
        private readonly Dictionary<string, IMonsterInfo> _infoByMID = new();
        private readonly Dictionary<string, IDropTable> _dropTables = new();
        private readonly Dictionary<string, MonsterInstance> _instances = new();

        private readonly IDropTableResolver _dropResolver;
        private readonly IRandomProvider _random;
        private readonly IBestiary _bestiary;
        private readonly IEventBus _eventBus;
        private readonly ITimeProvider _timeProvider;
        private MonsterEventCatalog _eventCatalog;

        public MonsterSystem(
            IDropTableResolver dropResolver = null,
            IRandomProvider random = null,
            IBestiary bestiary = null,
            IEventBus eventBus = null,
            ITimeProvider timeProvider = null)
        {
            _dropResolver = dropResolver ?? new DefaultDropTableResolver();
            _random = random ?? new DefaultRandomProvider();
            _bestiary = bestiary ?? new Bestiary();
            _eventBus = eventBus;
            _timeProvider = timeProvider;
            Debug.Log("[MonsterSystem] Init started");
        }

        public IBestiary Bestiary => _bestiary;

        public void Initialize(
            MonsterInfoCollection monsters,
            DropTableDataCollection dropTables,
            MonsterEventCatalog eventCatalog)
        {
            _infoByMID.Clear();
            if (monsters?.Items != null)
            {
                foreach (MonsterInfo info in monsters.Items)
                {
                    if (info == null || string.IsNullOrEmpty(info.MID)) continue;
                    _infoByMID[info.MID] = info;
                }
            }

            _dropTables.Clear();
            if (dropTables?.Items != null)
            {
                foreach (DropTableData t in dropTables.Items)
                {
                    if (t == null || string.IsNullOrEmpty(t.MID)) continue;
                    _dropTables[t.MID] = t;
                }
            }

            _eventCatalog = eventCatalog;

            Debug.Log($"[MonsterSystem] Initialized — monsters: {_infoByMID.Count}, dropTables: {_dropTables.Count}");
        }

        /// <summary>인터페이스 기반 Initialize — 테스트에서 Fake 주입 가능.</summary>
        public void Initialize(
            IReadOnlyList<IMonsterInfo> monsters,
            IReadOnlyList<IDropTable> dropTables,
            MonsterEventCatalog eventCatalog)
        {
            _infoByMID.Clear();
            if (monsters != null)
            {
                for (int i = 0; i < monsters.Count; i++)
                {
                    IMonsterInfo info = monsters[i];
                    if (info == null || string.IsNullOrEmpty(info.MID)) continue;
                    _infoByMID[info.MID] = info;
                }
            }

            _dropTables.Clear();
            if (dropTables != null)
            {
                for (int i = 0; i < dropTables.Count; i++)
                {
                    IDropTable t = dropTables[i];
                    if (t == null || string.IsNullOrEmpty(t.MID)) continue;
                    _dropTables[t.MID] = t;
                }
            }

            _eventCatalog = eventCatalog;

            Debug.Log($"[MonsterSystem] Initialized — monsters: {_infoByMID.Count}, dropTables: {_dropTables.Count}");
        }

        public IMonsterInfo GetInfo(string monsterMID)
        {
            if (string.IsNullOrEmpty(monsterMID)) return null;
            return _infoByMID.TryGetValue(monsterMID, out IMonsterInfo info) ? info : null;
        }

        public IMonsterInstance Get(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            return _instances.TryGetValue(instanceId, out MonsterInstance inst) ? inst : null;
        }

        public IReadOnlyCollection<IMonsterInstance> All => _instances.Values;

        public IDropTable GetDropTable(string dropTableMID)
        {
            if (string.IsNullOrEmpty(dropTableMID)) return null;
            return _dropTables.TryGetValue(dropTableMID, out IDropTable t) ? t : null;
        }

        public MonsterEventCatalogEntry GetEventEntry(string eventId) =>
            _eventCatalog?.GetEntry(eventId);

        public IMonsterInstance Spawn(string monsterMID, string instanceId, IStatContainer stats, Vector3 position)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            IMonsterInfo info = GetInfo(monsterMID);
            if (info == null) return null;
            if (_instances.ContainsKey(instanceId)) return null;

            var inst = new MonsterInstance(instanceId, info, stats);
            inst.SetLevel(info.Level);
            inst.SetPosition(position);
            _instances[instanceId] = inst;

            List<string> triggered = TriggerHooks(info.OnSpawnEvents, MonsterEventKind.Spawn, info.MID, instanceId);

            _eventBus?.Publish(new MonsterSpawnedEvent
            {
                MonsterMID = info.MID,
                InstanceId = instanceId,
                Position = position,
                TriggeredHookIds = triggered,
            });

            Debug.Log($"[MonsterSystem] Spawned: {info.MID} ({instanceId})");
            return inst;
        }

        public DefeatResult Defeat(string instanceId, string killerInstanceId, IDropContext dropContext)
        {
            if (!_instances.TryGetValue(instanceId, out MonsterInstance inst))
            {
                return new DefeatResult { Success = false, InstanceId = instanceId };
            }

            IMonsterInfo info = inst.Info;
            inst.Kill();

            IDropTable table = GetDropTable(info?.DropTableMID);
            DropResult drop = table != null
                ? _dropResolver.Resolve(table, dropContext, _random)
                : new DropResult { Drops = System.Array.Empty<DropItemResult>() };

            DateTime nowUtc = _timeProvider?.NowUtc ?? DateTime.UtcNow;
            bool firstSeen = _bestiary.Register(info?.MID, nowUtc);
            if (firstSeen)
            {
                _eventBus?.Publish(new MonsterFirstSeenEvent { MonsterMID = info.MID });
            }

            List<string> triggered = TriggerHooks(info?.OnDeathEvents, MonsterEventKind.Death, info?.MID, instanceId);

            var result = new DefeatResult
            {
                Success = true,
                MonsterMID = info?.MID,
                InstanceId = instanceId,
                KillerInstanceId = killerInstanceId,
                ExpReward = info?.ExpReward ?? 0,
                GoldReward = info?.GoldReward ?? 0,
                Drops = drop.Drops,
                TriggeredHookIds = triggered,
                Position = inst.Position,
                IsFirstSeen = firstSeen,
            };

            _eventBus?.Publish(new MonsterDefeatedEvent
            {
                MonsterMID = result.MonsterMID,
                InstanceId = result.InstanceId,
                KillerInstanceId = result.KillerInstanceId,
                ExpReward = result.ExpReward,
                GoldReward = result.GoldReward,
                Drops = result.Drops,
                TriggeredHookIds = result.TriggeredHookIds,
                Position = result.Position,
            });

            _instances.Remove(instanceId);

            Debug.Log($"[MonsterSystem] Defeated: {result.MonsterMID} ({instanceId}) drops={result.Drops?.Count ?? 0}");
            return result;
        }

        public bool ApplyHit(string instanceId, int damage)
        {
            if (!_instances.TryGetValue(instanceId, out MonsterInstance inst)) return false;
            if (damage <= 0) return false;

            _eventBus?.Publish(new MonsterHitEvent
            {
                MonsterMID = inst.Info?.MID,
                InstanceId = instanceId,
                Damage = damage,
                ReactionId = inst.Info?.HitReactionId,
            });
            return true;
        }

        private List<string> TriggerHooks(IReadOnlyList<string> eventIds, MonsterEventKind kind, string monsterMID, string instanceId)
        {
            var triggered = new List<string>();
            if (eventIds == null) return triggered;

            for (int i = 0; i < eventIds.Count; i++)
            {
                string id = eventIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                triggered.Add(id);
                _eventBus?.Publish(new MonsterHookTriggeredEvent
                {
                    EventId = id,
                    Kind = kind,
                    MonsterMID = monsterMID,
                    InstanceId = instanceId,
                });
            }
            return triggered;
        }
    }
}
