namespace PublicFramework.Tests
{
    /// <summary>테스트용 IBuffEffect. 콜백 호출 횟수만 기록.</summary>
    public class FakeBuffEffect : IBuffEffect
    {
        public int OnApplyCalls { get; private set; }
        public int OnTickCalls { get; private set; }
        public int OnRemoveCalls { get; private set; }
        public int OnStackCalls { get; private set; }
        public int LastStackCount { get; private set; }

        public void OnApply(string targetId) { OnApplyCalls++; }
        public void OnTick(string targetId, float deltaTime) { OnTickCalls++; }
        public void OnRemove(string targetId) { OnRemoveCalls++; }
        public void OnStack(string targetId, int newStack) { OnStackCalls++; LastStackCount = newStack; }
    }
}
