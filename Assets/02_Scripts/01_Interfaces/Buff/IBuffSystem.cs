using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 버프/디버프 관리 서비스 인터페이스
    /// </summary>
    public interface IBuffSystem : IService
    {
        BuffResult AddBuff(string targetId, BuffData buffData, string sourceId);
        bool RemoveBuff(string targetId, string buffId);
        int RemoveAllBuffs(string targetId, BuffCategory? category = null);
        IReadOnlyList<IBuffInstance> GetBuffs(string targetId);
        bool HasBuff(string targetId, string buffId);
        int GetStackCount(string targetId, string buffId);
        void AddImmunity(string targetId, string buffIdOrCategory);
        void RemoveImmunity(string targetId, string buffIdOrCategory);
        void Tick(float deltaTime);
        void ProcessTurn(string targetId);
    }
}
