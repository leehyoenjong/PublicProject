namespace PublicFramework
{
    /// <summary>
    /// 몬스터 런타임. IUnitInstance 공통 + 몬스터 고유(Info).
    /// </summary>
    public interface IMonsterInstance : IUnitInstance
    {
        IMonsterInfo Info { get; }
    }
}
