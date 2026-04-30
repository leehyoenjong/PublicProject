using UnityEngine;

namespace PublicFramework
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseOverlay : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;

        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                    _canvasGroup = GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            Debug.Log($"[오버레이] '{gameObject.name}' 초기화됨.");
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1f;
            Debug.Log($"[오버레이] '{gameObject.name}' 표시됨.");
        }

        public virtual void Hide()
        {
            CanvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            Debug.Log($"[오버레이] '{gameObject.name}' 숨김.");
        }
    }
}
