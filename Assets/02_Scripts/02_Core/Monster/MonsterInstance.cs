using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 몬스터 런타임. 스폰/처치 등 상태 변경은 MonsterSystem 경유 권장.
    /// </summary>
    public class MonsterInstance : UnitInstance, IMonsterInstance
    {
        private readonly IMonsterInfo _info;

        public MonsterInstance(string instanceId, IMonsterInfo info, IStatContainer stats)
            : base(instanceId, stats)
        {
            _info = info;
        }

        public override IUnit Unit => _info;
        public IMonsterInfo Info => _info;
        public Vector3 Position { get; set; }

        public void SetPosition(Vector3 position) => Position = position;
    }
}
