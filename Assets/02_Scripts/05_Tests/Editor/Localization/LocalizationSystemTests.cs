using NUnit.Framework;

namespace PublicFramework.Tests.Localization
{
    public class LocalizationSystemTests
    {
        private FakeEventBus _bus;
        private FakeSaveSystem _save;
        private LocalizationSystem _system;

        [SetUp]
        public void SetUp()
        {
            _bus = new FakeEventBus();
            _save = new FakeSaveSystem();
            _system = new LocalizationSystem(_bus, _save);
        }

        private static LocalizationTable Table(LanguageCode lang, params (int key, string val)[] pairs)
        {
            var entries = new LocalizationEntry[pairs.Length];
            for (int i = 0; i < pairs.Length; i++)
                entries[i] = new LocalizationEntry(pairs[i].key, pairs[i].val);
            return TestHelpers.MakeLocalizationTable(lang, entries);
        }

        // ---------- LoadTable ----------

        [Test]
        public void LoadTable_AddsToSupportedLanguages()
        {
            _system.LoadTable(Table(LanguageCode.Ko, (1, "안녕")));
            _system.LoadTable(Table(LanguageCode.En, (1, "Hello")));

            var supported = _system.GetSupportedLanguages();
            Assert.AreEqual(2, supported.Count);
            CollectionAssert.Contains(supported, LanguageCode.Ko);
            CollectionAssert.Contains(supported, LanguageCode.En);
        }

        [Test]
        public void LoadTable_PublishesLoadedEvent()
        {
            _system.LoadTable(Table(LanguageCode.Ko, (1, "안녕"), (2, "잘가")));

            var events = _bus.GetPublished<LocalizationLoadedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(LanguageCode.Ko, events[0].Language);
            Assert.AreEqual(2, events[0].KeyCount);
        }

        [Test]
        public void LoadTable_NullTable_NoOp()
        {
            _system.LoadTable(null);

            Assert.AreEqual(0, _system.GetSupportedLanguages().Count);
            Assert.AreEqual(0, _bus.GetPublished<LocalizationLoadedEvent>().Count);
        }

        [Test]
        public void LoadTable_SameLanguageTwice_DoesNotDoubleSupportedList()
        {
            _system.LoadTable(Table(LanguageCode.Ko, (1, "처음")));
            _system.LoadTable(Table(LanguageCode.Ko, (1, "다시")));

            Assert.AreEqual(1, _system.GetSupportedLanguages().Count);
        }

        // ---------- GetText ----------

        [Test]
        public void GetText_KnownKey_CurrentLanguage_ReturnsValue()
        {
            _system.LoadTable(Table(LanguageCode.Ko, (1, "안녕")));

            Assert.AreEqual("안녕", _system.GetText(1));
        }

        [Test]
        public void GetText_FormatArgs_AppliesFormat()
        {
            _system.LoadTable(Table(LanguageCode.Ko, (1, "{0}님 환영합니다")));

            Assert.AreEqual("Player님 환영합니다", _system.GetText(1, "Player"));
        }

        [Test]
        public void GetText_FormatError_ReturnsRawValue()
        {
            _system.LoadTable(Table(LanguageCode.Ko, (1, "{1} index out")));

            string result = _system.GetText(1, "only-one");

            Assert.AreEqual("{1} index out", result);
        }

        // ---------- Fallback ----------

        [Test]
        public void GetText_MissingInCurrent_FallsBackToEn()
        {
            _system.LoadTable(Table(LanguageCode.En, (1, "fallback-en")));
            _system.SetLanguage(LanguageCode.Ja);

            Assert.AreEqual("fallback-en", _system.GetText(1));
        }

        [Test]
        public void GetText_MissingInCurrentAndEn_FallsBackToKo()
        {
            _system.LoadTable(Table(LanguageCode.Ko, (1, "fallback-ko")));
            _system.SetLanguage(LanguageCode.Ja);

            Assert.AreEqual("fallback-ko", _system.GetText(1));
        }

        [Test]
        public void GetText_AllMissing_ReturnsKeyAsString()
        {
            string result = _system.GetText(42);
            Assert.AreEqual("42", result);
        }

        [Test]
        public void GetText_AllMissing_PublishesKeyMissingEvent()
        {
            _system.GetText(42);

            var events = _bus.GetPublished<LocalizationKeyMissingEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(42, events[0].Key);
        }

        // ---------- SetLanguage ----------

        [Test]
        public void SetLanguage_PublishesLanguageChanged()
        {
            _system.SetLanguage(LanguageCode.En);

            var events = _bus.GetPublished<LanguageChangedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(LanguageCode.Ko, events[0].OldLanguage);
            Assert.AreEqual(LanguageCode.En, events[0].NewLanguage);
        }

        [Test]
        public void SetLanguage_SameLanguage_NoOp()
        {
            _system.SetLanguage(LanguageCode.Ko);

            Assert.AreEqual(0, _bus.GetPublished<LanguageChangedEvent>().Count);
            Assert.AreEqual(0, _save.SaveCallCount);
        }

        [Test]
        public void SetLanguage_PersistsToSaveSystem()
        {
            _system.SetLanguage(LanguageCode.Ja);

            Assert.AreEqual(1, _save.SaveCallCount);
            Assert.IsTrue(_save.HasKey(0, "localization_language"));
            Assert.AreEqual((int)LanguageCode.Ja, _save.Load<int>(0, "localization_language"));
        }

        // ---------- Constructor ----------

        [Test]
        public void Constructor_LoadsSavedLanguage()
        {
            var save = new FakeSaveSystem();
            save.Save(0, "localization_language", (int)LanguageCode.Ja);

            var system = new LocalizationSystem(new FakeEventBus(), save);

            Assert.AreEqual(LanguageCode.Ja, system.CurrentLanguage);
        }

        [Test]
        public void Constructor_NoSavedLanguage_DefaultsToKo()
        {
            Assert.AreEqual(LanguageCode.Ko, _system.CurrentLanguage);
        }

        // ---------- HasKey / GetSupportedLanguages ----------

        [Test]
        public void HasKey_PresentInCurrent_ReturnsTrue()
        {
            _system.LoadTable(Table(LanguageCode.Ko, (1, "있음")));

            Assert.IsTrue(_system.HasKey(1));
            Assert.IsFalse(_system.HasKey(999));
        }

        [Test]
        public void HasKey_OnlyInFallback_ReturnsTrue()
        {
            _system.LoadTable(Table(LanguageCode.En, (5, "in-en")));
            _system.SetLanguage(LanguageCode.Ja);

            Assert.IsTrue(_system.HasKey(5));
        }
    }
}
