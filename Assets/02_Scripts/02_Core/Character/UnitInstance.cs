namespace PublicFramework
{
    /// <summary>
    /// 캐릭터·몬스터·펫 공통 런타임 베이스. 서브 클래스가 Unit 프로퍼티를 채운다.
    /// 현재는 Character 폴더에 위치하지만 Monster/Pet 도메인 착수 시점에 Common 으로 이동 예정.
    /// </summary>
    public abstract class UnitInstance : IUnitInstance
    {
        public string InstanceId { get; }
        public abstract IUnit Unit { get; }
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public IStatContainer Stats { get; }
        public bool IsAlive { get; private set; } = true;

        protected UnitInstance(string instanceId, IStatContainer stats)
        {
            InstanceId = instanceId;
            Stats = stats;
        }

        public virtual void SetLevel(int level)
        {
            Level = level < 1 ? 1 : level;
        }

        public virtual void AddExperience(int amount)
        {
            if (amount <= 0) return;
            Experience += amount;
        }

        public virtual void Kill() => IsAlive = false;
        public virtual void Revive() => IsAlive = true;
    }
}
