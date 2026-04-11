using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// EventBus 자동 추적 — 업적은 항상 추적
    /// </summary>
    public class AchievementTracker
    {
        private readonly IAchievementSystem _achievementSystem;
        private readonly IEventBus _eventBus;

        public AchievementTracker(IAchievementSystem achievementSystem, IEventBus eventBus)
        {
            _achievementSystem = achievementSystem;
            _eventBus = eventBus;

            SubscribeEvents();
            Debug.Log("[AchievementTracker] Init started");
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _eventBus.Subscribe<ConditionProgressEvent>(OnConditionProgress);
            _eventBus.Subscribe<EnhanceSuccessEvent>(OnEnhanceSuccess);
            _eventBus.Subscribe<GachaPullResultEvent>(OnGachaPull);
            _eventBus.Subscribe<BuffAppliedEvent>(OnBuffApplied);
            _eventBus.Subscribe<PurchaseCompleteEvent>(OnPurchaseComplete);
            _eventBus.Subscribe<MaterialConsumedEvent>(OnMaterialConsumed);
            _eventBus.Subscribe<QuestRewardClaimedEvent>(OnQuestRewarded);
        }

        private void UnsubscribeEvents()
        {
            _eventBus.Unsubscribe<ConditionProgressEvent>(OnConditionProgress);
            _eventBus.Unsubscribe<EnhanceSuccessEvent>(OnEnhanceSuccess);
            _eventBus.Unsubscribe<GachaPullResultEvent>(OnGachaPull);
            _eventBus.Unsubscribe<BuffAppliedEvent>(OnBuffApplied);
            _eventBus.Unsubscribe<PurchaseCompleteEvent>(OnPurchaseComplete);
            _eventBus.Unsubscribe<MaterialConsumedEvent>(OnMaterialConsumed);
            _eventBus.Unsubscribe<QuestRewardClaimedEvent>(OnQuestRewarded);
        }

        private void OnConditionProgress(ConditionProgressEvent evt)
        {
            _achievementSystem.NotifyProgress(evt.Type, evt.TargetId, evt.Amount);
        }

        private void OnEnhanceSuccess(EnhanceSuccessEvent evt)
        {
            _achievementSystem.NotifyProgress(ConditionType.EquipUpgrade, evt.InstanceId, 1);
        }

        private void OnGachaPull(GachaPullResultEvent evt)
        {
            int count = evt.Rewards != null ? evt.Rewards.Length : 0;
            _achievementSystem.NotifyProgress(ConditionType.GachaPull, evt.BannerId, count);
        }

        private void OnBuffApplied(BuffAppliedEvent evt)
        {
            _achievementSystem.NotifyProgress(ConditionType.BuffApply, evt.BuffId, 1);
        }

        private void OnPurchaseComplete(PurchaseCompleteEvent evt)
        {
            _achievementSystem.NotifyProgress(ConditionType.SpendCurrency, evt.ProductId, 1);
        }

        private void OnMaterialConsumed(MaterialConsumedEvent evt)
        {
            if (evt.MaterialType == EnhanceMaterialType.Currency)
            {
                _achievementSystem.NotifyProgress(ConditionType.SpendCurrency, "", evt.Amount);
            }
        }

        private void OnQuestRewarded(QuestRewardClaimedEvent evt)
        {
            _achievementSystem.NotifyProgress(ConditionType.Custom, "quest_complete", 1);
        }
    }
}
