using System;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 오버레이 — 화면 어둡게 + 하이라이트 영역.
    /// HighlightShape 별 비주얼은 Inspector 에 미리 GameObject 로 매핑하고 활성 전환한다.
    /// 디자이너는 Shape 마다 다른 Sprite/Material/Prefab 인스턴스를 Scene 에 배치해 두고 참조만 연결하면 된다.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        [Serializable]
        public class ShapeVisual
        {
            public HighlightShape shape;
            public RectTransform visual;
        }

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _overlayImage;
        [SerializeField] private Button _tapArea;
        [SerializeField] private ShapeVisual[] _shapeVisuals;

        private ITutorialSystem _tutorialSystem;

        private void Start()
        {
            _tutorialSystem = ServiceLocator.Get<ITutorialSystem>();

            if (_tapArea != null)
            {
                _tapArea.onClick.AddListener(OnTap);
            }

            DeactivateAllShapes();
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
            if (_shapeVisuals == null) return;

            for (int i = 0; i < _shapeVisuals.Length; i++)
            {
                var sv = _shapeVisuals[i];
                if (sv == null || sv.visual == null) continue;

                bool active = sv.shape == shape && shape != HighlightShape.None && target != null;
                sv.visual.gameObject.SetActive(active);

                if (active)
                {
                    sv.visual.position = target.position;
                    sv.visual.sizeDelta = target.sizeDelta;
                }
            }
        }

        public void ClearHighlight()
        {
            DeactivateAllShapes();
        }

        private void DeactivateAllShapes()
        {
            if (_shapeVisuals == null) return;
            for (int i = 0; i < _shapeVisuals.Length; i++)
            {
                var sv = _shapeVisuals[i];
                if (sv == null || sv.visual == null) continue;
                sv.visual.gameObject.SetActive(false);
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
