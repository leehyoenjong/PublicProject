using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 화살표 UI
    /// </summary>
    public class TutorialArrow : MonoBehaviour
    {
        [SerializeField] private RectTransform _arrowTransform;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _bounceAmount = 10f;
        [SerializeField] private float _bounceSpeed = 3f;

        private Vector3 _basePosition;
        private ArrowDirection _currentDirection;
        private bool _isVisible;

        public void Show(ArrowDirection direction)
        {
            _currentDirection = direction;
            _isVisible = true;

            if (_arrowTransform != null)
            {
                float rotation = direction switch
                {
                    ArrowDirection.Up => 0f,
                    ArrowDirection.Down => 180f,
                    ArrowDirection.Left => 90f,
                    ArrowDirection.Right => -90f,
                    _ => 0f
                };

                _arrowTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
                _basePosition = _arrowTransform.localPosition;
            }

            SetVisible(true);
        }

        public void Hide()
        {
            _isVisible = false;
            SetVisible(false);
        }

        private void Update()
        {
            if (!_isVisible || _arrowTransform == null) return;

            float offset = Mathf.Sin(Time.time * _bounceSpeed) * _bounceAmount;

            Vector3 bounceDir = _currentDirection switch
            {
                ArrowDirection.Up => Vector3.up,
                ArrowDirection.Down => Vector3.down,
                ArrowDirection.Left => Vector3.left,
                ArrowDirection.Right => Vector3.right,
                _ => Vector3.zero
            };

            _arrowTransform.localPosition = _basePosition + bounceDir * offset;
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
