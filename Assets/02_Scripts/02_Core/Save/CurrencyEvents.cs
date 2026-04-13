namespace PublicFramework
{
    /// <summary>
    /// 재화 수치 변동 이벤트. 프로젝트가 SaveSystem에 저장 후 EventBus로 발행하여 표시기 갱신.
    /// </summary>
    public struct CurrencyChangedEvent
    {
        public int SlotIndex;
        public string Key;
        public long OldValue;
        public long NewValue;
    }
}
