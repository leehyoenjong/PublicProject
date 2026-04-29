using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 등급 인덱스별 정책. Enhance_등급정책 시트가 parentId=enhance_grade 로 EnhanceData._gradePolicies 에 주입.
    /// gradeIndex 0~4 = Common~Legendary. Legendary 행은 promotion 컬럼 0(미적용).
    /// </summary>
    [Serializable]
    public class GradePolicyEntry
    {
        [SerializeField] private int _gradeIndex;
        [SerializeField] private int _maxLevel;
        [SerializeField] private float _promotionProb;
        [SerializeField] private int _promotionMaxPity;
        [SerializeField] private int _promotionCost;
        [SerializeField] private EnhanceFailPolicy _promotionFailPolicy;

        public int GradeIndex => _gradeIndex;
        public int MaxLevel => _maxLevel;
        public float PromotionProb => _promotionProb;
        public int PromotionMaxPity => _promotionMaxPity;
        public int PromotionCost => _promotionCost;
        public EnhanceFailPolicy PromotionFailPolicy => _promotionFailPolicy;
    }
}
