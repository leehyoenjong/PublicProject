using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 시스템 계약. 배너 노출 / 뽑기 자격 / 뽑기 처리의 단일 진입점.
    /// 재화 차감은 IInventorySystem, 추첨은 IDropResolver, 영속화는 IGachaRepository, 보상 지급도 IInventorySystem.
    /// </summary>
    public interface IGachaSystem : IService
    {
        /// <summary>활성 + 기간 + 해금 충족한 배너 목록.</summary>
        IReadOnlyList<IBanner> GetVisibleBanners(IGachaContext context);

        /// <summary>특정 배너 조회. 없으면 null.</summary>
        IBanner GetBanner(string bannerMID);

        /// <summary>특정 가챠 조회. 없으면 null.</summary>
        IGacha GetGacha(string gachaMID);

        /// <summary>가챠의 현재 천장 카운터 스냅샷.</summary>
        IPityCounter GetPityCounter(string gachaMID);

        /// <summary>count(1 또는 10) 회차 뽑기 가능 여부 + 실패 사유.</summary>
        PullEligibility CanPull(string gachaMID, int count, IGachaContext context);

        /// <summary>비동기 뽑기. 결과(보상 + 천장 + 집계)는 콜백.</summary>
        void Pull(string gachaMID, int count, IGachaContext context, Action<PullResult> callback);
    }

    public struct PullEligibility
    {
        public bool CanPull;
        public string BlockReason;
    }

    public struct PullResult
    {
        public bool Success;
        public string GachaMID;
        public string FailureReason;
        public IReadOnlyList<GachaRewardItem> Rewards;
        public GachaPullSummary Summary;
    }

    /// <summary>뽑기 결과 집계. UI 연출/이벤트 단일 페이로드.</summary>
    public struct GachaPullSummary
    {
        public int PullCount;
        public int SSRCount;
        public int SRCount;
        public int RCount;
        public int NCount;
        public bool HardPityTriggered;
        public bool PickupPityTriggered;
        public bool GuaranteedBonusApplied;
        public bool Bonus11thApplied;
    }
}
