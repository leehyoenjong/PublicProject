using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ShopData 에 ChildTable 로 주입되는 보상 요소.
    /// 시트 ShopReward 의 parentId/order 는 매칭·정렬용 예약어라 필드에는 매핑되지 않는다.
    /// </summary>
    [System.Serializable]
    public class ShopReward
    {
        [SerializeField, SheetAlias("rewardItemMID")] private int _rewardItemMID;
        [SerializeField, SheetAlias("rewardAmount")] private int _rewardAmount;

        public int RewardItemMID => _rewardItemMID;
        public int RewardAmount => _rewardAmount;
    }
}
