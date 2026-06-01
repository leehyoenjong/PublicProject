using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IRewardHandler 의 production 구현체. 보상(rewardId = ItemData MID)을 인벤토리에 적립한다.
    /// 스테이지/퀘스트/업적이 공유하는 보상 지급 경로. IInventorySystem 만 의존하는 순수 로직.
    /// </summary>
    public class InventoryRewardHandler : IRewardHandler
    {
        private readonly IInventorySystem _inventory;

        public InventoryRewardHandler(IInventorySystem inventory)
        {
            _inventory = inventory ?? throw new System.ArgumentNullException(nameof(inventory));
        }

        public void HandleReward(int rewardId, int amount, string source)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[보상] 무시됨: mid={rewardId}, amount={amount} (source={source})");
                return;
            }

            ItemAddResult result = _inventory.AddItem(rewardId, amount, source);
            if (!result.Success)
            {
                Debug.LogWarning($"[보상] 적립 실패: mid={rewardId}, amount={amount} (source={source})");
                return;
            }

            Debug.Log($"[보상] 적립: mid={rewardId} x{amount} (source={source})");
        }
    }
}
