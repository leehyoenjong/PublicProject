using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 추첨 전략. Tier → Item 2단 추첨 + 천장 규칙 적용.
    /// 기본 구현은 DefaultDropResolver. 프로젝트가 교체 가능(OCP).
    /// </summary>
    public interface IDropResolver
    {
        /// <summary>
        /// count 회차 뽑기 수행. 10연 보너스/천장은 시스템 레이어가 아닌 여기서 해석.
        /// 반환 배열 길이 = 실제 지급될 아이템 수 (bonus11th 적용 시 count+1).
        /// pityCounter 는 호출 후 내부 상태가 변경된 상태로 반환됨.
        /// </summary>
        IReadOnlyList<GachaRollResult> Resolve(IGacha gacha, PityCounterState pityCounter, int count);
    }

    /// <summary>IDropResolver 가 받는 천장 카운터 가변 상태. 내부용 struct-like 클래스.</summary>
    public class PityCounterState
    {
        public int PullsSinceLastSSR;
        public int PullsSinceLastPickup;

        public PityCounterState(int sinceSSR, int sincePickup)
        {
            PullsSinceLastSSR = sinceSSR;
            PullsSinceLastPickup = sincePickup;
        }
    }

    /// <summary>추첨 1회 결과 — tier + itemMID + 천장 발동 여부.</summary>
    public struct GachaRollResult
    {
        public GachaTierRank Tier;
        public int ItemMID;
        public bool TriggeredHardPity;
        public bool TriggeredPickupPity;
    }
}
