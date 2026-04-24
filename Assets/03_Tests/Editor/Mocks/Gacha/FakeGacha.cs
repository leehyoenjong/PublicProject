using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IGacha POCO. 모든 필드를 setter 로 조합.</summary>
    public class FakeGacha : IGacha
    {
        public string MID { get; set; } = "gacha_test";
        public int DisplayNameKey { get; set; }
        public int DescriptionKey { get; set; }
        public Sprite Icon { get; set; }
        public PaymentType PaymentType { get; set; } = PaymentType.Item;
        public int Cost1Item { get; set; } = 9001;
        public int Cost1Amount { get; set; } = 100;
        public int Cost10Item { get; set; } = 9001;
        public int Cost10Amount { get; set; } = 900;
        public int DailyLimit { get; set; }
        public int PeriodLimit { get; set; }
        public int LifetimeLimit { get; set; }
        public bool Bonus11th { get; set; }
        public GuaranteedTier BonusGuaranteedTier { get; set; } = GuaranteedTier.None;
        public int PitySoftCount { get; set; }
        public int PityHardCount { get; set; }
        public int PityPickupCount { get; set; }
        public GachaSubtype SubtypeType { get; set; } = GachaSubtype.None;
        public string SubtypeRef { get; set; }
        public bool IsActive { get; set; } = true;
        public IReadOnlyList<GachaTierEntry> Tiers { get; set; } = new List<GachaTierEntry>();
        public IReadOnlyList<GachaDropEntry> Drops { get; set; } = new List<GachaDropEntry>();
    }
}
