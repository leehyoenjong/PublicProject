using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 한 단위(펫/몬스터/캐릭터)의 기본 스탯 묶음을 단일 자산으로 보유하는 SO.
    /// 자식 시트(StatData) 가 parentId 그룹별로 _entries 를 채운다.
    /// 같은 묶음을 여러 부모 SO 가 참조해도 일관성 보장 (Skill 도메인의 SkillData 패턴과 동일).
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatGroupData", menuName = "PublicFramework/Stat/StatGroupData")]
    public class StatGroupData : ScriptableObject
    {
        [SerializeField, SheetAlias("MID")] private string _statGroupId;
        [SerializeField] private StatDataEntry[] _entries;

        public string StatGroupId => _statGroupId;
        public IReadOnlyList<StatDataEntry> Entries => _entries ?? System.Array.Empty<StatDataEntry>();
    }
}
