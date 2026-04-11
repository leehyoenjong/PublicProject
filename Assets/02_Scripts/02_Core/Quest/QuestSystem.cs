using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IQuestSystem 구현체. SaveSystem 연동, 리셋, 해금 체크.
    /// </summary>
    public class QuestSystem : IQuestSystem
    {
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly Dictionary<string, QuestInstance> _quests = new Dictionary<string, QuestInstance>();
        private IRewardHandler _rewardHandler;

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY_QUEST_STATES = "quest_states";
        private const string SAVE_KEY_QUEST_PROGRESS = "quest_progress";

        public QuestSystem(IEventBus eventBus, ISaveSystem saveSystem)
        {
            _eventBus = eventBus;
            _saveSystem = saveSystem;
            Debug.Log("[QuestSystem] Init started");
        }

        /// <summary>
        /// 퀘스트 등록 후 호출하여 저장된 상태/진행도를 복원한다.
        /// </summary>
        public void Initialize()
        {
            LoadQuestStates();
            Debug.Log("[QuestSystem] Initialized — quest states loaded");
        }

        public void SetRewardHandler(IRewardHandler handler)
        {
            _rewardHandler = handler;
        }

        public void RegisterQuest(QuestData questData)
        {
            if (questData == null || _quests.ContainsKey(questData.QuestId)) return;

            var instance = new QuestInstance(questData);
            _quests[questData.QuestId] = instance;

            _eventBus?.Publish(new QuestRegisteredEvent { QuestId = questData.QuestId });

            if (questData.AutoAccept)
            {
                instance.SetState(QuestState.InProgress);
            }
        }

        public bool AcceptQuest(string questId)
        {
            if (!_quests.TryGetValue(questId, out QuestInstance quest)) return false;
            if (quest.State != QuestState.Available) return false;

            quest.SetState(QuestState.InProgress);
            SaveQuestStates();

            _eventBus?.Publish(new QuestAcceptedEvent
            {
                QuestId = questId,
                QuestType = quest.QuestType
            });

            Debug.Log($"[QuestSystem] Quest accepted: {questId}");
            return true;
        }

        public bool AbandonQuest(string questId)
        {
            if (!_quests.TryGetValue(questId, out QuestInstance quest)) return false;
            if (quest.State != QuestState.InProgress) return false;

            quest.ResetConditions();
            quest.SetState(QuestState.Available);
            SaveQuestStates();

            _eventBus?.Publish(new QuestAbandonedEvent { QuestId = questId });

            Debug.Log($"[QuestSystem] Quest abandoned: {questId}");
            return true;
        }

        public bool ClaimReward(string questId)
        {
            if (!_quests.TryGetValue(questId, out QuestInstance quest)) return false;
            if (quest.State != QuestState.Completed) return false;

            quest.SetState(QuestState.Rewarded);

            foreach (QuestReward reward in quest.GetRewards())
            {
                _rewardHandler?.HandleReward(reward.RewardId, reward.RewardType, reward.Amount, "Quest");

                _eventBus?.Publish(new QuestRewardClaimedEvent
                {
                    QuestId = questId,
                    RewardId = reward.RewardId,
                    RewardType = reward.RewardType,
                    Amount = reward.Amount
                });
            }

            SaveQuestStates();

            Debug.Log($"[QuestSystem] Quest reward claimed: {questId}");
            return true;
        }

        public IReadOnlyList<IQuestInstance> GetQuests(QuestState? stateFilter = null, QuestType? typeFilter = null)
        {
            var result = new List<IQuestInstance>();

            foreach (QuestInstance quest in _quests.Values)
            {
                if (stateFilter.HasValue && quest.State != stateFilter.Value) continue;
                if (typeFilter.HasValue && quest.QuestType != typeFilter.Value) continue;
                result.Add(quest);
            }

            return result.AsReadOnly();
        }

        public IQuestInstance GetProgress(string questId)
        {
            _quests.TryGetValue(questId, out QuestInstance quest);
            return quest;
        }

        public void CheckUnlocks()
        {
            foreach (QuestInstance quest in _quests.Values)
            {
                if (quest.State != QuestState.Locked) continue;

                if (ArePrerequisitesMet(quest))
                {
                    quest.SetState(QuestState.Available);

                    _eventBus?.Publish(new QuestUnlockedEvent
                    {
                        QuestId = quest.QuestId,
                        QuestType = quest.QuestType
                    });

                    if (quest.Data.AutoAccept)
                    {
                        AcceptQuest(quest.QuestId);
                    }
                }
            }

            SaveQuestStates();
        }

        public void NotifyConditionProgress(ConditionType type, string targetId, int amount)
        {
            foreach (QuestInstance quest in _quests.Values)
            {
                if (quest.State != QuestState.InProgress) continue;

                bool wasCompleted = quest.IsCompleted;

                // Sequence 모드: 활성 조건만 대상
                if (quest.ConditionGroup.GroupType == ConditionGroupType.Sequence)
                {
                    ICondition active = quest.ConditionGroup.GetActiveCondition();
                    if (active != null && active.ConditionType == type &&
                        (string.IsNullOrEmpty(active.TargetId) || active.TargetId == targetId))
                    {
                        active.AddProgress(amount);
                    }
                }
                else
                {
                    foreach (ICondition condition in quest.ConditionGroup.Conditions)
                    {
                        if (condition.ConditionType == type && (string.IsNullOrEmpty(condition.TargetId) || condition.TargetId == targetId))
                        {
                            condition.AddProgress(amount);
                        }
                    }
                }

                // 조건별 진행 이벤트 발행
                foreach (ICondition condition in quest.ConditionGroup.Conditions)
                {
                    if (condition.ConditionType == type && (string.IsNullOrEmpty(condition.TargetId) || condition.TargetId == targetId))
                    {
                        _eventBus?.Publish(new QuestProgressEvent
                        {
                            QuestId = quest.QuestId,
                            ConditionId = condition.ConditionId,
                            Current = condition.CurrentAmount,
                            Required = condition.RequiredAmount
                        });
                    }
                }

                if (!wasCompleted && quest.IsCompleted)
                {
                    quest.SetState(QuestState.Completed);

                    _eventBus?.Publish(new QuestCompletedEvent
                    {
                        QuestId = quest.QuestId,
                        QuestType = quest.QuestType
                    });

                    Debug.Log($"[QuestSystem] Quest completed: {quest.QuestId}");
                }
            }

            SaveQuestStates();
        }

        public void ResetDaily()
        {
            ResetByType(QuestType.Daily);
            Debug.Log("[QuestSystem] Daily quests reset");
        }

        public void ResetWeekly()
        {
            ResetByType(QuestType.Weekly);
            Debug.Log("[QuestSystem] Weekly quests reset");
        }

        private void ResetByType(QuestType type)
        {
            foreach (QuestInstance quest in _quests.Values)
            {
                if (quest.QuestType != type) continue;

                quest.ResetConditions();
                quest.SetState(quest.Data.AutoAccept ? QuestState.InProgress : QuestState.Available);
            }

            SaveQuestStates();

            _eventBus?.Publish(new QuestResetEvent { QuestType = type });
        }

        private bool ArePrerequisitesMet(QuestInstance quest)
        {
            if (quest.Data.PrerequisiteQuestIds == null) return true;

            foreach (string preId in quest.Data.PrerequisiteQuestIds)
            {
                if (string.IsNullOrEmpty(preId)) continue;

                if (!_quests.TryGetValue(preId, out QuestInstance preQuest)) return false;
                if (preQuest.State != QuestState.Rewarded) return false;
            }

            return true;
        }

        private void SaveQuestStates()
        {
            if (_saveSystem == null) return;

            var states = new Dictionary<string, int>();
            var progress = new Dictionary<string, int[]>();

            foreach (var kvp in _quests)
            {
                states[kvp.Key] = (int)kvp.Value.State;

                var conditions = kvp.Value.ConditionGroup.Conditions;
                var amounts = new int[conditions.Count];
                for (int i = 0; i < conditions.Count; i++)
                {
                    amounts[i] = conditions[i].CurrentAmount;
                }
                progress[kvp.Key] = amounts;
            }

            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_QUEST_STATES, states);
            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_QUEST_PROGRESS, progress);
        }

        private void LoadQuestStates()
        {
            if (_saveSystem == null) return;
            if (!_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_QUEST_STATES)) return;

            var states = _saveSystem.Load<Dictionary<string, int>>(SAVE_SLOT, SAVE_KEY_QUEST_STATES);
            if (states == null) return;

            foreach (var kvp in states)
            {
                if (_quests.TryGetValue(kvp.Key, out QuestInstance quest))
                {
                    quest.SetState((QuestState)kvp.Value);
                }
            }

            // 조건 진행도 복원
            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_QUEST_PROGRESS))
            {
                var progress = _saveSystem.Load<Dictionary<string, int[]>>(SAVE_SLOT, SAVE_KEY_QUEST_PROGRESS);
                if (progress != null)
                {
                    foreach (var kvp in progress)
                    {
                        if (!_quests.TryGetValue(kvp.Key, out QuestInstance quest)) continue;

                        var conditions = quest.ConditionGroup.Conditions;
                        int[] amounts = kvp.Value;

                        for (int i = 0; i < conditions.Count && i < amounts.Length; i++)
                        {
                            if (conditions[i] is Condition c)
                            {
                                c.SetCurrentAmount(amounts[i]);
                            }
                        }
                    }
                }
            }
        }
    }
}
