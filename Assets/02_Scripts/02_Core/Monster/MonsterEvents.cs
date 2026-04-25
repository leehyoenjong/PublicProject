using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>몬스터 스폰. onSpawnEvents 트리거 직후 발행.</summary>
    public struct MonsterSpawnedEvent
    {
        public string MonsterMID;
        public string InstanceId;
        public Vector3 Position;
        public IReadOnlyList<string> TriggeredHookIds;
    }

    /// <summary>몬스터 처치. 드롭 결과 + 보상 + onDeathEvents 트리거 결과 포함.</summary>
    public struct MonsterDefeatedEvent
    {
        public string MonsterMID;
        public string InstanceId;
        public string KillerInstanceId;
        public int ExpReward;
        public int GoldReward;
        public IReadOnlyList<DropItemResult> Drops;
        public IReadOnlyList<string> TriggeredHookIds;
        public Vector3 Position;
    }

    /// <summary>몬스터 피격(데미지 적용 후).</summary>
    public struct MonsterHitEvent
    {
        public string MonsterMID;
        public string InstanceId;
        public int Damage;
        public string ReactionId;
    }

    /// <summary>도감 첫 등록.</summary>
    public struct MonsterFirstSeenEvent
    {
        public string MonsterMID;
    }

    /// <summary>훅(EventId) 발화. EventSystem 핸들러 추적용.</summary>
    public struct MonsterHookTriggeredEvent
    {
        public string EventId;
        public MonsterEventKind Kind;
        public string MonsterMID;
        public string InstanceId;
    }
}
