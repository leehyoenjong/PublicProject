namespace PublicFramework
{
    /// <summary>
    /// 커스텀 버프 동작 인터페이스
    /// </summary>
    public interface IBuffEffect
    {
        void OnApply(string targetId);
        void OnTick(string targetId, float deltaTime);
        void OnRemove(string targetId);
        void OnStack(string targetId, int newStack);
    }
}
