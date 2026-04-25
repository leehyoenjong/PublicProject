using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 캐릭터 데이터·인스턴스·보조 계산(슬롯/대사/프로필) 진입점.
    /// 인벤토리가 아이템 수집을 책임지고, 여기는 CharacterInfo 해석과 런타임 CharacterInstance 를 담당.
    /// </summary>
    public interface ICharacterSystem : IService
    {
        ICharacterInfo GetInfo(int itemMID);
        ICharacterInstance Get(string instanceId);
        IReadOnlyCollection<ICharacterInstance> All { get; }

        ICharacterInstance Create(int itemMID, string instanceId, IStatContainer stats, int level = 1, Rarity rarity = Rarity.Common);
        bool Remove(string instanceId);

        bool SetLevel(string instanceId, int level);
        bool SetAwakening(string instanceId, int awakening);
        bool SetEquippedSkill(string instanceId, int slot, SkillData skill);

        int CalculateSlotCount(ICharacterInstance instance);
        int GetDialogueLine(ICharacterInstance instance, DialogueEvent ev);
        string GetProfileValue(ICharacterInstance instance, string key);
        CharacterProfileEntry GetProfileEntry(ICharacterInstance instance, string key);
    }
}
