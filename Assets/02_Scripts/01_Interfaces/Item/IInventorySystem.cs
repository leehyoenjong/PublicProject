using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 인벤토리 시스템 계약. 모든 아이템 획득/소비/조회의 단일 진입점.
    /// Convert 치환 판단은 본 시스템이 전담하며 외부(가챠/보상)는 결과를 페이로드에 통합한다.
    /// </summary>
    public interface IInventorySystem : IService
    {
        ItemAddResult AddItem(int mid, int count, object source);
        bool ConsumeByMID(int mid, int count);
        bool ConsumeByInstance(string instanceId, int count);
        int GetCount(int mid);
        IItemInstance GetInstance(string instanceId);
        IReadOnlyList<IItemInstance> GetAll();
        IReadOnlyList<IItemInstance> GetByCategory(ItemCategory category);
        bool SetBound(string instanceId);
        int PurgeExpired();
    }
}

