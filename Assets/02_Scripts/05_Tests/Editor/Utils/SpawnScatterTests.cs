using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Utils
{
    /// <summary>
    /// SpawnScatter 황금각 나선 분산(anti-stack) 검증 — 결정론·비충돌·반경 성장.
    /// </summary>
    public class SpawnScatterTests
    {
        [Test]
        public void SpiralOffset_Deterministic_SameInputSameOutput()
        {
            Assert.AreEqual(SpawnScatter.SpiralOffset(7, 1.5f), SpawnScatter.SpiralOffset(7, 1.5f));
        }

        [Test]
        public void SpiralOffset_IndexZero_NearOrigin()
        {
            Assert.Less(SpawnScatter.SpiralOffset(0, 1f).magnitude, 1f);
        }

        [Test]
        public void SpiralOffset_AdjacentIndices_DoNotCollide()
        {
            for (int i = 0; i < 20; i++)
            {
                Vector3 a = SpawnScatter.SpiralOffset(i, 1f);
                Vector3 b = SpawnScatter.SpiralOffset(i + 1, 1f);
                Assert.Greater(Vector3.Distance(a, b), 0.1f, $"index {i} vs {i + 1} 너무 가까움");
            }
        }

        [Test]
        public void SpiralOffset_RadiusGrowsWithIndex()
        {
            Assert.Less(SpawnScatter.SpiralOffset(1, 1f).magnitude, SpawnScatter.SpiralOffset(50, 1f).magnitude);
        }

        [Test]
        public void SpiralOffset_NegativeIndex_TreatedAsZero()
        {
            Assert.AreEqual(SpawnScatter.SpiralOffset(0, 1f), SpawnScatter.SpiralOffset(-5, 1f));
        }

        [Test]
        public void SpiralOffset_StaysOnZPlane()
        {
            Assert.AreEqual(0f, SpawnScatter.SpiralOffset(13, 2f).z, 1e-5f);
        }

        [Test]
        public void SpiralOffset_ScalesWithSpacing()
        {
            float small = SpawnScatter.SpiralOffset(5, 1f).magnitude;
            float big = SpawnScatter.SpiralOffset(5, 3f).magnitude;
            Assert.AreEqual(small * 3f, big, 1e-4f);
        }
    }
}
