using System;

namespace PublicFramework
{
    /// <summary>
    /// 시트 임포터가 자동 매칭 단계에서 사용할 별칭을 필드에 선언.
    /// 복수 별칭 지원. 서브필드(중첩 SO/Serializable)의 별칭은 부모 필드 이름(접두 "_" 제거)을 prefix로 "parent.alias" 형태로 해석된다.
    /// 프로젝트별 예외는 여전히 Config 의 엔트리 별칭으로 오버라이드 가능.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class SheetAliasAttribute : Attribute
    {
        public string[] Aliases { get; }

        public SheetAliasAttribute(params string[] aliases)
        {
            Aliases = aliases ?? Array.Empty<string>();
        }
    }
}
