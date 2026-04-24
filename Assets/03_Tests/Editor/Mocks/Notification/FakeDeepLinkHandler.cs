using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IDeepLinkHandler. CanHandle/HandleDeepLink 호출 기록.</summary>
    public class FakeDeepLinkHandler : IDeepLinkHandler
    {
        public List<string> CanHandleQueries { get; } = new List<string>();
        public List<string> HandledLinks { get; } = new List<string>();
        public bool DefaultCanHandle { get; set; } = true;

        public bool CanHandle(string deepLink)
        {
            CanHandleQueries.Add(deepLink);
            return DefaultCanHandle;
        }

        public void HandleDeepLink(string deepLink)
        {
            HandledLinks.Add(deepLink);
        }
    }
}
