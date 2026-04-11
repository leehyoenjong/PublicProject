using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ITutorialSystem 구현체.
    /// 스텝 순차 실행, 조건부 트리거, SaveSystem 완료 기록.
    /// </summary>
    public class TutorialSystem : ITutorialSystem
    {
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly Dictionary<string, TutorialData> _tutorials = new Dictionary<string, TutorialData>();
        private readonly HashSet<string> _completedTutorials = new HashSet<string>();

        private ITutorialPresentation _presentation;
        private TutorialData _currentTutorial;
        private int _currentStepIndex;

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY_COMPLETED = "tutorial_completed";

        public bool IsRunning => _currentTutorial != null;
        public string CurrentTutorialId => _currentTutorial?.TutorialId;
        public int CurrentStepIndex => _currentStepIndex;

        public TutorialSystem(IEventBus eventBus, ISaveSystem saveSystem)
        {
            _eventBus = eventBus;
            _saveSystem = saveSystem;

            LoadCompletedData();
            Debug.Log("[TutorialSystem] Init started");
        }

        public void SetPresentation(ITutorialPresentation presentation)
        {
            _presentation = presentation;
        }

        public void RegisterTutorial(TutorialData data)
        {
            if (data == null) return;
            _tutorials[data.TutorialId] = data;
        }

        public void StartTutorial(string tutorialId)
        {
            if (IsRunning)
            {
                Debug.LogWarning($"[TutorialSystem] Already running: {_currentTutorial.TutorialId}");
                return;
            }

            if (!_tutorials.TryGetValue(tutorialId, out TutorialData data))
            {
                Debug.LogError($"[TutorialSystem] Tutorial not found: {tutorialId}");
                return;
            }

            if (IsTutorialCompleted(tutorialId))
            {
                Debug.Log($"[TutorialSystem] Already completed: {tutorialId}");
                return;
            }

            _currentTutorial = data;
            _currentStepIndex = 0;

            _eventBus?.Publish(new TutorialStartedEvent
            {
                TutorialId = tutorialId,
                TotalSteps = data.Steps.Count
            });

            Debug.Log($"[TutorialSystem] Tutorial started: {tutorialId} ({data.Steps.Count} steps)");
            ExecuteCurrentStep();
        }

        public void NextStep()
        {
            if (!IsRunning) return;

            _presentation?.HideStep();
            _presentation?.HideHighlight();
            _presentation?.HideDialog();
            _presentation?.HideArrow();

            _currentStepIndex++;

            if (_currentStepIndex >= _currentTutorial.Steps.Count)
            {
                CompleteTutorial(_currentTutorial.TutorialId);
                return;
            }

            ExecuteCurrentStep();
        }

        public void SkipTutorial()
        {
            if (!IsRunning) return;

            if (!_currentTutorial.CanSkip)
            {
                Debug.LogWarning($"[TutorialSystem] Cannot skip: {_currentTutorial.TutorialId}");
                return;
            }

            _eventBus?.Publish(new TutorialSkippedEvent
            {
                TutorialId = _currentTutorial.TutorialId,
                SkippedAtStep = _currentStepIndex
            });

            Debug.Log($"[TutorialSystem] Tutorial skipped: {_currentTutorial.TutorialId} at step {_currentStepIndex}");

            CompleteTutorial(_currentTutorial.TutorialId);
        }

        public void CompleteTutorial(string tutorialId)
        {
            _completedTutorials.Add(tutorialId);

            _presentation?.HideStep();
            _presentation?.HideHighlight();
            _presentation?.HideDialog();
            _presentation?.HideArrow();

            if (_currentTutorial != null && _currentTutorial.TutorialId == tutorialId)
            {
                _currentTutorial = null;
                _currentStepIndex = 0;
            }

            SaveCompletedData();

            _eventBus?.Publish(new TutorialCompletedEvent { TutorialId = tutorialId });
            Debug.Log($"[TutorialSystem] Tutorial completed: {tutorialId}");
        }

        public bool IsTutorialCompleted(string tutorialId)
        {
            return _completedTutorials.Contains(tutorialId);
        }

        public void CheckTriggers()
        {
            CheckTriggers(TriggerType.Manual, null);
        }

        public void CheckTriggers(TriggerType triggerType, string triggerValue)
        {
            // Priority 내림차순 정렬
            var candidates = new List<TutorialData>();
            foreach (TutorialData data in _tutorials.Values)
            {
                if (IsTutorialCompleted(data.TutorialId)) continue;
                if (!ArePrerequisitesMet(data)) continue;
                if (data.TriggerType != triggerType) continue;
                if (!string.IsNullOrEmpty(data.TriggerValue) && data.TriggerValue != triggerValue) continue;

                candidates.Add(data);
            }

            candidates.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (TutorialData data in candidates)
            {
                _eventBus?.Publish(new TutorialTriggeredEvent
                {
                    TutorialId = data.TutorialId,
                    TriggerType = data.TriggerType
                });

                StartTutorial(data.TutorialId);
                return;
            }
        }

        public IReadOnlyList<string> GetCompletedTutorials()
        {
            return new List<string>(_completedTutorials).AsReadOnly();
        }

        private void ExecuteCurrentStep()
        {
            if (_currentTutorial == null) return;
            if (_currentStepIndex >= _currentTutorial.Steps.Count) return;

            TutorialStepData step = _currentTutorial.Steps[_currentStepIndex];

            _eventBus?.Publish(new TutorialStepChangedEvent
            {
                TutorialId = _currentTutorial.TutorialId,
                StepIndex = _currentStepIndex,
                StepType = step.StepType
            });

            _presentation?.ShowStep(step, _currentStepIndex, _currentTutorial.Steps.Count);

            if (step.StepType == TutorialStepType.Highlight || step.StepType == TutorialStepType.Action)
            {
                _presentation?.ShowHighlight(step);
            }

            if (!string.IsNullOrEmpty(step.DialogText))
            {
                _presentation?.ShowDialog(step.DialogText, step.DialogPosition);
            }

            if (step.ArrowDirection != ArrowDirection.None)
            {
                _presentation?.ShowArrow(step.ArrowDirection);
            }

            Debug.Log($"[TutorialSystem] Step {_currentStepIndex}: {step.StepType}");
        }

        private bool ArePrerequisitesMet(TutorialData data)
        {
            if (data.PrerequisiteTutorialIds == null) return true;

            foreach (string preId in data.PrerequisiteTutorialIds)
            {
                if (string.IsNullOrEmpty(preId)) continue;
                if (!IsTutorialCompleted(preId)) return false;
            }

            return true;
        }

        private void SaveCompletedData()
        {
            if (_saveSystem == null) return;

            var list = new List<string>(_completedTutorials);
            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_COMPLETED, list);
        }

        private void LoadCompletedData()
        {
            if (_saveSystem == null) return;
            if (!_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_COMPLETED)) return;

            var list = _saveSystem.Load<List<string>>(SAVE_SLOT, SAVE_KEY_COMPLETED);
            if (list != null)
            {
                foreach (string id in list)
                {
                    _completedTutorials.Add(id);
                }
            }

            Debug.Log($"[TutorialSystem] Loaded {_completedTutorials.Count} completed tutorials");
        }
    }
}
