using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 같은 오브젝트 내 bool 필드가 true일 때만 Inspector에 표시.
    /// 사용 예: [ShowIf("_useCustomValue")]
    /// </summary>
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionFieldName { get; }
        public bool Invert { get; }

        public ShowIfAttribute(string conditionFieldName, bool invert = false)
        {
            ConditionFieldName = conditionFieldName;
            Invert = invert;
        }
    }
}
