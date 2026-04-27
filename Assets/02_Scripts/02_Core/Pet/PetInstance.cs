using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 펫 런타임. 장착 슬롯 인덱스(미장착 = -1)와 스킬 슬롯 배열을 보유한다.
    /// 상태 변경은 PetSystem 경유 권장 (이벤트 발행 일관화).
    /// </summary>
    public class PetInstance : UnitInstance, IPetInstance
    {
        private const int UNEQUIPPED = -1;

        private readonly IPetInfo _info;
        private readonly SkillData[] _equippedSkills;

        public PetInstance(string instanceId, IPetInfo info, IStatContainer stats)
            : base(instanceId, stats)
        {
            _info = info;
            int slotMax = info != null && info.SkillSlotMax > 0 ? info.SkillSlotMax : 0;
            _equippedSkills = new SkillData[slotMax];
            EquippedSlotIndex = UNEQUIPPED;
        }

        public override IUnit Unit => _info;
        public IPetInfo Info => _info;
        public int EquippedSlotIndex { get; private set; }
        public bool IsEquipped => EquippedSlotIndex >= 0;
        public IReadOnlyList<SkillData> EquippedSkills => _equippedSkills;

        public void SetEquippedSlot(int slotIndex) => EquippedSlotIndex = slotIndex;
        public void ClearEquippedSlot() => EquippedSlotIndex = UNEQUIPPED;

        public bool TrySetSkill(int skillSlot, SkillData skill)
        {
            if (_equippedSkills.Length == 0) return false;
            if (skillSlot < 0 || skillSlot >= _equippedSkills.Length) return false;
            _equippedSkills[skillSlot] = skill;
            return true;
        }
    }
}
