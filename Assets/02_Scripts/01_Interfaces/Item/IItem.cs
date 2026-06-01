using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ItemData(SO) 가 구현하는 공통 계약.
    /// 모든 엔터티(장비/캐릭터/펫/유물/소비/재료/티켓/재화)는 이 계약을 통해 단일 컨테이너로 취급된다.
    /// MID 는 대량 엔터티 성능 예외로 int 를 사용한다.
    /// </summary>
    public interface IItem
    {
        int MID { get; }
        Sprite Icon { get; }
        int DisplayNameKey { get; }
        int DescriptionKey { get; }
        Rarity Rarity { get; }
        ItemCategory Category { get; }
        StackType StackType { get; }
        int MaxStack { get; }
        int ConvertRewardMID { get; }
        int ConvertRewardCount { get; }
        IItemSubtypeInfo SubtypeRef { get; }
    }
}

