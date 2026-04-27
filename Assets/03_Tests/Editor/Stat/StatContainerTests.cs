using System;
using NUnit.Framework;

namespace PublicFramework.Tests.Stat
{
    public class StatContainerTests
    {
        private FakeEventBus _eventBus;
        private FakeTimeProvider _time;
        private StatContainer _c;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new FakeEventBus();
            _time = new FakeTimeProvider(new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc));
            _c = new StatContainer("u1", level: 1, _eventBus, _time);
        }

        // ────────────────────────────────────────────
        // Base / Final
        // ────────────────────────────────────────────
        [Test]
        public void NewContainer_AllStats_AreZero()
        {
            Assert.AreEqual(0f, _c.GetFinalValue(StatType.Attack));
            Assert.AreEqual(0f, _c.GetFinalValue(StatType.HP));
            Assert.AreEqual(1, _c.Level);
            Assert.IsFalse(_c.IsAlive);
        }

        [Test]
        public void SetBaseValue_Reflects_InFinal()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            Assert.AreEqual(100f, _c.GetFinalValue(StatType.Attack));
            Assert.AreEqual(100f, _c.GetBaseValue(StatType.Attack));
        }

        // ────────────────────────────────────────────
        // 4단계 계산식
        // ────────────────────────────────────────────
        [Test]
        public void FourStageFormula_Flat_Adds()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 50f, source: "eq"));
            Assert.AreEqual(150f, _c.GetFinalValue(StatType.Attack));
        }

        [Test]
        public void FourStageFormula_Percent_AppliesAfterFlat()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 70f));
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Percent, 0.25f));
            Assert.AreEqual((100f + 70f) * 1.25f, _c.GetFinalValue(StatType.Attack), 0.001f);
        }

        [Test]
        public void FourStageFormula_Multiplicative_StacksAsProduct()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Multiplicative, 1.5f));
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Multiplicative, 1.2f));
            Assert.AreEqual(100f * 1.5f * 1.2f, _c.GetFinalValue(StatType.Attack), 0.001f);
        }

        [Test]
        public void FourStageFormula_FullCombination()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 70f));
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Percent, 0.25f));
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Multiplicative, 1.5f));

            float expected = (100f + 70f) * 1.25f * 1.5f;
            Assert.AreEqual(expected, _c.GetFinalValue(StatType.Attack), 0.001f);
        }

        [Test]
        public void Final_NeverNegative()
        {
            _c.SetBaseValue(StatType.Attack, 10f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, -100f));
            Assert.AreEqual(0f, _c.GetFinalValue(StatType.Attack));
        }

        // ────────────────────────────────────────────
        // Modifier 추가/제거
        // ────────────────────────────────────────────
        [Test]
        public void RemoveModifier_RestoresFinal()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            var m = new StatModifier(StatType.Attack, StatLayer.Flat, 50f);
            _c.AddModifier(m);
            _c.RemoveModifier(m);
            Assert.AreEqual(100f, _c.GetFinalValue(StatType.Attack));
        }

        [Test]
        public void RemoveModifiersFromSource_RemovesAll_FromThatSource()
        {
            object equip = "sword";
            object buff = "rage";
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 30f, source: equip));
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Percent, 0.1f, source: equip));
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 10f, source: buff));

            int removed = _c.RemoveModifiersFromSource(equip);
            Assert.AreEqual(2, removed);
            Assert.AreEqual(100f + 10f, _c.GetFinalValue(StatType.Attack), 0.001f);
        }

        // ────────────────────────────────────────────
        // 커스텀 스탯
        // ────────────────────────────────────────────
        [Test]
        public void CustomStat_BasicGetSet()
        {
            _c.SetBaseValue("MP", 200f);
            Assert.AreEqual(200f, _c.GetFinalValue("MP"));
            Assert.AreEqual(200f, _c.GetBaseValue("MP"));
        }

        [Test]
        public void CustomStat_AppliesFourStage()
        {
            _c.SetBaseValue("Focus", 50f);
            _c.AddModifier(new StatModifier(default, StatLayer.Flat, 20f, customKey: "Focus"));
            _c.AddModifier(new StatModifier(default, StatLayer.Percent, 0.5f, customKey: "Focus"));
            Assert.AreEqual((50f + 20f) * 1.5f, _c.GetFinalValue("Focus"), 0.001f);
        }

        [Test]
        public void CustomStat_DoesNotAffectEnumStat()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.SetBaseValue("Focus", 50f);
            _c.AddModifier(new StatModifier(default, StatLayer.Flat, 999f, customKey: "Focus"));
            Assert.AreEqual(100f, _c.GetFinalValue(StatType.Attack));
        }

        // ────────────────────────────────────────────
        // 성장 커브
        // ────────────────────────────────────────────
        [Test]
        public void GrowthCurve_Linear_AppliesOnLevelChange()
        {
            _c.SetGrowthCurve(StatType.HP, new LevelCurve(GrowthCurve.Linear, 100f, 10f));
            Assert.AreEqual(110f, _c.GetFinalValue(StatType.HP));  // lv1 = 100 + 1*10
            _c.SetLevel(5);
            Assert.AreEqual(150f, _c.GetFinalValue(StatType.HP));  // lv5 = 100 + 5*10
        }

        [Test]
        public void GrowthCurve_Quadratic_GrowsFaster()
        {
            _c.SetGrowthCurve(StatType.Attack, new LevelCurve(GrowthCurve.Quadratic, 0f, 1f));
            _c.SetLevel(10);
            Assert.AreEqual(100f, _c.GetFinalValue(StatType.Attack));  // lv10² × 1
        }

        [Test]
        public void GrowthCurve_Exponential_GrowsByFactor()
        {
            _c.SetGrowthCurve(StatType.HP, new LevelCurve(GrowthCurve.Exponential, 100f, 0.10f));
            _c.SetLevel(10);
            Assert.AreEqual(100f * (float)Math.Pow(1.10, 10), _c.GetFinalValue(StatType.HP), 0.01f);
        }

        [Test]
        public void GrowthCurve_Custom_UsesRegisteredFormula()
        {
            _c.RegisterCustomCurve("triangular", lv => lv * (lv + 1) / 2f);
            _c.SetGrowthCurve(StatType.Attack, new LevelCurve(GrowthCurve.Custom, 0f, 0f, customKey: "triangular"));
            _c.SetLevel(5);
            Assert.AreEqual(15f, _c.GetFinalValue(StatType.Attack), 0.001f);  // 1+2+3+4+5 = 15
        }

        // ────────────────────────────────────────────
        // 스냅샷
        // ────────────────────────────────────────────
        [Test]
        public void Snapshot_Restore_RecoversBase()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            var snap = _c.TakeSnapshot();

            _c.SetBaseValue(StatType.Attack, 200f);
            Assert.AreEqual(200f, _c.GetFinalValue(StatType.Attack));

            _c.RestoreSnapshot(snap);
            Assert.AreEqual(100f, _c.GetFinalValue(StatType.Attack));
        }

        [Test]
        public void Snapshot_Restore_RecoversModifiers()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 50f));
            var snap = _c.TakeSnapshot();

            _c.RemoveModifiersFromSource(null);
            _c.SetBaseValue(StatType.Attack, 0f);
            _c.RestoreSnapshot(snap);

            Assert.AreEqual(150f, _c.GetFinalValue(StatType.Attack));
        }

        // ────────────────────────────────────────────
        // 히스토리
        // ────────────────────────────────────────────
        [Test]
        public void History_RecordsChanges()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 50f));

            var history = _c.GetHistory();
            Assert.GreaterOrEqual(history.Count, 2);
            Assert.AreEqual(StatType.Attack, history[history.Count - 1].Type);
        }

        [Test]
        public void History_ClearHistory_Empties()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.ClearHistory();
            Assert.AreEqual(0, _c.GetHistory().Count);
        }

        [Test]
        public void History_RespectsCapacity()
        {
            _c.HistoryCapacity = 3;
            for (int i = 1; i <= 10; i++)
                _c.SetBaseValue(StatType.Attack, i * 10f);
            Assert.AreEqual(3, _c.GetHistory().Count);
        }

        // ────────────────────────────────────────────
        // 분해
        // ────────────────────────────────────────────
        [Test]
        public void Decomposition_BreaksDownContributions()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 30f, sourceTag: ModifierSource.Equipment, sourceLabel: "Sword"));
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Percent, 0.20f, sourceTag: ModifierSource.Buff, sourceLabel: "Frenzy"));

            var d = _c.GetDecomposition(StatType.Attack);
            Assert.AreEqual(100f, d.BaseValue);
            Assert.AreEqual(30f, d.FlatTotal);
            Assert.AreEqual(0.20f, d.PercentTotal, 0.001f);
            Assert.AreEqual(2, d.Contributions.Count);
        }

        // ────────────────────────────────────────────
        // CurrentHP / Tick
        // ────────────────────────────────────────────
        [Test]
        public void ResetToMax_FillsCurrentHP()
        {
            _c.SetBaseValue(StatType.HP, 200f);
            _c.ResetToMax();
            Assert.AreEqual(200f, _c.CurrentHP);
            Assert.IsTrue(_c.IsAlive);
        }

        [Test]
        public void Kill_ZeroesCurrentHP()
        {
            _c.SetBaseValue(StatType.HP, 200f);
            _c.ResetToMax();
            _c.Kill();
            Assert.AreEqual(0f, _c.CurrentHP);
            Assert.IsFalse(_c.IsAlive);
        }

        [Test]
        public void Revive_RestoresToMax()
        {
            _c.SetBaseValue(StatType.HP, 200f);
            _c.Kill();
            _c.Revive();
            Assert.AreEqual(200f, _c.CurrentHP);
        }

        [Test]
        public void Tick_HPRegen_RecoversCurrentHP()
        {
            _c.SetBaseValue(StatType.HP, 200f);
            _c.SetBaseValue(StatType.HPRegen, 10f);
            _c.SetCurrentHP(50f);

            _c.Tick(1f);
            Assert.AreEqual(60f, _c.CurrentHP, 0.001f);
        }

        [Test]
        public void Tick_HPRegen_DoesNotExceedMax()
        {
            _c.SetBaseValue(StatType.HP, 100f);
            _c.SetBaseValue(StatType.HPRegen, 50f);
            _c.SetCurrentHP(80f);

            _c.Tick(10f);  // 50*10 = 500 더해도
            Assert.AreEqual(100f, _c.CurrentHP);
        }

        [Test]
        public void Tick_DeadUnit_NoRegen()
        {
            _c.SetBaseValue(StatType.HP, 100f);
            _c.SetBaseValue(StatType.HPRegen, 10f);
            _c.SetCurrentHP(0f);  // 죽음

            _c.Tick(5f);
            Assert.AreEqual(0f, _c.CurrentHP);  // 부활 안 함
        }

        [Test]
        public void Tick_TemporaryModifier_ExpiresAfterDuration()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            _c.AddModifier(new StatModifier(StatType.Attack, StatLayer.Flat, 50f, durationSeconds: 2f));
            Assert.AreEqual(150f, _c.GetFinalValue(StatType.Attack));

            _c.Tick(1f);
            Assert.AreEqual(150f, _c.GetFinalValue(StatType.Attack));  // still active

            _c.Tick(1.5f);  // expires
            Assert.AreEqual(100f, _c.GetFinalValue(StatType.Attack));
        }

        // ────────────────────────────────────────────
        // 이벤트
        // ────────────────────────────────────────────
        [Test]
        public void StatChange_PublishesEvent()
        {
            _c.SetBaseValue(StatType.Attack, 100f);
            var events = _eventBus.GetPublished<StatChangedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(StatType.Attack, events[0].Type);
            Assert.AreEqual(0f, events[0].OldValue);
            Assert.AreEqual(100f, events[0].NewValue);
        }
    }
}
