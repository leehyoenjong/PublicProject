using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 스탯 1개의 소스별 기여도 분해. 디버깅/툴팁 표시 용도.
    /// 최종값과 각 단계별 가산/배수 합계를 함께 제공.
    /// </summary>
    public struct StatDecomposition
    {
        public StatType Type;
        public string CustomKey;       // 커스텀 스탯이면 키, 아니면 null
        public float BaseValue;
        public float FlatTotal;
        public float PercentTotal;     // 0.1 = +10%
        public float MultiplicativeTotal;  // 1.5 = ×1.5
        public float FinalValue;
        public IReadOnlyList<StatContribution> Contributions;
    }

    /// <summary>
    /// 단일 Modifier 의 기여도. 분해 API 결과에 누적.
    /// </summary>
    public struct StatContribution
    {
        public StatLayer Layer;
        public float Value;
        public ModifierSource Source;
        public string SourceLabel;     // ToolTip 용 사람 가독 라벨 (예: "Sword of Truth", "Frenzy Buff")
    }
}
