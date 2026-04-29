using System.Collections.Generic;
using NUnit.Framework;

namespace PublicFramework.Tests.Stage
{
    public class WaveTransitionEvaluatorTests
    {
        [Test]
        public void AllKill_EmptyDict_ReturnsTrue()
        {
            var dict = new Dictionary<string, int>();

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.AllKill, null, 0f, dict, 0f);

            Assert.IsTrue(ok);
        }

        [Test]
        public void AllKill_ZeroCounts_ReturnsTrue()
        {
            var dict = new Dictionary<string, int> { { "mon_a", 0 }, { "mon_b", 0 } };

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.AllKill, null, 0f, dict, 0f);

            Assert.IsTrue(ok);
        }

        [Test]
        public void AllKill_AnyAlive_ReturnsFalse()
        {
            var dict = new Dictionary<string, int> { { "mon_a", 0 }, { "mon_b", 1 } };

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.AllKill, null, 0f, dict, 0f);

            Assert.IsFalse(ok);
        }

        [Test]
        public void BossKill_TargetAbsent_ReturnsTrue()
        {
            var dict = new Dictionary<string, int> { { "mon_minion", 5 } };

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.BossKill, "mon_boss", 0f, dict, 0f);

            Assert.IsTrue(ok, "보스 MID 가 dict 에 없으면 클리어로 간주");
        }

        [Test]
        public void BossKill_TargetAlive_ReturnsFalse()
        {
            var dict = new Dictionary<string, int> { { "mon_boss", 1 } };

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.BossKill, "mon_boss", 0f, dict, 0f);

            Assert.IsFalse(ok);
        }

        [Test]
        public void BossKill_TargetIdEmpty_ReturnsFalse()
        {
            var dict = new Dictionary<string, int>();

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.BossKill, "", 0f, dict, 0f);

            Assert.IsFalse(ok, "target MID 미지정이면 클리어 안 함");
        }

        [Test]
        public void SpecificKill_TargetAbsent_ReturnsTrue()
        {
            var dict = new Dictionary<string, int> { { "mon_other", 3 } };

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.SpecificKill, "mon_target", 0f, dict, 0f);

            Assert.IsTrue(ok);
        }

        [Test]
        public void Timer_ElapsedReachedThreshold_ReturnsTrue()
        {
            var dict = new Dictionary<string, int> { { "mon_a", 5 } };

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.Timer, null, 10f, dict, 10f);

            Assert.IsTrue(ok, "alive 가 남아있어도 timer 초과 시 전환");
        }

        [Test]
        public void Timer_ElapsedBelowThreshold_ReturnsFalse()
        {
            var dict = new Dictionary<string, int>();

            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.Timer, null, 10f, dict, 9.99f);

            Assert.IsFalse(ok);
        }

        [Test]
        public void Timer_ZeroOrNegativeThreshold_ReturnsFalse()
        {
            var dict = new Dictionary<string, int>();

            Assert.IsFalse(WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.Timer, null, 0f, dict, 100f));
            Assert.IsFalse(WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.Timer, null, -1f, dict, 100f));
        }

        [Test]
        public void NullDict_AllKill_ReturnsTrue()
        {
            bool ok = WaveTransitionEvaluator.ShouldTransition(
                WaveTransitionCondition.AllKill, null, 0f, null, 0f);

            Assert.IsTrue(ok, "dict null 도 카운트 0 으로 간주");
        }
    }
}
