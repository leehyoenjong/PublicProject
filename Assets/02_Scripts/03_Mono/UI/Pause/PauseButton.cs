using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 화면 일시정지 버튼. 클릭 시 IPauseService 를 호출하는 어댑터 — 전투 HUD 등에 부착.
    /// PauseService 는 plain class 라 Button.onClick 에 직접 못 붙으므로 이 어댑터를 둔다(OpenPopupButton 동일 패턴).
    /// _toggle=true 면 Toggle(눌러서 멈춤/재개), false 면 Pause 전용(재개는 PauseOverlay 가 담당).
    /// 주의: _toggle=false 로 쓸 땐 재개 주체(PauseOverlay 등)가 씬에 반드시 있어야 한다 —
    /// 없으면 timeScale 0 에서 빠져나올 경로가 사라져 게임이 멈춘 채 고착된다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PauseButton : MonoBehaviour
    {
        [SerializeField] private bool _toggle = false;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (!ServiceLocator.Has<IPauseService>())
            {
                Debug.LogWarning("[일시정지] IPauseService 미등록 — 일시정지 실패");
                return;
            }

            IPauseService pause = ServiceLocator.Get<IPauseService>();
            if (_toggle) pause.Toggle();
            else pause.Pause();
        }
    }
}
