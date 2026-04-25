namespace PublicFramework
{
    /// <summary>
    /// 캐릭터·몬스터·펫 런타임 공통.
    /// </summary>
    public interface IUnitInstance
    {
        string InstanceId { get; }
        IUnit Unit { get; }
        int Level { get; }
        int Experience { get; }
        IStatContainer Stats { get; }
        bool IsAlive { get; }
    }
}
