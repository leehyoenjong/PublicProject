using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 버튼 클릭 시 지정한 팝업을 IPopupManager 로 연다. 로비 메뉴 버튼 등에 범용 사용.
    /// PopupManager 는 plain class 라 Button.onClick(UnityEvent)에 직접 연결할 수 없으므로 이 어댑터를 둔다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OpenPopupButton : MonoBehaviour
    {
        [SerializeField] private string _popupId;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(Open);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(Open);
        }

        private void Open()
        {
            if (ServiceLocator.Has<IPopupManager>())
                ServiceLocator.Get<IPopupManager>().Show(_popupId);
            else
                Debug.LogWarning($"[로비] IPopupManager 미등록 — 팝업 '{_popupId}' 열기 실패");
        }
    }
}
