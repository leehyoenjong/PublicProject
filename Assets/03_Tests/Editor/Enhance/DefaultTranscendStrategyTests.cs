using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Enhance
{
    public class DefaultTranscendStrategyTests
    {
        private EnhanceConfig _config;
        private DefaultTranscendStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EnhanceConfig>();
            _strategy = new DefaultTranscendStrategy(_config);
        }

        private EquipmentInstanceData MakeLegendaryMaxLevel(int transcendStep = 0)
        {
            return new EquipmentInstanceData
            {
                InstanceId = "eq1",
                Grade = (int)EquipmentGrade.Legendary,
                Level = 50, // max for legendary
                TranscendStep = transcendStep
            };
        }

        [Test]
        public void CanEnhance_LegendaryAtMaxLevel_ReturnsTrue()
        {
            var eq = MakeLegendaryMaxLevel();
            Assert.IsTrue(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void CanEnhance_BelowLegendary_ReturnsFalse()
        {
            var eq = new EquipmentInstanceData
            {
                Grade = (int)EquipmentGrade.Epic,
                Level = 40
            };
            Assert.IsFalse(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void CanEnhance_NotMaxLevel_ReturnsFalse()
        {
            var eq = new EquipmentInstanceData
            {
                Grade = (int)EquipmentGrade.Legendary,
                Level = 49
            };
            Assert.IsFalse(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void CanEnhance_AlreadyMaxStep_ReturnsFalse()
        {
            var eq = MakeLegendaryMaxLevel(transcendStep: 5); // max=5
            Assert.IsFalse(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void Execute_IncrementsStep()
        {
            var eq = MakeLegendaryMaxLevel(transcendStep: 2);
            EnhanceResult result = _strategy.Execute(eq, default);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.BeforeValue);
            Assert.AreEqual(3, result.AfterValue);
            Assert.AreEqual(3, eq.TranscendStep);
        }

        [Test]
        public void GetCost_ReturnsTranscendItemAndSameEquipment()
        {
            var eq = MakeLegendaryMaxLevel();
            EnhanceCost cost = _strategy.GetCost(eq, default);

            Assert.AreEqual(2, cost.Materials.Length);
            Assert.AreEqual(EnhanceMaterialType.TranscendItem, cost.Materials[0].MaterialType);
            Assert.AreEqual(EnhanceMaterialType.SameEquipment, cost.Materials[1].MaterialType);
            Assert.AreEqual(1, cost.Materials[1].Amount);
        }
    }
}
