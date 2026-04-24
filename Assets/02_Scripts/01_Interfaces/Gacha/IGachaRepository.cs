using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 천장 카운터 + 구매 카운트 영속화 계약.
    /// 프로젝트가 로컬(PlayerPrefs) / 원격(뒤끝) 중 택해 구현체 주입.
    /// </summary>
    public interface IGachaRepository
    {
        /// <summary>전체 가챠 카운터 로드. 첫 실행 시 빈 리스트.</summary>
        IReadOnlyList<IPityCounter> LoadAll();

        /// <summary>단건 저장/갱신.</summary>
        void Save(IPityCounter counter);

        /// <summary>배너 종료 시 카운터 초기화/승계 — carryOverCounter 플래그에 따라.</summary>
        void OnBannerEnded(string bannerMID, bool carryOver);

        /// <summary>가챠별 구매 카운트 로드(daily/period/lifetime 키 별).</summary>
        int GetPurchaseCount(string gachaMID, PurchaseScope scope);

        /// <summary>구매 카운트 저장.</summary>
        void SetPurchaseCount(string gachaMID, PurchaseScope scope, int count);

        /// <summary>scope 경계 도달 시 해당 scope 카운트 전체 0으로.</summary>
        void ResetPurchaseScope(PurchaseScope scope);
    }

    /// <summary>가챠 구매 카운트 scope. Shop 의 LimitScope 와 분리 — 가챠 전용 lifecycle 으로 해석.</summary>
    public enum PurchaseScope
    {
        Daily,
        Period,
        Lifetime
    }
}
