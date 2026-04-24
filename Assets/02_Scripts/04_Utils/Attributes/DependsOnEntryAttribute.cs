using System;

namespace PublicFramework
{
    /// <summary>
    /// 시트 임포터가 엔트리 간 의존 순서를 해결할 때 사용하는 힌트.
    /// 필드 타입이 구체 ScriptableObject 타입이면 리플렉션으로 자동 감지되지만,
    /// 필드 타입이 ScriptableObject 등 베이스일 때는 어떤 서브타입을 가리키는지 알 수 없어 이 힌트가 필요하다.
    /// 복수 타입을 나열하면 각 타입을 TargetType 으로 가진 엔트리 모두가 선행 실행 대상이 된다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class DependsOnEntryAttribute : Attribute
    {
        public Type[] TargetTypes { get; }

        public DependsOnEntryAttribute(params Type[] targetTypes)
        {
            TargetTypes = targetTypes ?? Array.Empty<Type>();
        }
    }
}
