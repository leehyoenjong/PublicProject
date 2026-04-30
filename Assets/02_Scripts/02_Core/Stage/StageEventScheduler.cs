using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// StageEventEntry 트리거 평가 헬퍼.
    /// 7종 트리거 별로 외부에서 호출되는 메서드 제공.
    /// </summary>
    public class StageEventScheduler
    {
        private readonly IEventBus _eventBus;
        private readonly IRewardHandler _rewardHandler;

        public StageEventScheduler(IEventBus eventBus, IRewardHandler rewardHandler)
        {
            _eventBus = eventBus;
            _rewardHandler = rewardHandler;
        }

        public void OnEnter(StageInstance instance)
        {
            EvaluateTrigger(instance, StageEventTrigger.OnEnter, null);
        }

        public void OnWaveStart(StageInstance instance, int waveIndex)
        {
            EvaluateTrigger(instance, StageEventTrigger.OnWaveStart, waveIndex.ToString());
        }

        public void OnWaveEnd(StageInstance instance, int waveIndex)
        {
            EvaluateTrigger(instance, StageEventTrigger.OnWaveEnd, waveIndex.ToString());
        }

        public void OnAllClear(StageInstance instance)
        {
            EvaluateTrigger(instance, StageEventTrigger.OnAllClear, null);
        }

        public void OnTimer(StageInstance instance, float elapsedSeconds)
        {
            if (instance.Data.Events == null) return;
            for (int i = 0; i < instance.Data.Events.Count; i++)
            {
                StageEventEntry evt = instance.Data.Events[i];
                if (evt.TriggerType != StageEventTrigger.OnTimer) continue;
                if (instance.IsEventCompleted(i) && !evt.CanRepeat) continue;

                if (float.TryParse(evt.TriggerValue, out float threshold) && elapsedSeconds >= threshold)
                {
                    Trigger(instance, i, evt);
                }
            }
        }

        public void OnHpThreshold(StageInstance instance, float currentHpRatio)
        {
            if (instance.Data.Events == null) return;
            for (int i = 0; i < instance.Data.Events.Count; i++)
            {
                StageEventEntry evt = instance.Data.Events[i];
                if (evt.TriggerType != StageEventTrigger.OnHpThreshold) continue;
                if (instance.IsEventCompleted(i) && !evt.CanRepeat) continue;

                if (float.TryParse(evt.TriggerValue, out float threshold) && currentHpRatio <= threshold)
                {
                    Trigger(instance, i, evt);
                }
            }
        }

        public void TriggerManual(StageInstance instance, int eventIndex)
        {
            if (instance.Data.Events == null) return;
            if (eventIndex < 0 || eventIndex >= instance.Data.Events.Count) return;
            StageEventEntry evt = instance.Data.Events[eventIndex];
            if (evt.TriggerType != StageEventTrigger.Manual) return;
            if (instance.IsEventCompleted(eventIndex) && !evt.CanRepeat) return;

            Trigger(instance, eventIndex, evt);
        }

        public void Complete(StageInstance instance, int eventIndex)
        {
            if (instance.Data.Events == null) return;
            if (eventIndex < 0 || eventIndex >= instance.Data.Events.Count) return;
            StageEventEntry evt = instance.Data.Events[eventIndex];

            instance.MarkEventCompleted(eventIndex);
            AwardEventRewards(evt);

            _eventBus?.Publish(new StageEventCompletedEvent
            {
                StageId = instance.StageId,
                EventIndex = eventIndex,
                EventType = evt.EventType,
                TargetId = evt.TargetId
            });

            Debug.Log($"[스테이지] 이벤트 완료: {instance.StageId} #{eventIndex} ({evt.EventType})");
        }

        private void EvaluateTrigger(StageInstance instance, StageEventTrigger triggerType, string triggerMatchValue)
        {
            if (instance.Data.Events == null) return;
            for (int i = 0; i < instance.Data.Events.Count; i++)
            {
                StageEventEntry evt = instance.Data.Events[i];
                if (evt.TriggerType != triggerType) continue;
                if (instance.IsEventCompleted(i) && !evt.CanRepeat) continue;

                if (!string.IsNullOrEmpty(triggerMatchValue) && !string.IsNullOrEmpty(evt.TriggerValue)
                    && evt.TriggerValue != triggerMatchValue)
                {
                    continue;
                }

                Trigger(instance, i, evt);
            }
        }

        private void Trigger(StageInstance instance, int eventIndex, StageEventEntry evt)
        {
            _eventBus?.Publish(new StageEventTriggeredEvent
            {
                StageId = instance.StageId,
                EventIndex = eventIndex,
                EventType = evt.EventType,
                TargetId = evt.TargetId
            });

            Debug.Log($"[스테이지] 이벤트 발동: {instance.StageId} #{eventIndex} ({evt.EventType})");
        }

        private void AwardEventRewards(StageEventEntry evt)
        {
            if (evt.RewardItems == null) return;
            foreach (QuestReward r in evt.RewardItems)
            {
                _rewardHandler?.HandleReward(r.RewardId, r.Amount, "StageEvent");
            }
        }
    }
}
