using System;

namespace PublicFramework
{
    /// <summary>
    /// 게임 일시정지 seam. Time.timeScale 을 0↔복원으로 토글하고 상태 변화를 통지한다.
    /// 전투 중 메뉴/설정 등 "게임 시간을 멈춰야 하는" 모든 곳의 단일 창구.
    /// ServiceLocator 에 등록되어 씬 전환을 가로질러 생존한다(INIT DontDestroyOnLoad).
    ///
    /// 트리거 정책은 파생 게임의 선택사항 — 이 서비스는 시간 제어만 책임진다:
    ///  - 화면 일시정지 버튼(PauseButton 샘플)이 Pause/Toggle 호출
    ///  - ESC/안드로이드 Back 키(InputActionReference 바인딩은 파생 몫 — 입력 정책이 게임마다 다름)
    ///  - 앱 포커스 상실 자동 일시정지(OnApplicationPause 훅 — 파생 몫)
    /// </summary>
    public interface IPauseService : IService
    {
        /// <summary>현재 일시정지 상태.</summary>
        bool IsPaused { get; }

        /// <summary>일시정지. 이미 멈춰 있으면 무시. 직전 timeScale 을 기억해 Resume 시 복원한다.</summary>
        void Pause();

        /// <summary>재개. 멈춰 있지 않으면 무시. 기억한 timeScale 로 복원한다.</summary>
        void Resume();

        /// <summary>현재 상태를 뒤집는다(멈춤↔재개).</summary>
        void Toggle();

        /// <summary>일시정지 상태가 바뀔 때 발행(true=멈춤, false=재개). UI/입력 게이팅에 직접 구독.</summary>
        event Action<bool> PauseChanged;
    }
}
