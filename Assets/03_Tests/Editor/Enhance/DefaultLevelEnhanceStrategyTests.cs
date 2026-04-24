using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Enhance
{
    public class DefaultLevelEnhanceStrategyTests
    {
        private EnhanceConfig _config;
        private DefaultLevelEnhanceStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EnhanceConfig>();
            _strategy = new DefaultLevelEnhanceStrategy(_config);
        }

        private EquipmentInstanceData MakeEquipment(int level = 0, int grade = 0)
        {
            return new EquipmentInstanceData { InstanceId = "eq1", Level = level, Grade = grade };
        }

        [Test]
        public void CanEnhance_BelowMax_ReturnsTrue()
        {
            var eq = MakeEquipment(level: 5, grade: 0); // max=10
            Assert.IsTrue(_strategy.CanEnhance(eq, new EnhanceContext { Type = EnhanceType.Level }));
        }

        [Test]
        public void CanEnhance_AtMax_ReturnsFalse()
        {
            var eq = MakeEquipment(level: 10, grade: 0);
            Assert.IsFalse(_strategy.CanEnhance(eq, new EnhanceContext { Type = EnhanceType.Level }));
        }

        [Test]
        public void Execute_IncrementsLevel()
        {
            var eq = MakeEquipment(level: 3, grade: 0);
            EnhanceResult result = _strategy.Execute(eq, new EnhanceContext { Type = EnhanceType.Level });

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(3, result.BeforeValue);
            Assert.AreEqual(4, result.AfterValue);
            Assert.AreEqual(4, eq.Level);
        }

        [Test]
        public void GetCost_ReturnsCurrencyAndStone()
        {
            var eq = MakeEquipment(level: 0, grade: 0);
            EnhanceCost cost = _strategy.GetCost(eq, new EnhanceContext { Type = EnhanceType.Level });

            Assert.IsNotNull(cost.Materials);
            Assert.AreEqual(2, cost.Materials.Length);
            Assert.AreEqual(EnhanceMaterialType.Currency, cost.Materials[0].MaterialType);
            Assert.AreEqual(EnhanceMaterialType.Stone, cost.Materials[1].MaterialType);
            Assert.IsTrue(cost.CanAfford);
        }

        [Test]
        public void GetDisplayProbability_AlwaysOne()
        {
            var eq = MakeEquipment();
            Assert.AreEqual(1f, _strategy.GetDisplayProbability(eq, new EnhanceContext { Type = EnhanceType.Level }));
        }
    }
}
