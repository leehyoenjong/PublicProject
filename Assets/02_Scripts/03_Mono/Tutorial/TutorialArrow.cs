using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 화살표 UI.
    /// ArrowDirection 별 비주얼은 Inspector 에 미리 GameObject 로 매핑하고 활성 전환한다.
    /// 디자이너는 방향마다 다른 아트/애니메이션을 가진 Prefab 인스턴스를 Scene 에 배치해 두고 참조만 연결하면 된다.
    /// </summary>
    public class TutorialArrow : MonoBehaviour
    {
        [Serializable]
        public class DirectionVisual
        {
            public ArrowDirection direction;
            public GameObject visual;
        }

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private DirectionVisual[] _directionVisuals;

        private void Awake()
        {
            DeactivateAll();
        }

        public void Show(ArrowDirection direction)
        {
            if (direction == ArrowDirection.None)
            {
                DeactivateAll();
                SetVisible(false);
                return;
            }

            if (_directionVisuals != null)
            {
                for (int i = 0; i < _directionVisuals.Length; i++)
                {
                    var dv = _directionVisuals[i];
                    if (dv == null || dv.visual == null) continue;
                    dv.visual.SetActive(dv.direction == direction);
                }
            }

            SetVisible(true);
        }

        public void Hide()
        {
            DeactivateAll();
            SetVisible(false);
        }

        private void DeactivateAll()
        {
            if (_directionVisuals == null) return;
            for (int i = 0; i < _directionVisuals.Length; i++)
            {
                var dv = _directionVisuals[i];
                if (dv == null || dv.visual == null) continue;
                dv.visual.SetActive(false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
