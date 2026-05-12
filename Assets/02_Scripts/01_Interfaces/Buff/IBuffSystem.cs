using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 버프/디버프 관리 서비스 인터페이스
    /// </summary>
    public interface IBuffSystem : IService
    {
        BuffResult AddBuff(string targetId, BuffData buffData, string sourceId, string sourceSkillId = "");
        bool RemoveBuff(string targetId, string buffId, string sourceSkillId = null);
        int RemoveAllBuffs(string targetId, BuffCategory? category = null);
        IReadOnlyList<IBuffInstance> GetBuffs(string targetId);
        bool HasBuff(string targetId, string buffId, string sourceSkillId = null);
        int GetStackCount(string targetId, string buffId, string sourceSkillId = null);
        void AddImmunity(string targetId, string buffIdOrCategory);
        void RemoveImmunity(string targetId, string buffIdOrCategory);
        void Tick(float deltaTime);
        void ProcessTurn(string targetId);
    }
}
