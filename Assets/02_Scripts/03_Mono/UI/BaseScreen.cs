using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 모든 Screen의 기본 클래스.
    /// Screen은 전체 화면을 차지하는 UI 단위 (메인메뉴, 인게임, 설정 등).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseScreen : MonoBehaviour
    {
        [SerializeField] private string _screenId;

        private CanvasGroup _canvasGroup;

        public string ScreenId => _screenId;

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                    _canvasGroup = GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }

        /// <summary>
        /// Screen이 스택에 Push되어 활성화될 때 호출.
        /// </summary>
        public virtual void OnScreenEnter()
        {
            Debug.Log($"[UI] Screen enter: {_screenId}");
        }

        /// <summary>
        /// Screen이 스택에서 Pop되어 제거될 때 호출.
        /// </summary>
        public virtual void OnScreenExit()
        {
            Debug.Log($"[UI] Screen exit: {_screenId}");
        }

        /// <summary>
        /// Screen을 화면에 보여준다. (위에 다른 Screen이 올라갔다가 돌아올 때도 호출)
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Screen을 화면에서 숨긴다. (Destroy가 아닌 비활성화)
        /// </summary>
        public virtual void Hide()
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
