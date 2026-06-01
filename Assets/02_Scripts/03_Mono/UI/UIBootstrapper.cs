using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// UI 시스템 부팅 진입점. UI(Screen/Popup)가 사는 씬(로비/전투)에 부착한다.
    /// Canvas 하위의 ScreenRoot/PopupRoot/DimBackground 를 받아 UIManager/PopupManager 를 생성·등록하고,
    /// 등록할 팝업 prefab 들을 PopupManager 에 매핑한다.
    /// GameBootstrapper(DontDestroyOnLoad)의 코어 시스템 위에서 동작하므로 씬마다 가볍게 둘 수 있다.
    /// </summary>
    public class UIBootstrapper : MonoBehaviour
    {
        [System.Serializable]
        public struct PopupEntry
        {
            public string popupId;
            public BasePopup prefab;
        }

        [Header("Canvas 하위 컨테이너")]
        [SerializeField] private Transform _screenRoot;
        [SerializeField] private Transform _popupRoot;
        [SerializeField] private GameObject _dimBackground;

        [Header("등록할 팝업 prefab")]
        [SerializeField] private PopupEntry[] _popups;

        private UIManager _uiManager;
        private PopupManager _popupManager;
        private bool _isOwner;

        private void Awake()
        {
            // 이미 등록돼 있으면(씬 재진입 등) 중복 생성 방지
            if (ServiceLocator.Has<IPopupManager>())
            {
                Debug.LogWarning("[UI부팅] IPopupManager 이미 등록됨 — 이 인스턴스는 등록 생략");
                return;
            }

            _isOwner = true;

            _uiManager = new UIManager(_screenRoot, this);
            ServiceLocator.Register<IUIManager>(_uiManager);

            _popupManager = new PopupManager(_popupRoot, _dimBackground, this);
            ServiceLocator.Register<IPopupManager>(_popupManager);

            if (_popups != null)
            {
                foreach (PopupEntry e in _popups)
                {
                    if (e.prefab != null && !string.IsNullOrEmpty(e.popupId))
                        _popupManager.RegisterPopup(e.popupId, e.prefab);
                }
            }

            Debug.Log($"[UI부팅] UIManager / PopupManager 등록 완료 (팝업 {(_popups != null ? _popups.Length : 0)}종)");
        }

        private void OnDestroy()
        {
            if (!_isOwner) return;
            if (ServiceLocator.Has<IPopupManager>()) ServiceLocator.Unregister<IPopupManager>();
            if (ServiceLocator.Has<IUIManager>()) ServiceLocator.Unregister<IUIManager>();
        }
    }
}
