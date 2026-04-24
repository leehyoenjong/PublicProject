using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 장비 착용/해제 및 세트효과 집계 계약.
    /// 슬롯 식별자는 프로젝트별 enum/string 차이를 수용하기 위해 string slotId 로 추상화한다.
    /// 세트효과 집계는 장착 상태 변경 시마다 갱신되어 스탯 시스템에 Modifier 로 반영된다.
    /// </summary>
    public interface IEquipmentSystem : IService
    {
        bool Equip(string ownerId, string slotId, string itemInstanceId);
        bool Unequip(string ownerId, string slotId);
        IItemInstance GetEquipped(string ownerId, string slotId);
        IReadOnlyDictionary<string, IItemInstance> GetAllEquipped(string ownerId);
        IReadOnlyDictionary<int, int> GetSetPieceCounts(string ownerId);
    }
}

