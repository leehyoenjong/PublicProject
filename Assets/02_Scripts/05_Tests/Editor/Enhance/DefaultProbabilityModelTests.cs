using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Enhance
{
    public class DefaultProbabilityModelTests
    {
        private DefaultProbabilityModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new DefaultProbabilityModel();
            Random.InitState(42);
        }

        [Test]
        public void Roll_FullProbability_AlwaysSucceeds()
        {
            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(_model.Roll(1f, 0, 0));
            }
        }

        [Test]
        public void Roll_ZeroProbability_NeverSucceedsUntilPity()
        {
            for (int i = 0; i < 50; i++)
            {
                Assert.IsFalse(_model.Roll(0f, 0, 0));
            }
        }

        [Test]
        public void Roll_PityReached_GuaranteedSuccess()
        {
            // maxPity=5, pityCount>=4 → 보장
            Assert.IsTrue(_model.Roll(0f, 4, 5));
            Assert.IsTrue(_model.Roll(0f, 10, 5));
        }

        [Test]
        public void Roll_PityNotReached_UsesBaseProb()
        {
            // maxPity=5, pityCount=3 → 확률 0 이면 실패
            Assert.IsFalse(_model.Roll(0f, 3, 5));
        }

        [Test]
        public void Roll_NoPity_ZeroMaxPity_IgnoresPityLogic()
        {
            // maxPity=0 이면 천장 비활성
            Assert.IsFalse(_model.Roll(0f, 999, 0));
        }

        [Test]
        public void GetDisplayProb_PityReached_Returns100Percent()
        {
            Assert.AreEqual(1f, _model.GetDisplayProb(0.1f, 4, 5));
        }

        [Test]
        public void GetDisplayProb_BeforePity_ReturnsBase()
        {
            Assert.AreEqual(0.3f, _model.GetDisplayProb(0.3f, 1, 5), 0.0001f);
        }

        [Test]
        public void Roll_StatisticalConvergence_50Percent()
        {
            Random.InitState(1234);
            int successes = 0;
            const int trials = 2000;

            for (int i = 0; i < trials; i++)
            {
                if (_model.Roll(0.5f, 0, 0)) successes++;
            }

            float ratio = successes / (float)trials;
            Assert.That(ratio, Is.InRange(0.45f, 0.55f), $"실제 성공률: {ratio:F3}");
        }
    }
}
