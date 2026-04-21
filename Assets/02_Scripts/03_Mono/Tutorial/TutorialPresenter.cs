using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ITutorialPresentation 기본 구현체.
    /// Overlay / Arrow / Dialog 3개 컴포넌트를 조합하고 TutorialTarget 레지스트리로 하이라이트 대상을 해석한다.
    /// Start 시 ServiceLocator 의 ITutorialSystem 에 자기 자신을 Presentation 으로 등록.
    /// </summary>
    public class TutorialPresenter : MonoBehaviour, ITutorialPresentation
    {
        [SerializeField] private TutorialOverlay _overlay;
        [SerializeField] private TutorialArrow _arrow;
        [SerializeField] private TutorialDialog _dialog;

        private ITutorialSystem _tutorialSystem;

        private void Start()
        {
            _tutorialSystem = ServiceLocator.Get<ITutorialSystem>();
            _tutorialSystem?.SetPresentation(this);
        }

        public void ShowStep(TutorialStepData step, int stepIndex, int totalSteps)
        {
            _overlay?.Show();
        }

        public void HideStep()
        {
            _overlay?.Hide();
            _arrow?.Hide();
            _dialog?.Hide();
        }

        public void ShowHighlight(TutorialStepData step)
        {
            if (_overlay == null || step == null) return;

            if (TutorialTarget.TryFind(step.HighlightTargetId, out RectTransform target))
            {
                _overlay.SetHighlight(target, step.HighlightShape);
            }
            else
            {
                Debug.LogWarning($"[TutorialPresenter] HighlightTarget '{step.HighlightTargetId}' not found in scene registry");
                _overlay.ClearHighlight();
            }
        }

        public void HideHighlight()
        {
            _overlay?.ClearHighlight();
        }

        public void ShowDialog(int dialogTextKey, DialogPosition position)
        {
            _dialog?.Show(dialogTextKey, position);
        }

        public void HideDialog()
        {
            _dialog?.Hide();
        }

        public void ShowArrow(ArrowDirection direction)
        {
            _arrow?.Show(direction);
        }

        public void HideArrow()
        {
            _arrow?.Hide();
        }
    }
}
