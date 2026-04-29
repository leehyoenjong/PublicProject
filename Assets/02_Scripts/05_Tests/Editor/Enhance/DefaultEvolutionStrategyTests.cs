using NUnit.Framework;

namespace PublicFramework.Tests.Enhance
{
    public class DefaultEvolutionStrategyTests
    {
        private EnhanceDataCollection _collection;
        private DefaultEvolutionStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _collection = TestHelpers.MakeDefaultEnhanceCollection();
            _strategy = new DefaultEvolutionStrategy(_collection);
        }

        private EquipmentInstanceData MakeReadyEquipment(int evolutionStage = 0)
        {
            return new EquipmentInstanceData
            {
                InstanceId = "eq1",
                Grade = (int)EquipmentGrade.Legendary,
                Level = 50,
                TranscendStep = 5,
                EvolutionStage = evolutionStage
            };
        }

        [Test]
        public void CanEnhance_LegendaryAndTranscendMax_ReturnsTrue()
        {
            var eq = MakeReadyEquipment();
            Assert.IsTrue(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void CanEnhance_BelowLegendary_ReturnsFalse()
        {
            var eq = new EquipmentInstanceData
            {
                Grade = (int)EquipmentGrade.Epic,
                TranscendStep = 5,
                EvolutionStage = 0
            };
            Assert.IsFalse(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void CanEnhance_TranscendNotMax_ReturnsFalse()
        {
            var eq = new EquipmentInstanceData
            {
                Grade = (int)EquipmentGrade.Legendary,
                TranscendStep = 4,
                EvolutionStage = 0
            };
            Assert.IsFalse(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void CanEnhance_AlreadyMaxStage_ReturnsFalse()
        {
            // default 컬렉션은 stages 0~2 (3 entries). stage=3 이면 더 이상 entry 없음
            var eq = MakeReadyEquipment(evolutionStage: 3);
            Assert.IsFalse(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void Execute_IncrementsStage()
        {
            var eq = MakeReadyEquipment(evolutionStage: 1);
            EnhanceResult result = _strategy.Execute(eq, default);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(EnhanceType.Evolution, result.Type);
            Assert.AreEqual(1, result.BeforeValue);
            Assert.AreEqual(2, result.AfterValue);
            Assert.AreEqual(2, eq.EvolutionStage);
        }

        [Test]
        public void GetCost_ReturnsCurrencyAndEvolutionMaterial()
        {
            var eq = MakeReadyEquipment(evolutionStage: 0);
            EnhanceCost cost = _strategy.GetCost(eq, default);

            Assert.AreEqual(2, cost.Materials.Length);
            Assert.AreEqual(EnhanceMaterialType.Currency, cost.Materials[0].MaterialType);
            Assert.AreEqual(100, cost.Materials[0].Amount);
            Assert.AreEqual(EnhanceMaterialType.EvolutionMaterial, cost.Materials[1].MaterialType);
            Assert.AreEqual(1, cost.Materials[1].Amount);
            Assert.AreEqual("item_evolution_stone", cost.Materials[1].MaterialMID);
            Assert.IsTrue(cost.CanAfford);
        }

        [Test]
        public void GetCost_DifferentStage_DifferentCost()
        {
            var eq0 = MakeReadyEquipment(evolutionStage: 0);
            var eq2 = MakeReadyEquipment(evolutionStage: 2);

            Assert.AreEqual(100, _strategy.GetCost(eq0, default).Materials[0].Amount);
            Assert.AreEqual(400, _strategy.GetCost(eq2, default).Materials[0].Amount);
        }

        [Test]
        public void GetCost_AtMaxStage_ReturnsCannotAfford()
        {
            var eq = MakeReadyEquipment(evolutionStage: 3);
            EnhanceCost cost = _strategy.GetCost(eq, default);

            Assert.AreEqual(0, cost.Materials.Length);
            Assert.IsFalse(cost.CanAfford);
        }

        [Test]
        public void GetDisplayProbability_AlwaysOne()
        {
            var eq = MakeReadyEquipment();
            Assert.AreEqual(1f, _strategy.GetDisplayProbability(eq, default));
        }
    }
}
