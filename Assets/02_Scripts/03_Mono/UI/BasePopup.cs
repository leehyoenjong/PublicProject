using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 모든 팝업의 기본 클래스.
    /// 팝업은 Screen 위에 표시되며, 뒤 UI 입력을 차단한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        [SerializeField] private string _popupId;
        [SerializeField] private int _priority;
        [SerializeField] private bool _isModal = true;

        private CanvasGroup _canvasGroup;
        private Action<PopupResult> _onResult;
        private object _lastData;

        public string PopupId => _popupId;
        public int Priority => _priority;
        public bool IsModal => _isModal;
        public object LastData => _lastData;

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                    _canvasGroup = GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }

        public RectTransform RectTransform => (RectTransform)transform;

        /// <summary>
        /// 팝업을 표시한다. data로 초기화 정보를 전달받는다.
        /// </summary>
        public virtual void Show(object data)
        {
            _lastData = data;
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            Debug.Log($"[UI] Popup show: {_popupId}");
        }

        /// <summary>
        /// 팝업을 닫는다.
        /// </summary>
        public virtual void Hide()
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            Debug.Log($"[UI] Popup hide: {_popupId}");
        }

        /// <summary>
        /// 결과 콜백을 등록한다.
        /// </summary>
        public void OnResult(Action<PopupResult> callback)
        {
            _onResult = callback;
        }

        /// <summary>
        /// 결과를 전달하고 팝업을 닫는다. 하위 클래스에서 버튼 등에 연결한다.
        /// </summary>
        protected void SetResult(PopupResult result)
        {
            _onResult?.Invoke(result);
            _onResult = null;
            ServiceLocator.Get<IPopupManager>()?.Hide();
        }
    }
}
