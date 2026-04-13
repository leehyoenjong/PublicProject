using System.Collections.Generic;
using System.Linq;

namespace PublicFramework
{
    /// <summary>
    /// <see cref="IFlexibleTableFilter"/> 기본 구현. 체이닝으로 조건을 누적한다.
    /// </summary>
    public class FlexibleTableFilter : IFlexibleTableFilter
    {
        private readonly List<FilterCondition> _conditions = new();

        public IReadOnlyList<FilterCondition> Conditions => _conditions;

        public IFlexibleTableFilter Eq(string column, object value) => Append(column, FlexibleFilterOp.Eq, value);
        public IFlexibleTableFilter Gt(string column, object value) => Append(column, FlexibleFilterOp.Gt, value);
        public IFlexibleTableFilter Lt(string column, object value) => Append(column, FlexibleFilterOp.Lt, value);

        public IFlexibleTableFilter In(string column, IEnumerable<object> values)
        {
            var list = values != null ? values.ToArray() : System.Array.Empty<object>();
            return Append(column, FlexibleFilterOp.In, list);
        }

        private IFlexibleTableFilter Append(string column, FlexibleFilterOp op, object value)
        {
            if (string.IsNullOrEmpty(column)) return this;
            _conditions.Add(new FilterCondition
            {
                Column = column,
                Op = op,
                Value = value,
            });
            return this;
        }
    }
}
