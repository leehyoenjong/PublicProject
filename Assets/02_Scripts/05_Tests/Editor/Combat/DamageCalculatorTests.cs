using NUnit.Framework;

namespace PublicFramework.Tests.Combat
{
    public class DamageCalculatorTests
    {
        [Test]
        public void ZeroDefense_ReturnsRawDamage()
        {
            Assert.AreEqual(100f, DamageCalculator.Mitigate(100f, 0f));
            Assert.AreEqual(100f, DamageCalculator.Mitigate(100f, -50f), "음수 DEF 도 경감 없음");
        }

        [Test]
        public void DefenseEqualsConstant_HalvesDamage()
        {
            // K=100, DEF=100 → mult = 100/200 = 0.5
            Assert.AreEqual(50f, DamageCalculator.Mitigate(100f, 100f, 100f), 0.001f);
        }

        [Test]
        public void HigherDefense_ReducesMore_Monotonic()
        {
            float d100 = DamageCalculator.Mitigate(100f, 50f);
            float d300 = DamageCalculator.Mitigate(100f, 300f);
            Assert.Less(d300, d100, "DEF 가 높을수록 받는 데미지 ↓");
        }

        [Test]
        public void NonPositiveRaw_ReturnsZero()
        {
            Assert.AreEqual(0f, DamageCalculator.Mitigate(0f, 100f));
            Assert.AreEqual(0f, DamageCalculator.Mitigate(-30f, 100f));
        }

        [Test]
        public void Result_NeverNegative_AndNeverExceedsRaw()
        {
            float raw = 100f;
            foreach (float def in new[] { 1f, 10f, 100f, 1000f, 99999f })
            {
                float r = DamageCalculator.Mitigate(raw, def);
                Assert.GreaterOrEqual(r, 0f);
                Assert.LessOrEqual(r, raw, "경감 결과가 원본보다 클 수 없음");
            }
        }

        [Test]
        public void LargerConstant_MeansLessMitigation()
        {
            // 같은 DEF 라도 K 가 크면 경감 효율 ↓ → 받는 데미지 ↑
            float smallK = DamageCalculator.Mitigate(100f, 100f, 50f);
            float largeK = DamageCalculator.Mitigate(100f, 100f, 500f);
            Assert.Less(smallK, largeK);
        }
    }
}
