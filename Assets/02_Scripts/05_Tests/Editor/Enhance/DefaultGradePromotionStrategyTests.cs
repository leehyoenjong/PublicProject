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

        // ========== Phase 2-B: 보호권/축복/연속시도 ==========

        /// <summary>Decrease 정책 등급 정책 + 정책 컬럼(보호권/축복/연속) 비제로 컬렉션.</summary>
        private DefaultGradePromotionStrategy MakePolicyStrategy(
            int protectionTicketCost = 5,
            float blessingBoost = 0.1f,
            float consecutiveBonusBase = 0.05f,
            EnhanceFailPolicy failPolicy = EnhanceFailPolicy.Decrease)
        {
            var gradePolicies = new[]
            {
                TestHelpers.MakeGradePolicy(0, 10, 1.0f, 0, 5, EnhanceFailPolicy.Keep),
                TestHelpers.MakeGradePolicy(1, 20, 0.5f, 3, 10, failPolicy),
                TestHelpers.MakeGradePolicy(2, 30, 0.5f, 5, 20, failPolicy),
                TestHelpers.MakeGradePolicy(3, 40, 0.2f, 10, 50, failPolicy),
                TestHelpers.MakeGradePolicy(4, 50, 0f, 0, 0, EnhanceFailPolicy.Keep),
            };
            var gradeData = TestHelpers.MakeEnhanceData(
                EnhanceType.Grade,
                gradePolicies: gradePolicies,
                protectionTicketCost: protectionTicketCost,
                blessingBoost: blessingBoost,
                consecutiveBonusBase: consecutiveBonusBase);
            var collection = TestHelpers.MakeEnhanceCollection(gradeData);
            return new DefaultGradePromotionStrategy(collection, _probability, _eventBus);
        }

        [Test]
        public void Execute_UseBlessing_AddsBlessingBoostToRollProb()
        {
            DefaultGradePromotionStrategy strategy = MakePolicyStrategy(blessingBoost: 0.2f);
            _probability.NextSuccess = true;
            var eq = new EquipmentInstanceData { InstanceId = "eq1", Grade = 1, Level = 20, PityCount = 0 };

            strategy.Execute(eq, new EnhanceContext { Type = EnhanceType.Grade, UseBlessing = true });

            Assert.AreEqual(0.7f, _probability.LastBaseProb, 0.0001f); // 0.5 base + 0.2 blessing
        }

        [Test]
        public void Execute_ConsecutiveAttempts_AddsBonusPerAttempt()
        {
            DefaultGradePromotionStrategy strategy = MakePolicyStrategy(consecutiveBonusBase: 0.05f);
            _probability.NextSuccess = true;
            var eq = new EquipmentInstanceData { InstanceId = "eq1", Grade = 1, Level = 20, PityCount = 0 };

            strategy.Execute(eq, new EnhanceContext { Type = EnhanceType.Grade, ConsecutiveAttempts = 3 });

            Assert.AreEqual(0.65f, _probability.LastBaseProb, 0.0001f); // 0.5 base + 0.05*3
        }

        [Test]
        public void Execute_BlessingPlusConsecutive_StacksAndClampsAt100Percent()
        {
            DefaultGradePromotionStrategy strategy = MakePolicyStrategy(blessingBoost: 0.6f, consecutiveBonusBase: 0.5f);
            _probability.NextSuccess = true;
            var eq = new EquipmentInstanceData { InstanceId = "eq1", Grade = 1, Level = 20, PityCount = 0 };

            strategy.Execute(eq, new EnhanceContext { Type = EnhanceType.Grade, UseBlessing = true, ConsecutiveAttempts = 5 });

            Assert.AreEqual(1.0f, _probability.LastBaseProb, 0.0001f); // 0.5 + 0.6 + 2.5 → clamp 1
        }

        [Test]
        public void Execute_UseProtectionTicket_OverridesDecreasePolicyToKeep()
        {
            DefaultGradePromotionStrategy strategy = MakePolicyStrategy(failPolicy: EnhanceFailPolicy.Decrease);
            _probability.NextSuccess = false;
            var eq = new EquipmentInstanceData { InstanceId = "eq1", Grade = 2, Level = 30, PityCount = 0 };

            EnhanceResult result = strategy.Execute(eq, new EnhanceContext { Type = EnhanceType.Grade, UseProtectionTicket = true });

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(2, eq.Grade); // Decrease 무력화
            Assert.AreEqual(EnhanceFailPolicy.Keep, result.FailPolicy);
        }

        [Test]
        public void Execute_ProtectionTicketOff_AppliesDecreasePolicy()
        {
            DefaultGradePromotionStrategy strategy = MakePolicyStrategy(failPolicy: EnhanceFailPolicy.Decrease);
            _probability.NextSuccess = false;
            var eq = new EquipmentInstanceData { InstanceId = "eq1", Grade = 2, Level = 30, PityCount = 0 };

            EnhanceResult result = strategy.Execute(eq, new EnhanceContext { Type = EnhanceType.Grade });

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(1, eq.Grade); // Decrease 정상 적용
            Assert.AreEqual(EnhanceFailPolicy.Decrease, result.FailPolicy);
        }

        [Test]
        public void GetCost_UseProtectionTicket_AddsTicketCostEntry()
        {
            DefaultGradePromotionStrategy strategy = MakePolicyStrategy(protectionTicketCost: 3);
            var eq = new EquipmentInstanceData { InstanceId = "eq1", Grade = 1, Level = 20 };

            EnhanceCost cost = strategy.GetCost(eq, new EnhanceContext { Type = EnhanceType.Grade, UseProtectionTicket = true });

            Assert.AreEqual(2, cost.Materials.Length);
            Assert.AreEqual(EnhanceMaterialType.PromotionItem, cost.Materials[0].MaterialType);
            Assert.AreEqual(EnhanceMaterialType.ProtectionTicket, cost.Materials[1].MaterialType);
            Assert.AreEqual(3, cost.Materials[1].Amount);
        }

        [Test]
        public void GetDisplayProbability_UseBlessingAndConsecutive_ReflectsBoosts()
        {
            DefaultGradePromotionStrategy strategy = MakePolicyStrategy(blessingBoost: 0.1f, consecutiveBonusBase: 0.05f);
            var eq = new EquipmentInstanceData { InstanceId = "eq1", Grade = 1, Level = 20, PityCount = 0 };

            float prob = strategy.GetDisplayProbability(eq, new EnhanceContext
            {
                Type = EnhanceType.Grade,
                UseBlessing = true,
                ConsecutiveAttempts = 2
            });

            Assert.AreEqual(0.7f, prob, 0.0001f); // 0.5 + 0.1 + 0.05*2
        }
    }
}
