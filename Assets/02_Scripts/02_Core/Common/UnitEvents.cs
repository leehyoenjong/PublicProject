using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 유닛(캐릭터/몬스터/펫) 인스턴스가 씬에 스폰됐을 때 발행. UI 미니맵·타게팅 등이 구독.
    /// </summary>
    public struct UnitSpawnedEvent
    {
        public string InstanceId;
        public string UnitId;
        public Vector3 Position;
    }

    /// <summary>
    /// 유닛 인스턴스가 사망했을 때 발행 (CurrentHP 가 0 이하로 떨어진 첫 시점, 1회 한정).
    /// 보상 분배·재스폰·웨이브 진행 등이 구독.
    /// </summary>
    public struct UnitDiedEvent
    {
        public string InstanceId;
        public string UnitId;
        public string LastDamageSource;
    }

    /// <summary>
    /// 유닛 인스턴스의 CurrentHP 변동 시 발행. HP 바·툴팁 갱신용.
    /// </summary>
    public struct UnitHpChangedEvent
    {
        public string InstanceId;
        public float OldHp;
        public float NewHp;
        public string Source;
    }
}
