using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 상점 구매 이력·인스턴스 영속화 계약.
    /// 프로젝트가 로컬(PlayerPrefs) / 원격(뒤끝) 중 택해 구현체를 주입.
    /// 동기 API 로 단순화 — 비동기 필요 시 프로젝트 구현체가 내부에서 처리.
    /// </summary>
    public interface IShopRepository
    {
        /// <summary>모든 상품 인스턴스 로드. 첫 실행 시 빈 리스트.</summary>
        IReadOnlyList<IShopProductInstance> LoadAll();

        /// <summary>단건 저장/갱신.</summary>
        void Save(IShopProductInstance instance);

        /// <summary>scope 경계 도달 시 CurrentScopePurchaseCount 0 리셋.</summary>
        void ResetScope(LimitScope scope);
    }
}
