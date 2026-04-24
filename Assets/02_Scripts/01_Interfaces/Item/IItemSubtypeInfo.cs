namespace PublicFramework
{
    /// <summary>
    /// 하이브리드 SO 서브타입 베이스 마커 인터페이스.
    /// EquipmentInfo / CharacterInfo / PetInfo / RelicInfo 등이 이 인터페이스를 구현하고
    /// ItemData.subtypeRef 에 할당된다. Category 가 subtypeRef 와 일치하는지 검증 용도로 쓴다.
    /// </summary>
    public interface IItemSubtypeInfo
    {
        ItemCategory OwnerCategory { get; }
    }
}

