using System;
using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IStatContainer. AddModifier/Remove/RecalculateAll 호출만 기록.</summary>
    public class FakeStatContainer : IStatContainer
    {
        public List<IStatModifier> Modifiers { get; } = new();
        public int AddModifierCalls { get; private set; }
        public int RemoveFromSourceCalls { get; private set; }
        public object LastRemoveSource { get; private set; }

        public string OwnerId { get; set; } = "fake";
        public int Level { get; private set; } = 1;
        public float CurrentHP { get; private set; }
        public float CurrentMP { get; private set; }
        public bool IsAlive => CurrentHP > 0f;
        public int HistoryCapacity { get; set; } = 100;
        public IReadOnlyCollection<string> CustomKeys { get; } = Array.Empty<string>();
        public IReadOnlyList<IStatModifier> AllModifiers => Modifiers;
        public float MaxMP => 0f;

        public float GetFinalValue(StatType type) => 0f;
        public float GetBaseValue(StatType type) => 0f;
        public void SetBaseValue(StatType type, float value) { }
        public void SetGrowthCurve(StatType type, LevelCurve curve) { }
        public void RegisterCustomCurve(string key, Func<int, float> formula) { }

        public float GetFinalValue(string customKey) => 0f;
        public float GetBaseValue(string customKey) => 0f;
        public void SetBaseValue(string customKey, float value) { }
        public void SetGrowthCurve(string customKey, LevelCurve curve) { }

        public void AddModifier(IStatModifier modifier)
        {
            AddModifierCalls++;
            Modifiers.Add(modifier);
        }

        public bool RemoveModifier(IStatModifier modifier) => Modifiers.Remove(modifier);

        public int RemoveModifiersFromSource(object source)
        {
            RemoveFromSourceCalls++;
            LastRemoveSource = source;
            return Modifiers.RemoveAll(m => ReferenceEquals(m.Source, source));
        }

        public IReadOnlyList<IStatModifier> GetModifiers(StatLayer layer) => Modifiers;

        public void SetLevel(int level) { Level = level < 1 ? 1 : level; }

        public void SetCurrentHP(float hp) => CurrentHP = hp;
        public void SetCurrentMP(float mp) => CurrentMP = mp;
        public void ResetToMax() { }
        public void Kill() => CurrentHP = 0f;
        public void Revive() { CurrentHP = 1f; }

        public StatDecomposition GetDecomposition(StatType type) => default;
        public StatDecomposition GetDecomposition(string customKey) => default;

        public IStatSnapshot TakeSnapshot() => null;
        public void RestoreSnapshot(IStatSnapshot snapshot) { }

        public IReadOnlyList<StatHistoryEntry> GetHistory(int? limit = null) => Array.Empty<StatHistoryEntry>();
        public void ClearHistory() { }

        public void Tick(float deltaTime) { }
        public void RecalculateAll() { }
    }
}
