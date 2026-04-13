using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// GDPR 동의 상태를 PlayerPrefs 기반으로 저장/조회.
    /// 세부 카테고리(Analytics/Marketing/Functional) + 저장된 동의 버전 관리.
    /// </summary>
    public static class ConsentStore
    {
        private const string PREF_REQUIRED = "consent.required";
        private const string PREF_ANALYTICS = "consent.analytics";
        private const string PREF_MARKETING = "consent.marketing";
        private const string PREF_FUNCTIONAL = "consent.functional";
        private const string PREF_ACCEPTED_VERSION = "consent.acceptedVersion";

        public static bool GetConsent(ConsentCategory category)
        {
            int stored = PlayerPrefs.GetInt(KeyFor(category), DefaultFor(category));
            return stored != 0;
        }

        public static void SetConsent(ConsentCategory category, bool accepted)
        {
            PlayerPrefs.SetInt(KeyFor(category), accepted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static int GetAcceptedVersion()
        {
            return PlayerPrefs.GetInt(PREF_ACCEPTED_VERSION, 0);
        }

        public static void SetAcceptedVersion(int version)
        {
            PlayerPrefs.SetInt(PREF_ACCEPTED_VERSION, version);
            PlayerPrefs.Save();
        }

        public static bool RequiresReshow(int currentVersion)
        {
            return GetAcceptedVersion() != currentVersion;
        }

        private static string KeyFor(ConsentCategory category)
        {
            switch (category)
            {
                case ConsentCategory.Required: return PREF_REQUIRED;
                case ConsentCategory.Analytics: return PREF_ANALYTICS;
                case ConsentCategory.Marketing: return PREF_MARKETING;
                case ConsentCategory.Functional: return PREF_FUNCTIONAL;
                default: return PREF_FUNCTIONAL;
            }
        }

        private static int DefaultFor(ConsentCategory category)
        {
            // Required/Functional 은 기본 허용. Analytics/Marketing 은 기본 거부 (opt-in).
            return (category == ConsentCategory.Required || category == ConsentCategory.Functional) ? 1 : 0;
        }
    }
}
