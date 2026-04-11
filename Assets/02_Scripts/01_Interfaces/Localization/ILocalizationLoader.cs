using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 다국어 데이터 로더 인터페이스
    /// </summary>
    public interface ILocalizationLoader
    {
        Dictionary<string, string> Load(LanguageCode language);
        bool SupportsLanguage(LanguageCode language);
    }
}
