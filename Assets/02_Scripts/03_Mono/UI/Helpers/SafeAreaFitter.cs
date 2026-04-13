using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// Screen.safeArea에 맞춰 RectTransform 앵커를 조정. 노치/다이나믹 아일랜드 대응.
    /// Canvas 루트에 Attach. 화면 회전/해상도 변경 시 자동 갱신.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool _ignoreLeft;
        [SerializeField] private bool _ignoreRight;
        [SerializeField] private bool _ignoreTop;
        [SerializeField] private bool _ignoreBottom;

        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (HasChanged())
            {
                Apply();
            }
        }

        private bool HasChanged()
        {
            return Screen.safeArea != _lastSafeArea
                || Screen.width != _lastScreenSize.x
                || Screen.height != _lastScreenSize.y;
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;

            if (_ignoreLeft) min.x = 0;
            if (_ignoreBottom) min.y = 0;
            if (_ignoreRight) max.x = Screen.width;
            if (_ignoreTop) max.y = Screen.height;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rect.anchorMin = min;
            _rect.anchorMax = max;

            _lastSafeArea = safe;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
