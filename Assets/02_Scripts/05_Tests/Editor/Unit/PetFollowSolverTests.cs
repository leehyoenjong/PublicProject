using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Unit
{
    public class PetFollowSolverTests
    {
        private const float Eps = 0.001f;

        [Test]
        public void Behind_ReturnsOwnerMinusFacingTimesDistance()
        {
            Vector3 d = PetFollowSolver.ComputeDesiredPosition(
                PetFollowStrategy.Behind,
                new Vector3(10f, 0f, 0f),
                Vector2.right,
                2f,
                0f);

            Assert.AreEqual(8f, d.x, Eps);
            Assert.AreEqual(0f, d.y, Eps);
        }

        [Test]
        public void Side_ReturnsOwnerPlusPerpTimesDistance()
        {
            Vector3 d = PetFollowSolver.ComputeDesiredPosition(
                PetFollowStrategy.Side,
                Vector3.zero,
                Vector2.right,
                3f,
                0f);

            Assert.AreEqual(0f, d.x, Eps);
            Assert.AreEqual(3f, d.y, Eps, "Right 의 90° 좌회전 perp = Up");
        }

        [Test]
        public void Orbit_AtZeroRad_PointsRight()
        {
            Vector3 d = PetFollowSolver.ComputeDesiredPosition(
                PetFollowStrategy.Orbit,
                Vector3.zero,
                Vector2.right,
                5f,
                0f);

            Assert.AreEqual(5f, d.x, Eps);
            Assert.AreEqual(0f, d.y, Eps);
        }

        [Test]
        public void Orbit_AtHalfPi_PointsUp()
        {
            Vector3 d = PetFollowSolver.ComputeDesiredPosition(
                PetFollowStrategy.Orbit,
                Vector3.zero,
                Vector2.right,
                5f,
                Mathf.PI / 2f);

            Assert.AreEqual(0f, d.x, Eps);
            Assert.AreEqual(5f, d.y, Eps);
        }

        [Test]
        public void Aerial_BehindOwnerPlusUp()
        {
            Vector3 d = PetFollowSolver.ComputeDesiredPosition(
                PetFollowStrategy.Aerial,
                Vector3.zero,
                Vector2.right,
                4f,
                0f);

            Assert.AreEqual(-2f, d.x, Eps, "facing.x * dist*0.5 만큼 뒤로");
            Assert.AreEqual(4f, d.y, Eps);
        }

        [Test]
        public void Hover_AlwaysAboveOwner()
        {
            Vector3 d = PetFollowSolver.ComputeDesiredPosition(
                PetFollowStrategy.Hover,
                new Vector3(7f, 0f, 0f),
                Vector2.right,
                3f,
                0f);

            Assert.AreEqual(7f, d.x, Eps);
            Assert.AreEqual(3f, d.y, Eps);
        }

        [Test]
        public void ZeroFacing_FallsBackToRight()
        {
            Vector3 d = PetFollowSolver.ComputeDesiredPosition(
                PetFollowStrategy.Behind,
                new Vector3(5f, 0f, 0f),
                Vector2.zero,
                2f,
                0f);

            Assert.AreEqual(3f, d.x, Eps, "facing 0 → +X 폴백, 그 뒤(-X)*2 적용");
            Assert.AreEqual(0f, d.y, Eps);
        }

        [Test]
        public void NonNormalizedFacing_NormalizesInternally()
        {
            Vector3 d = PetFollowSolver.ComputeDesiredPosition(
                PetFollowStrategy.Behind,
                Vector3.zero,
                new Vector2(10f, 0f),
                2f,
                0f);

            Assert.AreEqual(-2f, d.x, Eps);
            Assert.AreEqual(0f, d.y, Eps);
        }

        [Test]
        public void ShouldCatchUp_BeyondThreshold_True()
        {
            bool result = PetFollowSolver.ShouldCatchUp(Vector3.zero, new Vector3(10f, 0f, 0f), 5f);

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldCatchUp_WithinThreshold_False()
        {
            bool result = PetFollowSolver.ShouldCatchUp(Vector3.zero, new Vector3(3f, 0f, 0f), 5f);

            Assert.IsFalse(result);
        }

        [Test]
        public void ShouldCatchUp_ZeroOrNegativeThreshold_False()
        {
            Assert.IsFalse(PetFollowSolver.ShouldCatchUp(Vector3.zero, new Vector3(100f, 0f, 0f), 0f));
            Assert.IsFalse(PetFollowSolver.ShouldCatchUp(Vector3.zero, new Vector3(100f, 0f, 0f), -1f));
        }
    }
}
