using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터 런타임. IUnitInstance 공통 + 몬스터 고유(Info/Position).
    /// Position 은 BT 이동(MoveToTarget)·거리 판정용 — 몬스터 한정 노출(캐릭터/펫엔 강제 안 함, ISP).
    /// </summary>
    public interface IMonsterInstance : IUnitInstance
    {
        IMonsterInfo Info { get; }
        Vector3 Position { get; }
        void SetPosition(Vector3 position);
    }
}
