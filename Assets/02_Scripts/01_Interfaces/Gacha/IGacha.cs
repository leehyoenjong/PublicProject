using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 가챠(뽑기) 설정 계약. GachaData(SO) 가 구현.
    /// 시트 3탭(본체 + GachaTier + GachaDrop) 병합 결과.
    /// </summary>
    public interface IGacha
    {
        string MID { get; }
        int DisplayNameKey { get; }
        int DescriptionKey { get; }
        Sprite Icon { get; }

        PaymentType PaymentType { get; }
        int Cost1Item { get; }
        int Cost1Amount { get; }
        int Cost10Item { get; }
        int Cost10Amount { get; }

        int DailyLimit { get; }
        int PeriodLimit { get; }
        int LifetimeLimit { get; }

        bool Bonus11th { get; }
        GuaranteedTier BonusGuaranteedTier { get; }

        int PitySoftCount { get; }
        int PityHardCount { get; }
        int PityPickupCount { get; }

        GachaSubtype SubtypeType { get; }
        string SubtypeRef { get; }

        bool IsActive { get; }

        IReadOnlyList<GachaTierEntry> Tiers { get; }
        IReadOnlyList<GachaDropEntry> Drops { get; }
    }
}
