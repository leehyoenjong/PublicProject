namespace PublicFramework
{
    /// <summary>드롭 테이블을 컨텍스트에 따라 해석하여 결과를 산출하는 전략. (Gacha 의 IDropResolver 와는 무관.)</summary>
    public interface IDropTableResolver
    {
        DropResult Resolve(IDropTable table, IDropContext context, IRandomProvider random);
    }

    /// <summary>드롭 계산에 필요한 외부 상태(플레이어 레벨, 누적 드롭).</summary>
    public interface IDropContext
    {
        int PlayerLevel { get; }
        int GetDropCount(int itemMID);
    }

    /// <summary>난수 추상화. 테스트에서 결정론적 시퀀스 주입 가능.</summary>
    public interface IRandomProvider
    {
        int NextInt(int maxExclusive);
        int NextInt(int minInclusive, int maxExclusive);
    }
}
