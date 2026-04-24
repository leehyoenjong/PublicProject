using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// EventBus 구독으로 퀘스트 조건 자동 추적
    /// </summary>
    public class QuestTracker
    {
        private readonly IQuestSystem _questSystem;
        private readonly IEventBus _eventBus;

        public QuestTracker(IQuestSystem questSystem, IEventBus eventBus)
        {
            _questSystem = questSystem;
            _eventBus = eventBus;

            SubscribeEvents();
            Debug.Log("[QuestTracker] Init started");
        }

        public void Dispose()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _eventBus.Subscribe<ConditionProgressEvent>(OnConditionProgress);
            _eventBus.Subscribe<EnhanceSuccessEvent>(OnEnhanceSuccess);
            _eventBus.Subscribe<GachaPullCompletedEvent>(OnGachaPull);
            _eventBus.Subscribe<BuffAppliedEvent>(OnBuffApplied);
            _eventBus.Subscribe<MaterialConsumedEvent>(OnMaterialConsumed);
        }

        private void UnsubscribeEvents()
        {
            _eventBus.Unsubscribe<ConditionProgressEvent>(OnConditionProgress);
            _eventBus.Unsubscribe<EnhanceSuccessEvent>(OnEnhanceSuccess);
            _eventBus.Unsubscribe<GachaPullCompletedEvent>(OnGachaPull);
            _eventBus.Unsubscribe<BuffAppliedEvent>(OnBuffApplied);
            _eventBus.Unsubscribe<MaterialConsumedEvent>(OnMaterialConsumed);
        }

        private void OnConditionProgress(ConditionProgressEvent evt)
        {
            _questSystem.NotifyConditionProgress(evt.Type, evt.TargetId, evt.Amount);
        }

        private void OnEnhanceSuccess(EnhanceSuccessEvent evt)
        {
            _questSystem.NotifyConditionProgress(ConditionType.EquipUpgrade, evt.InstanceId, 1);
        }

        private void OnGachaPull(GachaPullCompletedEvent evt)
        {
            int count = evt.Rewards != null ? evt.Rewards.Count : evt.Count;
            _questSystem.NotifyConditionProgress(ConditionType.GachaPull, evt.GachaMID, count);
        }

        private void OnBuffApplied(BuffAppliedEvent evt)
        {
            _questSystem.NotifyConditionProgress(ConditionType.BuffApply, evt.BuffId, 1);
        }

        private void OnMaterialConsumed(MaterialConsumedEvent evt)
        {
            if (evt.MaterialType == EnhanceMaterialType.Currency)
            {
                _questSystem.NotifyConditionProgress(ConditionType.SpendCurrency, "", evt.Amount);
            }
        }
    }
}
