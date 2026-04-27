using System;

namespace PublicFramework
{
    /// <summary>
    /// 스탯 변동 1건 기록. 디버깅/전투 로그/데미지 미터 활용.
    /// </summary>
    public struct StatHistoryEntry
    {
        public DateTime Timestamp;
        public StatType Type;
        public string CustomKey;       // 커스텀 스탯이면 키, 아니면 null
        public float OldValue;
        public float NewValue;
        public ModifierSource Source;
    }
}
