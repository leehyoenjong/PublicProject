using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IStageSystem 구현체.
    /// 챕터/스테이지 등록 + 입장 검증 + Wave/Event 진행 + 별점/보상 처리.
    /// 실제 몬스터 스폰/HP 추적은 외부(IBattleHost) 가 담당하고, 본 시스템에는 Report* 로 보고.
    /// </summary>
    public class StageSystem : IStageSystem
    {
        private readonly IEventBus _eventBus;
        private readonly StageConfig _config;
        private readonly Dictionary<string, ChapterData> _chapters = new Dictionary<string, ChapterData>();
        private readonly Dictionary<string, StageInstance> _stages = new Dictionary<string, StageInstance>();
        private readonly HashSet<string> _completedChapters = new HashSet<string>();

        private readonly WaveScheduler _waveScheduler;
        private StageEventScheduler _eventScheduler;
        private IRewardHandler _rewardHandler;

        private StageInstance _activeStage;
        private StageContext _activeContext;

        public StageSystem(IEventBus eventBus, StageConfig config)
        {
            _eventBus = eventBus;
            _config = config;
            _waveScheduler = new WaveScheduler(eventBus);
            Debug.Log("[StageSystem] Init started");
        }

        public void SetRewardHandler(IRewardHandler handler)
        {
            _rewardHandler = handler;
            _eventScheduler = new StageEventScheduler(_eventBus, _rewardHandler);
        }

        public void RegisterChapter(ChapterData chapterData)
        {
            if (chapterData == null) return;
            _chapters[chapterData.ChapterId] = chapterData;
        }

        public void RegisterStage(StageData stageData)
        {
            if (stageData == null || _stages.ContainsKey(stageData.StageId)) return;

            var instance = new StageInstance(stageData);
            _stages[stageData.StageId] = instance;
            _eventBus?.Publish(new StageRegisteredEvent { StageId = stageData.StageId });
        }

        public bool CanEnter(string stageId, int playerLevel)
        {
            if (!_stages.TryGetValue(stageId, out StageInstance instance))
            {
                Debug.LogWarning($"[StageSystem] Unknown stage: {stageId}");
                return false;
            }

            if (instance.State == StageState.Locked) return false;
            if (instance.Data.RequiredLevel > 0 && playerLevel < instance.Data.RequiredLevel) return false;

            if (instance.Data.DailyEnterLimit > 0)
            {
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                if (instance.LastEnterDateUtc == today && instance.TodayEnterCount >= instance.Data.DailyEnterLimit)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryEnter(StageContext context, int playerLevel)
        {
            if (!CanEnter(context.StageId, playerLevel)) return false;
            StageInstance instance = _stages[context.StageId];

            instance.RecordEnter(DateTime.UtcNow.ToString("yyyy-MM-dd"));
            instance.SetState(StageState.InProgress);
            instance.ResetRuntime();
            instance.ResetEventCompletion();

            _activeStage = instance;
            _activeContext = context;

            _eventBus?.Publish(new StageEnteredEvent
            {
                StageId = context.StageId,
                IsSweep = context.IsSweep,
                AutoBattle = context.AutoBattle
            });

            _eventScheduler?.OnEnter(instance);

            if (instance.Data.Waves != null && instance.Data.Waves.Count > 0)
            {
                _waveScheduler.StartWave(instance);
                _eventScheduler?.OnWaveStart(instance, 0);
            }

            Debug.Log($"[StageSystem] Stage entered: {context.StageId} (sweep={context.IsSweep})");
            return true;
        }

        public bool TrySweep(string stageId, int playerLevel, int times)
        {
            if (!_stages.TryGetValue(stageId, out StageInstance instance)) return false;
            if (!instance.Data.SweepEnabled)
            {
                Debug.LogWarning($"[StageSystem] Sweep not enabled: {stageId}");
                return false;
            }
            if (instance.ClearCount <= 0)
            {
                Debug.LogWarning($"[StageSystem] Sweep requires first clear: {stageId}");
                return false;
            }
            if (times <= 0) return false;

            int success = 0;
            for (int i = 0; i < times; i++)
            {
                if (!CanEnter(stageId, playerLevel)) break;

                instance.RecordEnter(DateTime.UtcNow.ToString("yyyy-MM-dd"));
                AwardClearRewards(instance, isFirstClear: false, isSweep: true);
                instance.RecordClear(instance.BestStars);
                success++;
            }

            if (success > 0)
            {
                _eventBus?.Publish(new StageSweptEvent { StageId = stageId, SweepCount = success });
                Debug.Log($"[StageSystem] Stage swept: {stageId} x{success}");
            }
            return success > 0;
        }

        public void ReportWaveCleared()
        {
            if (_activeStage == null) return;
            int waveIdx = _activeStage.CurrentWaveIndex;
            _eventScheduler?.OnWaveEnd(_activeStage, waveIdx);

            bool hasMore = _waveScheduler.AdvanceWave(_activeStage);
            if (hasMore)
            {
                _eventScheduler?.OnWaveStart(_activeStage, _activeStage.CurrentWaveIndex);
            }
            else
            {
                _eventScheduler?.OnAllClear(_activeStage);
            }
        }

        public void ReportHpThreshold(float currentHpRatio)
        {
            if (_activeStage == null) return;
            _eventScheduler?.OnHpThreshold(_activeStage, currentHpRatio);
        }

        public void ReportStageWin(int starsAchieved)
        {
            if (_activeStage == null) return;
            StageInstance instance = _activeStage;
            bool isFirstClear = instance.IsFirstClear;

            AwardClearRewards(instance, isFirstClear, isSweep: false);
            instance.RecordClear(starsAchieved);

            _eventBus?.Publish(new StageClearedEvent
            {
                StageId = instance.StageId,
                IsFirstClear = isFirstClear,
                Stars = starsAchieved,
                ElapsedSeconds = instance.ElapsedSeconds
            });

            CheckChapterCompletion(instance.Data.ChapterId);

            Debug.Log($"[StageSystem] Stage cleared: {instance.StageId} stars={starsAchieved} firstClear={isFirstClear}");
            _activeStage = null;
        }

        public void ReportStageFail(StageLoseCondition reason)
        {
            if (_activeStage == null) return;
            StageInstance instance = _activeStage;

            instance.SetState(StageState.Available);
            _eventBus?.Publish(new StageFailedEvent
            {
                StageId = instance.StageId,
                Reason = reason,
                ElapsedSeconds = instance.ElapsedSeconds
            });

            Debug.Log($"[StageSystem] Stage failed: {instance.StageId} reason={reason}");
            _activeStage = null;
        }

        public void Tick(float deltaTime)
        {
            if (_activeStage == null) return;
            float elapsed = _activeStage.ElapsedSeconds + deltaTime;
            _activeStage.SetElapsed(elapsed);

            _eventScheduler?.OnTimer(_activeStage, elapsed);

            float limit = _activeStage.Data.TimeLimitSeconds;
            if (limit > 0f && elapsed >= limit && _activeStage.Data.LoseCondition == StageLoseCondition.Timeout)
            {
                ReportStageFail(StageLoseCondition.Timeout);
            }
        }

        public void TriggerManualEvent(int eventIndex)
        {
            if (_activeStage == null) return;
            _eventScheduler?.TriggerManual(_activeStage, eventIndex);
        }

        public void CompleteEvent(int eventIndex)
        {
            if (_activeStage == null) return;
            _eventScheduler?.Complete(_activeStage, eventIndex);
        }

        public StageInstance GetInstance(string stageId)
        {
            _stages.TryGetValue(stageId, out StageInstance instance);
            return instance;
        }

        public IReadOnlyList<StageInstance> GetStagesByChapter(string chapterId)
        {
            var result = new List<StageInstance>();
            foreach (StageInstance instance in _stages.Values)
            {
                if (instance.Data.ChapterId == chapterId) result.Add(instance);
            }
            result.Sort((a, b) => a.Data.SortOrder.CompareTo(b.Data.SortOrder));
            return result.AsReadOnly();
        }

        public bool IsChapterCompleted(string chapterId)
        {
            return _completedChapters.Contains(chapterId);
        }

        public void CheckUnlocks(int playerLevel)
        {
            foreach (StageInstance instance in _stages.Values)
            {
                if (instance.State != StageState.Locked) continue;
                if (!ArePrerequisitesMet(instance)) continue;
                if (instance.Data.RequiredLevel > 0 && playerLevel < instance.Data.RequiredLevel) continue;

                instance.SetState(StageState.Available);
                _eventBus?.Publish(new StageUnlockedEvent { StageId = instance.StageId });
            }
        }

        private bool ArePrerequisitesMet(StageInstance instance)
        {
            if (instance.Data.PrerequisiteStageIds == null) return true;
            foreach (string preId in instance.Data.PrerequisiteStageIds)
            {
                if (string.IsNullOrEmpty(preId)) continue;
                if (!_stages.TryGetValue(preId, out StageInstance preInstance)) return false;
                if (preInstance.State != StageState.Cleared) return false;
            }
            return true;
        }

        private void AwardClearRewards(StageInstance instance, bool isFirstClear, bool isSweep)
        {
            if (_rewardHandler == null) return;

            if (isSweep)
            {
                AwardList(instance.Data.SweepRewards, "StageSweep");
                return;
            }

            if (isFirstClear)
            {
                AwardList(instance.Data.FirstClearRewards, "StageFirstClear");
            }
            AwardList(instance.Data.RepeatRewards, "StageRepeat");
        }

        private void AwardList(IReadOnlyList<QuestReward> rewards, string source)
        {
            if (rewards == null) return;
            foreach (QuestReward r in rewards)
            {
                _rewardHandler.HandleReward(r.RewardId, r.Amount, source);
            }
        }

        private void CheckChapterCompletion(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId)) return;
            if (_completedChapters.Contains(chapterId)) return;

            IReadOnlyList<StageInstance> stages = GetStagesByChapter(chapterId);
            if (stages.Count == 0) return;

            foreach (StageInstance s in stages)
            {
                if (s.State != StageState.Cleared) return;
            }

            _completedChapters.Add(chapterId);

            if (_chapters.TryGetValue(chapterId, out ChapterData chapter) && chapter.ChapterCompleteRewards != null)
            {
                foreach (QuestReward r in chapter.ChapterCompleteRewards)
                {
                    _rewardHandler?.HandleReward(r.RewardId, r.Amount, "ChapterComplete");
                }
            }

            _eventBus?.Publish(new ChapterCompletedEvent { ChapterId = chapterId });
            Debug.Log($"[StageSystem] Chapter completed: {chapterId}");
        }
    }
}
