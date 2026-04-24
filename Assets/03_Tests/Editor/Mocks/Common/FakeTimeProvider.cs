using System;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 ITimeProvider. 시간을 명시적으로 고정/조작.</summary>
    public class FakeTimeProvider : ITimeProvider
    {
        private DateTime _now;

        public DateTime NowUtc => _now;
        public long NowUnixSeconds => new DateTimeOffset(_now, TimeSpan.Zero).ToUnixTimeSeconds();

        public FakeTimeProvider()
        {
            _now = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        }

        public FakeTimeProvider(DateTime utc)
        {
            _now = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        }

        public void Set(DateTime utc)
        {
            _now = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        }

        public void Advance(TimeSpan delta)
        {
            _now = _now.Add(delta);
        }
    }
}
