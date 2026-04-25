using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// CharacterInfo 에 ChildTable 로 주입되는 프로필 key-value.
    /// 다국어 표시가 필요한 값은 valueKey(Language MID) 로 참조, 숫자·단위는 value 로 직접 기입.
    /// </summary>
    [System.Serializable]
    public class CharacterProfileEntry
    {
        [SerializeField, SheetAlias("key")] private string _key;
        [SerializeField, SheetAlias("value")] private string _value;
        [SerializeField, LocalizationKey, SheetAlias("valueKey")] private int _valueKey;

        public string Key => _key;
        public string Value => _value;
        public int ValueKey => _valueKey;
        public bool UsesLocalization => _valueKey > 0;
    }
}
