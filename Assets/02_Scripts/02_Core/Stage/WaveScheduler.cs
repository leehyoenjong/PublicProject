using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// Wave 진행 헬퍼. StageSystem 이 Tick 마다 호출.
    /// transitionCondition 평가 + 다음 wave 진행 판단.
    /// 실제 몬스터 스폰/HP 추적은 외부 IBattleHost 가 보고.
    /// </summary>
    public class WaveScheduler
    {
        private readonly IEventBus _eventBus;

        public WaveScheduler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>현재 wave 시작 통지.</summary>
        public void StartWave(StageInstance instance)
        {
            int idx = instance.CurrentWaveIndex;
            _eventBus?.Publish(new WaveStartedEvent
            {
                StageId = instance.StageId,
                WaveIndex = idx
            });
            Debug.Log($"[WaveScheduler] Wave started: {instance.StageId} #{idx}");
        }

        /// <summary>외부 보고로 transition 조건 충족 시 호출. 다음 wave 가 있으면 시작, 아니면 false.</summary>
        public bool AdvanceWave(StageInstance instance)
        {
            int prevIdx = instance.CurrentWaveIndex;

            _eventBus?.Publish(new WaveClearedEvent
            {
                StageId = instance.StageId,
                WaveIndex = prevIdx
            });

            int nextIdx = prevIdx + 1;
            if (instance.Data.Waves == null || nextIdx >= instance.Data.Waves.Count)
            {
                Debug.Log($"[WaveScheduler] All waves cleared: {instance.StageId}");
                return false;
            }

            instance.SetCurrentWaveIndex(nextIdx);
            StartWave(instance);
            return true;
        }

        /// <summary>현재 wave 데이터.</summary>
        public WaveData GetCurrentWave(StageInstance instance)
        {
            if (instance.Data.Waves == null) return null;
            int idx = instance.CurrentWaveIndex;
            if (idx < 0 || idx >= instance.Data.Waves.Count) return null;
            return instance.Data.Waves[idx];
        }
    }
}
