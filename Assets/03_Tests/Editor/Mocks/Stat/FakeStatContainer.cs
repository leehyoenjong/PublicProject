using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IStatContainer. AddModifier/RemoveModifiersFromSource 호출만 기록.</summary>
    public class FakeStatContainer : IStatContainer
    {
        public List<IStatModifier> Modifiers { get; } = new List<IStatModifier>();
        public int AddModifierCalls { get; private set; }
        public int RemoveFromSourceCalls { get; private set; }
        public object LastRemoveSource { get; private set; }

        public float GetFinalValue(StatType type) => 0f;
        public float GetBaseValue(StatType type) => 0f;
        public void SetBaseValue(StatType type, float value) { }
        public void SetGrowthValue(StatType type, float value) { }

        public void AddModifier(IStatModifier modifier)
        {
            AddModifierCalls++;
            Modifiers.Add(modifier);
        }

        public void RemoveModifier(IStatModifier modifier)
        {
            Modifiers.Remove(modifier);
        }

        public int RemoveModifiersFromSource(object source)
        {
            RemoveFromSourceCalls++;
            LastRemoveSource = source;
            int removed = Modifiers.RemoveAll(m => ReferenceEquals(m.Source, source));
            return removed;
        }

        public IReadOnlyList<IStatModifier> GetModifiers(StatLayer layer) => Modifiers;
        public void RecalculateAll() { }
    }
}
