using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>
    /// 결정론적 IRandomProvider. NextInt 호출마다 미리 큐에 넣은 값을 반환한다.
    /// 큐가 비면 0 반환. minInclusive 가 음수가 아닌 한 maxExclusive 미만으로 클램프 한다.
    /// </summary>
    internal class FakeRandomProvider : IRandomProvider
    {
        private readonly Queue<int> _queue = new();

        public FakeRandomProvider(params int[] values)
        {
            if (values == null) return;
            foreach (int v in values) _queue.Enqueue(v);
        }

        public void Enqueue(int value) => _queue.Enqueue(value);

        public int NextInt(int maxExclusive)
        {
            if (_queue.Count == 0) return 0;
            int v = _queue.Dequeue();
            if (v < 0) v = 0;
            if (v >= maxExclusive) v = maxExclusive - 1;
            return v;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (_queue.Count == 0) return minInclusive;
            int v = _queue.Dequeue();
            if (v < minInclusive) v = minInclusive;
            if (v >= maxExclusive) v = maxExclusive - 1;
            return v;
        }
    }
}
