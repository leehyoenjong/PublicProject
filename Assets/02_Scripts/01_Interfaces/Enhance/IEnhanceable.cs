namespace PublicFramework
{
    /// <summary>
    /// 강화 가능한 인스턴스의 공통 계약. 장비/캐릭터/펫 등이 IEnhanceable 을 구현해 EnhanceSystem 통과.
    /// 모든 EnhanceType(Level/Grade/Transcend/Awakening) 의 핵심 멤버를 노출 — Strategy 가 구체 타입을 모르고도 작업 가능.
    /// AwakeningSlots 는 강화 비대상 도메인에선 null 또는 빈 배열로 둘 수 있다.
    /// </summary>
    public interface IEnhanceable
    {
        string InstanceId { get; }
        int Level { get; set; }
        int Grade { get; set; }
        int TranscendStep { get; set; }
        int PityCount { get; set; }
        AwakeningSlotData[] AwakeningSlots { get; set; }
    }
}
