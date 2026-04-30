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
                Debug.LogWarning($"[알림] 딥링크 처리 불가: {deepLink}");
                return;
            }

            string path = deepLink.Substring(SCHEME.Length);

            Debug.Log($"[알림] 딥링크 라우팅: {path}");
        }
    }
}
