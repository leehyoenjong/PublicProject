using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// LayoutGroup 즉시 재계산 헬퍼. OnEnable, 자식 추가/제거 후 수동 호출 가능.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AutoLayoutRefresher : MonoBehaviour
    {
        [SerializeField] private bool _refreshOnEnable = true;

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (_refreshOnEnable)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (_rect == null) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        }
    }
}
