namespace PublicFramework
{
    /// <summary>
    /// 게임 일시정지 상태 변화. true=멈춤, false=재개.
    /// IPauseService.PauseChanged(직접 구독) 외에, EventBus 로도 느슨하게 받고 싶은 시스템용
    /// (예: BGM 볼륨 다운, 입력 비활성화). PauseService 에 EventBus 가 주입됐을 때만 발행된다.
    /// </summary>
    public struct PauseChangedEvent
    {
        public bool IsPaused;
    }
}
