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
            Debug.Log($"[BaseOverlay] '{gameObject.name}' initialized.");
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1f;
            Debug.Log($"[BaseOverlay] '{gameObject.name}' shown.");
        }

        public virtual void Hide()
        {
            CanvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            Debug.Log($"[BaseOverlay] '{gameObject.name}' hidden.");
        }
    }
}
