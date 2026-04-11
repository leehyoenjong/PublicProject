using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 기본 딥링크 핸들러. URL 파싱 후 라우팅.
    /// </summary>
    public class DefaultDeepLinkHandler : IDeepLinkHandler
    {
        private const string SCHEME = "publicframework://";

        public bool CanHandle(string deepLink)
        {
            return !string.IsNullOrEmpty(deepLink) && deepLink.StartsWith(SCHEME);
        }

        public void HandleDeepLink(string deepLink)
        {
            if (!CanHandle(deepLink))
            {
                Debug.LogWarning($"[DeepLink] Cannot handle: {deepLink}");
                return;
            }

            string path = deepLink.Substring(SCHEME.Length);

            Debug.Log($"[DeepLink] Routing: {path}");
        }
    }
}
