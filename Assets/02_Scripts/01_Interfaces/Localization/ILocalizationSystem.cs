using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 다국어 시스템 서비스 인터페이스
    /// </summary>
    public interface ILocalizationSystem : IService
    {
        LanguageCode CurrentLanguage { get; }
        void SetLanguage(LanguageCode language);
        string GetText(int key, params object[] args);
        bool HasKey(int key);
        IReadOnlyList<LanguageCode> GetSupportedLanguages();
    }
}
