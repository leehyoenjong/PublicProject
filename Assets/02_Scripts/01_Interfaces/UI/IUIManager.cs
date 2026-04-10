namespace PublicFramework
{
    /// <summary>
    /// Screen 스택 기반 UI 매니저 인터페이스.
    /// Push/Pop 패턴으로 Screen 전환을 관리한다.
    /// </summary>
    public interface IUIManager : IService
    {
        void Push(string screenId, IScreenTransition transition = null);
        void Pop(IScreenTransition transition = null);
        void Replace(string screenId, IScreenTransition transition = null);
        void ClearAndPush(string screenId, IScreenTransition transition = null);
        BaseScreen GetCurrentScreen();
        int ScreenCount { get; }
        void RegisterScreen(string screenId, BaseScreen prefab);
    }
}
