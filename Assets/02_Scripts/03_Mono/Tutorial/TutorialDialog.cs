using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 대화 UI
    /// </summary>
    public class TutorialDialog : MonoBehaviour
    {
        [SerializeField] private Text _dialogText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _skipButton;

        private ITutorialSystem _tutorialSystem;

        private void Start()
        {
            _tutorialSystem = ServiceLocator.Get<ITutorialSystem>();

            if (_nextButton != null) _nextButton.onClick.AddListener(OnNext);
            if (_skipButton != null) _skipButton.onClick.AddListener(OnSkip);

            SetVisible(false);
        }

        public void Show(string text, DialogPosition position)
        {
            if (_dialogText != null) _dialogText.text = text;

            SetPosition(position);
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetPosition(DialogPosition position)
        {
            if (_rectTransform == null) return;

            Vector2 anchor = position switch
            {
                DialogPosition.Top => new Vector2(0.5f, 0.85f),
                DialogPosition.Center => new Vector2(0.5f, 0.5f),
                DialogPosition.Bottom => new Vector2(0.5f, 0.15f),
                _ => new Vector2(0.5f, 0.5f)
            };

            _rectTransform.anchorMin = anchor;
            _rectTransform.anchorMax = anchor;
            _rectTransform.anchoredPosition = Vector2.zero;
        }

        private void OnNext()
        {
            _tutorialSystem?.NextStep();
        }

        private void OnSkip()
        {
            _tutorialSystem?.SkipTutorial();
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
