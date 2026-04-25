using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 캐릭터 런타임 상태. 생성/변경은 CharacterSystem 경유 권장.
    /// </summary>
    public class CharacterInstance : UnitInstance, ICharacterInstance
    {
        private readonly ICharacterInfo _info;
        private readonly List<SkillData> _equippedSkills = new();

        public CharacterInstance(string instanceId, ICharacterInfo info, IStatContainer stats)
            : base(instanceId, stats)
        {
            _info = info;
        }

        public override IUnit Unit => _info;
        public ICharacterInfo Info => _info;
        public int Awakening { get; private set; }
        public Rarity Rarity { get; private set; } = Rarity.Common;
        public IReadOnlyList<SkillData> EquippedSkills => _equippedSkills;

        public void SetRarity(Rarity rarity) => Rarity = rarity;

        public void SetAwakening(int value) => Awakening = value < 0 ? 0 : value;

        public void SetEquippedSkills(IEnumerable<SkillData> skills)
        {
            _equippedSkills.Clear();
            if (skills == null) return;
            foreach (var s in skills) if (s != null) _equippedSkills.Add(s);
        }

        public bool SetEquippedSkill(int slot, SkillData skill)
        {
            if (slot < 0) return false;
            while (_equippedSkills.Count <= slot) _equippedSkills.Add(null);
            _equippedSkills[slot] = skill;
            return true;
        }
    }
}
