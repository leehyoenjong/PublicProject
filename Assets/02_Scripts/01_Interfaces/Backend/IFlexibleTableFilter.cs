using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 유연 테이블 쿼리 필터 추상화. 뒤끝 SDK 의 Where/조건 타입을 외부로 노출하지 않기 위해
    /// 프레임워크 내부용 중립 빌더를 제공한다.
    /// 구현체가 축적한 조건은 <see cref="Conditions"/> 로 확인 가능하며, BackendDatabase 가 이를
    /// BACKND.Database 의 실제 필터로 변환한다(리플렉션).
    /// </summary>
    public interface IFlexibleTableFilter
    {
        IFlexibleTableFilter Eq(string column, object value);
        IFlexibleTableFilter Gt(string column, object value);
        IFlexibleTableFilter Lt(string column, object value);
        IFlexibleTableFilter In(string column, IEnumerable<object> values);

        IReadOnlyList<FilterCondition> Conditions { get; }
    }

    public enum FlexibleFilterOp
    {
        Eq,
        Gt,
        Lt,
        In,
    }

    public struct FilterCondition
    {
        public string Column;
        public FlexibleFilterOp Op;
        public object Value;
    }
}
