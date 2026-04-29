using NUnit.Framework;

namespace PublicFramework.Tests.Enhance
{
    public class DefaultGradePromotionStrategyTests
    {
        private EnhanceDataCollection _collection;
        private FakeProbabilityModel _probability;
        private FakeEventBus _eventBus;
        private DefaultGradePromotionStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _collection = TestHelpers.MakeDefaultEnhanceCollection();
            _probability = new FakeProbabilityModel();
            _eventBus = new FakeEventBus();
            _strategy = new DefaultGradePromotionStrategy(_collection, _probability, _eventBus);
        }

        private int GetMaxLevel(int grade)
        {
            EnhanceData gradeData = _collection.Find(EnhanceType.Grade);
            GradePolicyEntry policy = gradeData.FindGradePolicy(grade);
            return policy != null ? policy.MaxLevel : 0;
        }

        private EquipmentInstanceData MakeEquipment(int grade = 0, int level = -1, int pityCount = 0)
        {
            int effectiveLevel = level < 0 ? GetMaxLevel(grade) : level;
            return new EquipmentInstanceData
            {
                InstanceId = "eq1",
                Grade = grade,
                Level = effectiveLevel,
                PityCount = pityCount
            };
        }

        [Test]
        public void CanEnhance_AtMaxGradeLegendary_ReturnsFalse()
        {
            var eq = MakeEquipment(grade: (int)EquipmentGrade.Legendary);
            Assert.IsFalse(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void CanEnhance_NotMaxLevel_ReturnsFalse()
        {
            var eq = MakeEquipment(grade: 0, level: 5); // max=10
            Assert.IsFalse(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void CanEnhance_MaxLevelForGrade_ReturnsTrue()
        {
            var eq = MakeEquipment(grade: 0);
            Assert.IsTrue(_strategy.CanEnhance(eq, default));
        }

        [Test]
        public void Execute_Success_IncrementsGradeAndResetsPity()
        {
            _probability.NextSuccess = true;
            var eq = MakeEquipment(grade: 1, pityCount: 2);
            EnhanceResult result = _strategy.Execute(eq, default);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, result.BeforeValue);
            Assert.AreEqual(2, result.AfterValue);
            Assert.AreEqual(2, eq.Grade);
            Assert.AreEqual(0, eq.PityCount);
        }

        [Test]
        public void Execute_Failure_IncrementsPity()
        {
            _probability.NextSuccess = false;
            var eq = MakeEquipment(grade: 1, pityCount: 0);
            EnhanceResult result = _strategy.Execute(eq, default);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(1, eq.Grade); // Keep 기본 정책
            Assert.AreEqual(1, eq.PityCount);
        }

        [Test]
        public void Execute_SuccessAtPityBoundary_PublishesPityReached()
        {
            // grade=1 은 maxPity=3. pityCount=2 이면 다음 호출이 천장.
            _probability.NextSuccess = true;
            var eq = MakeEquipment(grade: 1, pityCount: 2);

            _strategy.Execute(eq, default);

            var pityEvents = _eventBus.GetPublished<PityReachedEvent>();
            Assert.AreEqual(1, pityEvents.Count);
            Assert.AreEqual(EnhanceType.Grade, pityEvents[0].EnhanceType);
        }

        [Test]
        public void Execute_SuccessBeforePity_DoesNotPublishPity()
        {
            _probability.NextSuccess = true;
            var eq = MakeEquipment(grade: 1, pityCount: 0);

            _strategy.Execute(eq, default);

            Assert.AreEqual(0, _eventBus.GetPublished<PityReachedEvent>().Count);
        }

        [Test]
        public void GetCost_ReturnsPromotionItem()
        {
            var eq = MakeEquipment(grade: 1);
            EnhanceCost cost = _strategy.GetCost(eq, default);

            Assert.AreEqual(1, cost.Materials.Length);
            Assert.AreEqual(EnhanceMaterialType.PromotionItem, cost.Materials[0].MaterialType);
            Assert.AreEqual(10, cost.Materials[0].Amount); // grade=1 cost=10
        }

        [Test]
        public void GetDisplayProbability_DelegatesToModel()
        {
            var eq = MakeEquipment(grade: 2);
            float prob = _strategy.GetDisplayProbability(eq, default);
            Assert.AreEqual(0.5f, prob, 0.0001f); // grade 2 baseProb
        }

        [Test]
        public void Execute_PassesPityParamsToModel()
        {
            _probability.NextSuccess = true;
            var eq = MakeEquipment(grade: 2, pityCount: 3);

            _strategy.Execute(eq, default);

            Assert.AreEqual(3, _probability.LastPityCount);
            Assert.AreEqual(5, _probability.LastMaxPity); // grade 2 maxPity
            Assert.AreEqual(0.5f, _probability.LastBaseProb, 0.0001f);
        }
    }
}
