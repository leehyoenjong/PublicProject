using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 전투 중 일시정지 메뉴. IPauseService.PauseChanged 를 구독해 멈춤이면 표시, 재개면 숨긴다.
    /// "재개" 는 IPauseService.Resume(), "나가기" 는 Resume 후 로비로 전환한다
    /// (timeScale 0 인 채 씬을 떠나면 다음 씬이 멈추므로 먼저 복원).
    ///
    /// Show/Hide 를 CanvasGroup 만 토글하도록 재정의한다 — GameObject 를 비활성화하면 PauseChanged
    /// 구독이 끊겨 다시 켜지지 않으므로, 항상 활성 상태로 두고 alpha/raycast 로만 가시성을 제어한다.
    ///
    /// 제거 가능한 기본 샘플 — 파생 게임은 이 오버레이를 자체 메뉴로 대체하거나,
    /// PauseChanged 만 구독해 다른 UI 를 띄워도 된다. 시간 제어는 IPauseService 가 전담.
    ///
    /// 의도적 빈칸(파생 확장): 설정 버튼, BGM 볼륨 다운, 일시정지 딤 배경/애니메이션.
    /// </summary>
    public class PauseOverlay : BaseOverlay
    {
        [Header("버튼")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _exitButton;
        [SerializeField, SceneName] private string _lobbyScene = "01_Lobby";

        private IPauseService _pauseService;

        protected override void Awake()
        {
            base.Awake();

            if (ServiceLocator.Has<IPauseService>())
            {
                _pauseService = ServiceLocator.Get<IPauseService>();
                _pauseService.PauseChanged += OnPauseChanged;
            }
            else
            {
                Debug.LogWarning("[일시정지] IPauseService 미등록 — PauseOverlay 비활성");
            }

            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResume);
            if (_exitButton != null) _exitButton.onClick.AddListener(OnExit);

            // 초기 가시성을 현재 일시정지 상태와 동기화 — 이미 멈춘 채(IsPaused=true) 새 씬에 진입해도
            // overlay 가 어긋나지 않도록. Show/Hide 가 alpha·blocksRaycasts·interactable 을 일괄 처리한다.
            if (_pauseService != null && _pauseService.IsPaused) Show();
            else Hide();
        }

        private void OnDestroy()
        {
            if (_pauseService != null) _pauseService.PauseChanged -= OnPauseChanged;
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(OnResume);
            if (_exitButton != null) _exitButton.onClick.RemoveListener(OnExit);
        }

        public override void Show()
        {
            CanvasGroup.alpha = 1f;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
            Debug.Log("[일시정지] 메뉴 표시");
        }

        public override void Hide()
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
            // SetActive(false) 하지 않음 — PauseChanged 구독 유지를 위해.
            Debug.Log("[일시정지] 메뉴 숨김");
        }

        private void OnPauseChanged(bool paused)
        {
            if (paused) Show();
            else Hide();
        }

        private void OnResume()
        {
            _pauseService?.Resume();
        }

        private void OnExit()
        {
            _pauseService?.Resume(); // timeScale 복원 후 전환 — 안 하면 다음 씬이 멈춘 채 시작.
            if (!string.IsNullOrEmpty(_lobbyScene)) SceneFlowRouter.Load(_lobbyScene);
        }
    }
}
