using NUnit.Framework;
using PublicFramework;
using PublicFramework.Editor.SheetImporter;

namespace PublicFramework.Tests.Buff
{
    /// <summary>
    /// BuffStatRangeValidator.IsSuspicious 순수 판정 로직 검증.
    /// 단위 규약: Percent = 비율(1.0=+100%), Multiplicative = 곱셈 인자(1.5=×1.5).
    /// </summary>
    public class BuffStatRangeValidatorTests
    {
        // --- Percent 레이어: 값 = 비율 ---

        [Test]
        public void Percent_NormalRatio_NotSuspicious()
        {
            // 0.15 = +15% (정상)
            Assert.IsFalse(BuffStatRangeValidator.IsSuspicious(StatLayer.Percent, 0.15f, out _));
        }

        [Test]
        public void Percent_HundredPercent_NotSuspicious()
        {
            // 1.0 = +100% (정상 범위 내)
            Assert.IsFalse(BuffStatRangeValidator.IsSuspicious(StatLayer.Percent, 1.0f, out _));
        }

        [Test]
        public void Percent_AtBoundary_NotSuspicious()
        {
            // |3.0| 은 상한과 같음 → 초과만 의심하므로 통과
            Assert.IsFalse(BuffStatRangeValidator.IsSuspicious(StatLayer.Percent, 3.0f, out _));
        }

        [Test]
        public void Percent_WholeNumberTypo_IsSuspicious()
        {
            // 0.15 를 15 로 적은 단위 실수 (= +1500%)
            Assert.IsTrue(BuffStatRangeValidator.IsSuspicious(StatLayer.Percent, 15f, out string detail));
            StringAssert.Contains("1500", detail);
        }

        [Test]
        public void Percent_LargeNegative_IsSuspicious()
        {
            // 절대값 기준 (디버프 단위 실수도 잡음)
            Assert.IsTrue(BuffStatRangeValidator.IsSuspicious(StatLayer.Percent, -5f, out _));
        }

        // --- Multiplicative 레이어: 값 = 곱셈 인자 ---

        [Test]
        public void Mult_NormalFactor_NotSuspicious()
        {
            // 1.5 = ×1.5 (정상)
            Assert.IsFalse(BuffStatRangeValidator.IsSuspicious(StatLayer.Multiplicative, 1.5f, out _));
        }

        [Test]
        public void Mult_Zero_IsSuspicious()
        {
            // ×0 은 스탯을 0 으로 만듦 → 비정상
            Assert.IsTrue(BuffStatRangeValidator.IsSuspicious(StatLayer.Multiplicative, 0f, out _));
        }

        [Test]
        public void Mult_Negative_IsSuspicious()
        {
            Assert.IsTrue(BuffStatRangeValidator.IsSuspicious(StatLayer.Multiplicative, -1f, out _));
        }

        [Test]
        public void Mult_AtBoundary_NotSuspicious()
        {
            // ×5.0 은 상한과 같음 → 통과
            Assert.IsFalse(BuffStatRangeValidator.IsSuspicious(StatLayer.Multiplicative, 5.0f, out _));
        }

        [Test]
        public void Mult_TooLarge_IsSuspicious()
        {
            // ×15 는 명백한 오타
            Assert.IsTrue(BuffStatRangeValidator.IsSuspicious(StatLayer.Multiplicative, 15f, out _));
        }

        // --- Flat 레이어: 절대값, 범위 검사 안 함 ---

        [Test]
        public void Flat_LargeValue_NotSuspicious()
        {
            // Flat 은 절대 가산값이라 큰 값도 정상
            Assert.IsFalse(BuffStatRangeValidator.IsSuspicious(StatLayer.Flat, 9999f, out _));
        }
    }
}
