namespace PublicFramework
{
    public struct LanguageChangedEvent
    {
        public LanguageCode OldLanguage;
        public LanguageCode NewLanguage;
    }

    public struct LocalizationLoadedEvent
    {
        public LanguageCode Language;
        public int KeyCount;
    }

    public struct LocalizationKeyMissingEvent
    {
        public string Key;
        public LanguageCode Language;
    }
}
