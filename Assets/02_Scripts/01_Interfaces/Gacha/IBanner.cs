using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 배너 계약. BannerData(SO) 가 구현.
    /// Banner 는 컨테이너 — 실제 뽑기 설정은 포함된 GachaData 에 있음.
    /// </summary>
    public interface IBanner
    {
        string MID { get; }
        int DisplayNameKey { get; }
        int DescriptionKey { get; }
        Sprite KeyVisual { get; }

        BannerCategory Category { get; }
        string PeriodStartUtc { get; }
        string PeriodEndUtc { get; }
        int DisplayOrder { get; }

        BannerUnlockType UnlockType { get; }
        string UnlockValue { get; }

        bool CarryOverCounter { get; }
        bool IsActive { get; }

        IReadOnlyList<BannerGachaEntry> Gachas { get; }
    }
}
