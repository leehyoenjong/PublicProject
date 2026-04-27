using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>펫 획득. onAcquireEvents 트리거 직후 발행.</summary>
    public struct PetAcquiredEvent
    {
        public string PetMID;
        public string InstanceId;
        public IReadOnlyList<string> TriggeredHookIds;
    }

    /// <summary>펫 해제(소유에서 제거).</summary>
    public struct PetReleasedEvent
    {
        public string PetMID;
        public string InstanceId;
    }

    /// <summary>펫 장착. onEquipEvents 트리거 결과 포함.</summary>
    public struct PetEquippedEvent
    {
        public string PetMID;
        public string InstanceId;
        public int SlotIndex;
        public IReadOnlyList<string> TriggeredHookIds;
    }

    /// <summary>펫 해제(슬롯 비우기). onUnequipEvents 트리거 결과 포함.</summary>
    public struct PetUnequippedEvent
    {
        public string PetMID;
        public string InstanceId;
        public int SlotIndex;
        public IReadOnlyList<string> TriggeredHookIds;
    }

    /// <summary>펫 스킬 슬롯 변경.</summary>
    public struct PetSkillChangedEvent
    {
        public string InstanceId;
        public int SkillSlot;
        public string SkillMID;
    }

    /// <summary>훅(EventId) 발화. EventSystem 핸들러 추적용.</summary>
    public struct PetHookTriggeredEvent
    {
        public string EventId;
        public PetEventKind Kind;
        public string PetMID;
        public string InstanceId;
    }

    /// <summary>펫 이벤트 트리거 시점.</summary>
    public enum PetEventKind
    {
        Acquire,
        Equip,
        Unequip
    }
}
