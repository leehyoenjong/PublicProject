using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 프레임워크 이벤트를 튜토리얼 트리거로 자동 브리지.
    /// - QuestCompletedEvent  → TriggerType.QuestComplete
    /// - AchievementCompleted → TriggerType.AchievementUnlocked
    /// 씬(또는 부트스트랩)에 1개만 배치.
    /// </summary>
    public class TutorialEventBridge : MonoBehaviour
    {
        [SerializeField] private bool _bridgeQuest = true;
        [SerializeField] private bool _bridgeAchievement = true;

        private IEventBus _eventBus;
        private ITutorialSystem _tutorial;

        private void Start()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
            _tutorial = ServiceLocator.Get<ITutorialSystem>();

            if (_eventBus == null || _tutorial == null)
            {
                Debug.LogWarning("[TutorialEventBridge] EventBus 또는 TutorialSystem 미등록");
                return;
            }

            if (_bridgeQuest)
            {
                _eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
            }

            if (_bridgeAchievement)
            {
                _eventBus.Subscribe<AchievementCompletedEvent>(OnAchievementCompleted);
            }
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;

            if (_bridgeQuest)
            {
                _eventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
            }

            if (_bridgeAchievement)
            {
                _eventBus.Unsubscribe<AchievementCompletedEvent>(OnAchievementCompleted);
            }
        }

        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            _tutorial?.CheckTriggers(TriggerType.QuestComplete, evt.QuestId);
        }

        private void OnAchievementCompleted(AchievementCompletedEvent evt)
        {
            _tutorial?.CheckTriggers(TriggerType.AchievementUnlocked, evt.AchievementId);
        }
    }
}
