using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IPauseService 기본 구현. Pause 시 현재 Time.timeScale 을 저장하고 0 으로,
    /// Resume 시 저장값으로 복원한다(보통 1, 슬로모션 등 커스텀 timeScale 환경도 보존).
    /// 상태 변화는 PauseChanged(Action) 로 직접 통지하고, EventBus 가 주입됐으면 PauseChangedEvent 도 발행한다.
    ///
    /// 순수 로직 — MonoBehaviour 비의존. Time.timeScale 한 줄만 Unity 에 의존(시간 제어가 본 책임).
    /// </summary>
    public sealed class PauseService : IPauseService
    {
        private const float DEFAULT_TIME_SCALE = 1f;

        private readonly IEventBus _eventBus;
        private float _resumeTimeScale = DEFAULT_TIME_SCALE;

        public bool IsPaused { get; private set; }

        public event Action<bool> PauseChanged;

        /// <param name="eventBus">선택 — 주어지면 PauseChangedEvent 를 추가로 발행한다.</param>
        public PauseService(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public void Pause()
        {
            if (IsPaused) return;

            // 이미 0(다른 이유로 멈춤) 이면 복원 기준을 1 로 — Resume 후 게임이 멈춘 채 남지 않도록.
            _resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : DEFAULT_TIME_SCALE;
            Time.timeScale = 0f;
            IsPaused = true;
            Debug.Log($"[일시정지] 멈춤 (복원 예정 timeScale: {_resumeTimeScale})");
            Notify();
        }

        public void Resume()
        {
            if (!IsPaused) return;

            Time.timeScale = _resumeTimeScale;
            IsPaused = false;
            Debug.Log($"[일시정지] 재개 (timeScale: {_resumeTimeScale})");
            Notify();
        }

        public void Toggle()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        private void Notify()
        {
            PauseChanged?.Invoke(IsPaused);
            _eventBus?.Publish(new PauseChangedEvent { IsPaused = IsPaused });
        }
    }
}
