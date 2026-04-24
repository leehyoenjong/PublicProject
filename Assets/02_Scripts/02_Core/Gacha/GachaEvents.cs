using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>뽑기 요청 발생 — Pull() 진입 직후.</summary>
    public struct GachaPullRequestedEvent
    {
        public string GachaMID;
        public int Count;
        public PaymentType PaymentType;
    }

    /// <summary>뽑기 성공 — 재화 차감 + 추첨 + 보상 지급 완료 후. UI 연출 단일 페이로드.</summary>
    public struct GachaPullCompletedEvent
    {
        public string GachaMID;
        public string BannerMID;
        public int Count;
        public IReadOnlyList<GachaRewardItem> Rewards;
        public GachaPullSummary Summary;
    }

    /// <summary>뽑기 실패 — 자격 부족/재화 부족/추첨 오류 등.</summary>
    public struct GachaPullFailedEvent
    {
        public string GachaMID;
        public int Count;
        public string Reason;
    }

    /// <summary>천장 발동 — Hard/Pickup 중 하나 트리거됐을 때.</summary>
    public struct GachaPityTriggeredEvent
    {
        public string GachaMID;
        public bool HardPity;
        public bool PickupPity;
        public int PullCountAtTrigger;
    }
}
