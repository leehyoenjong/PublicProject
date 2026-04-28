namespace PublicFramework
{
    /// <summary>
    /// 스테이지 진입 컨텍스트. 소탕/자동전투/파티 정보 등.
    /// </summary>
    public struct StageContext
    {
        public string StageId;
        public bool IsSweep;
        public bool AutoBattle;
        public string[] PartyCharacterIds;
    }
}
