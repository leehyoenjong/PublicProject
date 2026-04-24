using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IBanner POCO. 모든 필드를 setter 로 조합.</summary>
    public class FakeBanner : IBanner
    {
        public string MID { get; set; } = "banner_test";
        public int DisplayNameKey { get; set; }
        public int DescriptionKey { get; set; }
        public Sprite KeyVisual { get; set; }
        public BannerCategory Category { get; set; } = BannerCategory.Regular;
        public string PeriodStartUtc { get; set; }
        public string PeriodEndUtc { get; set; }
        public int DisplayOrder { get; set; }
        public BannerUnlockType UnlockType { get; set; } = BannerUnlockType.None;
        public string UnlockValue { get; set; }
        public bool CarryOverCounter { get; set; }
        public bool IsActive { get; set; } = true;
        public IReadOnlyList<BannerGachaEntry> Gachas { get; set; } = new List<BannerGachaEntry>();
    }
}
