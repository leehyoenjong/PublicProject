namespace PublicFramework
{
    /// <summary>
    /// 딥링크 라우팅 인터페이스.
    /// </summary>
    public interface IDeepLinkHandler
    {
        void HandleDeepLink(string deepLink);
        bool CanHandle(string deepLink);
    }
}
