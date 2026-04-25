namespace PublicFramework
{
    /// <summary>
    /// 캐릭터·몬스터·펫이 공유하는 데이터 공통 계약.
    /// 스킬·버프·데미지 로직이 유닛 타입과 무관하게 동작하도록 식별자와 스탯 참조만 요구한다.
    /// 이름/아이콘은 서브 인터페이스(또는 ItemData 연결) 에서 제공한다.
    /// </summary>
    public interface IUnit
    {
        string UnitId { get; }
        string BaseStatMID { get; }
    }
}
