using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터 처치 처리 결과. MonsterDefeatedEvent 와 동일 페이로드를 반환값으로도 제공한다.
    /// </summary>
    public struct DefeatResult
    {
        public bool Success;
        public string MonsterMID;
        public string InstanceId;
        public string KillerInstanceId;
        public int ExpReward;
        public int GoldReward;
        public IReadOnlyList<DropItemResult> Drops;
        public IReadOnlyList<string> TriggeredHookIds;
        public Vector3 Position;
        public bool IsFirstSeen;
    }
}
