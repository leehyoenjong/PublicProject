using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Item
{
    public class FlyTrajectoryTests
    {
        private const float Eps = 0.001f;

        [Test]
        public void Evaluate_AtZero_ReturnsStart()
        {
            Vector3 d = FlyTrajectory.Evaluate(new Vector3(1f, 2f, 3f), new Vector3(10f, 20f, 30f), 0f, 5f);

            Assert.AreEqual(1f, d.x, Eps);
            Assert.AreEqual(2f, d.y, Eps, "양 끝점은 arc 영향 없음");
            Assert.AreEqual(3f, d.z, Eps);
        }

        [Test]
        public void Evaluate_AtOne_ReturnsTarget()
        {
            Vector3 d = FlyTrajectory.Evaluate(new Vector3(1f, 2f, 3f), new Vector3(10f, 20f, 30f), 1f, 5f);

            Assert.AreEqual(10f, d.x, Eps);
            Assert.AreEqual(20f, d.y, Eps, "양 끝점은 arc 영향 없음");
            Assert.AreEqual(30f, d.z, Eps);
        }

        [Test]
        public void Evaluate_AtHalf_ZeroArc_LinearMidpoint()
        {
            Vector3 d = FlyTrajectory.Evaluate(Vector3.zero, new Vector3(10f, 0f, 0f), 0.5f, 0f);

            Assert.AreEqual(5f, d.x, Eps);
            Assert.AreEqual(0f, d.y, Eps);
        }

        [Test]
        public void Evaluate_AtHalf_PositiveArc_PeaksAboveLinear()
        {
            Vector3 d = FlyTrajectory.Evaluate(Vector3.zero, new Vector3(10f, 0f, 0f), 0.5f, 4f);

            Assert.AreEqual(5f, d.x, Eps);
            Assert.AreEqual(4f, d.y, Eps, "sin(π/2)=1 이므로 +arcHeight 만큼 위");
        }

        [Test]
        public void Evaluate_TBeyondRange_ClampsTo01()
        {
            Vector3 below = FlyTrajectory.Evaluate(Vector3.zero, new Vector3(10f, 0f, 0f), -0.5f, 4f);
            Vector3 above = FlyTrajectory.Evaluate(Vector3.zero, new Vector3(10f, 0f, 0f), 1.5f, 4f);

            Assert.AreEqual(0f, below.x, Eps, "t<0 → t=0 (start)");
            Assert.AreEqual(0f, below.y, Eps);
            Assert.AreEqual(10f, above.x, Eps, "t>1 → t=1 (target)");
            Assert.AreEqual(0f, above.y, Eps);
        }

        [Test]
        public void Evaluate_SameStartAndTarget_StaysAtPoint_WhenArcZero()
        {
            Vector3 p = new Vector3(7f, 7f, 7f);
            Vector3 d = FlyTrajectory.Evaluate(p, p, 0.5f, 0f);

            Assert.AreEqual(7f, d.x, Eps);
            Assert.AreEqual(7f, d.y, Eps);
            Assert.AreEqual(7f, d.z, Eps);
        }

        [Test]
        public void Evaluate_NegativeArc_TreatedAsZero_NoLift()
        {
            Vector3 d = FlyTrajectory.Evaluate(Vector3.zero, new Vector3(10f, 0f, 0f), 0.5f, -3f);

            Assert.AreEqual(5f, d.x, Eps);
            Assert.AreEqual(0f, d.y, Eps, "음수 arcHeight 는 0 으로 폴백");
        }
    }
}
