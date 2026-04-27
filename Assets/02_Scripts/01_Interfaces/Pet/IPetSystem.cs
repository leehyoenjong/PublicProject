using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 펫 도메인 진입점. PetInfo 룩업, 인스턴스 관리, 장착/해제, 스킬 슬롯 변경, 획득 처리.
    /// 다중 장착 슬롯은 프로젝트별 정원(maxSlots)을 주입받아 인덱스 기반으로 관리한다.
    /// AI/BT 결합·따라다니기 Mono 동작은 후속 Phase 책임.
    /// </summary>
    public interface IPetSystem : IService
    {
        IPetInfo GetInfo(string petMID);
        IPetInfo GetInfoByItemMID(int itemMID);
        IPetInstance Get(string instanceId);
        IReadOnlyCollection<IPetInstance> All { get; }

        int MaxSlots { get; }
        IReadOnlyList<IPetInstance> EquippedSlots { get; }
        IPetInstance GetEquipped(int slotIndex);

        IPetInstance Acquire(string petMID, string instanceId, IStatContainer stats);
        bool Release(string instanceId);

        bool Equip(string instanceId, int slotIndex);
        bool Unequip(int slotIndex);
        bool UnequipInstance(string instanceId);

        bool SetEquippedSkill(string instanceId, int skillSlot, SkillData skill);
    }
}
