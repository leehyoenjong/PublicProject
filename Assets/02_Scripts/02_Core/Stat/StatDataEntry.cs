using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 캐릭터/몬스터/펫의 기본 스탯 1행. StatData 자식 시트가 parentId/order 로
    /// 각 SO 의 _baseStats 에 주입한다.
    /// stat 와 customKey 중 하나만 사용 (자명한 stat = enum, 게임 고유 = customKey).
    /// </summary>
    [Serializable]
    public class StatDataEntry
    {
        [SerializeField] private StatType _stat;
        [SerializeField] private string _customKey;
        [SerializeField, SheetAlias("base")] private float _baseValue;
        [SerializeField] private GrowthCurve _curve;
        [SerializeField] private float _growth;

        public StatType Stat => _stat;
        public string CustomKey => _customKey;
        public float Base => _baseValue;
        public GrowthCurve Curve => _curve;
        public float Growth => _growth;

        public LevelCurve ToLevelCurve()
        {
            return new LevelCurve(_curve, _baseValue, _growth, _customKey);
        }

        public StatDataEntry() { }

        public StatDataEntry(StatType stat, string customKey, float baseValue, GrowthCurve curve, float growth)
        {
            _stat = stat;
            _customKey = customKey;
            _baseValue = baseValue;
            _curve = curve;
            _growth = growth;
        }
    }
}
