using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 오버레이 — 화면 어둡게 + 하이라이트 영역
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _overlayImage;
        [SerializeField] private RectTransform _highlightMask;
        [SerializeField] private Button _tapArea;

        private ITutorialSystem _tutorialSystem;

        private void Start()
        {
            _tutorialSystem = ServiceLocator.Get<ITutorialSystem>();

            if (_tapArea != null)
            {
                _tapArea.onClick.AddListener(OnTap);
            }

            SetVisible(false);
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void SetHighlight(RectTransform target, HighlightShape shape)
        {
            if (_highlightMask == null || target == null) return;

            _highlightMask.position = target.position;
            _highlightMask.sizeDelta = target.sizeDelta;

            // TODO: HighlightShape에 따라 마스크 형태 변경
            // Circle → 원형 마스크, Rectangle → 사각 마스크, None → 마스크 없음
            // 프로젝트별 구현 시 Image.sprite나 Material로 분기

            _highlightMask.gameObject.SetActive(shape != HighlightShape.None);
        }

        public void ClearHighlight()
        {
            if (_highlightMask != null)
            {
                _highlightMask.gameObject.SetActive(false);
            }
        }

        private void OnTap()
        {
            if (_tutorialSystem != null && _tutorialSystem.IsRunning)
            {
                _tutorialSystem.NextStep();
            }
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
