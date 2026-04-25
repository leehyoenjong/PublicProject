using System;

namespace PublicFramework
{
    /// <summary>System.Random 기반 기본 IRandomProvider. 시드 제어 가능.</summary>
    public class DefaultRandomProvider : IRandomProvider
    {
        private readonly Random _random;

        public DefaultRandomProvider() => _random = new Random();
        public DefaultRandomProvider(int seed) => _random = new Random(seed);

        public int NextInt(int maxExclusive) => _random.Next(maxExclusive);
        public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    }
}
