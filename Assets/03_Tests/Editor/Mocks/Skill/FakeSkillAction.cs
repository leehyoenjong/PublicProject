using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 ISkillAction. Execute 호출과 받은 Context/Entry 를 기록.</summary>
    public class FakeSkillAction : ISkillAction
    {
        private readonly SkillActionType _type;
        public SkillActionType Type => _type;
        public int ExecuteCalls { get; private set; }
        public List<SkillContext> Contexts { get; } = new List<SkillContext>();
        public List<SkillActionEntry> Entries { get; } = new List<SkillActionEntry>();
        public bool ThrowOnExecute { get; set; }

        public SkillContext LastContext => Contexts.Count > 0 ? Contexts[Contexts.Count - 1] : null;
        public SkillActionEntry LastEntry => Entries.Count > 0 ? Entries[Entries.Count - 1] : null;

        public FakeSkillAction(SkillActionType type) { _type = type; }

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            ExecuteCalls++;
            Contexts.Add(context);
            Entries.Add(entry);
            if (ThrowOnExecute) throw new System.Exception("FakeSkillAction.ThrowOnExecute");
        }
    }
}
