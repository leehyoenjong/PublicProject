using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터 도메인 진입점. MonsterInfo 룩업, 인스턴스 관리, 스폰/처치 처리, 도감 통합.
    /// 드롭 계산은 <see cref="IDropTableResolver"/>, 도감은 <see cref="IBestiary"/> 에 위임.
    /// </summary>
    public interface IMonsterSystem : IService
    {
        IMonsterInfo GetInfo(string monsterMID);
        IMonsterInstance Get(string instanceId);
        IReadOnlyCollection<IMonsterInstance> All { get; }

        IDropTable GetDropTable(string dropTableMID);
        MonsterEventCatalogEntry GetEventEntry(string eventId);

        IMonsterInstance Spawn(string monsterMID, string instanceId, IStatContainer stats, UnityEngine.Vector3 position);
        DefeatResult Defeat(string instanceId, string killerInstanceId, IDropContext dropContext);
        bool ApplyHit(string instanceId, int damage);

        IBestiary Bestiary { get; }
    }
}
