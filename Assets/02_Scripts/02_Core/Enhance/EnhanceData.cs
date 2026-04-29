using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 강화 정책 1행. Enhance 시트의 EnhanceType 1종 = SO 1개.
    /// 자식 시트(Enhance_등급정책 / 초월 / 각성옵션 / 진화)가 parentId=MID 로 자식 배열에 주입.
    /// 스칼라 공식 상수(_levelCostBase 등)는 시트 미연동 — Inspector 기본값 사용.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnhanceData", menuName = "PublicFramework/Enhance/EnhanceData")]
    public class EnhanceData : ScriptableObject
    {
        [Header("기본")]
        [SerializeField, SheetAlias("MID")] private string _mid;
        [SerializeField] private EnhanceType _enhanceType;

        [Header("정책 (Phase 2-B)")]
        [SerializeField] private int _protectionTicketCost;
        [SerializeField] private float _blessingBoost;
        [SerializeField] private float _consecutiveBonusBase;

        [Header("자식 (시트)")]
        [SerializeField] private GradePolicyEntry[] _gradePolicies;
        [SerializeField] private TranscendStepEntry[] _transcendSteps;
        [SerializeField] private AwakeningOptionEntry[] _awakeningOptions;
        [SerializeField] private EvolutionStageEntry[] _evolutionStages;

        [Header("스칼라 (시트 미연동 — Inspector 기본값)")]
        [SerializeField] private int _levelCostBase = 100;
        [SerializeField] private float _levelCostMultiplier = 1.2f;
        [SerializeField] private float _gradeCostMultiplier = 0.5f;
        [SerializeField] private int _stoneCostBase = 1;
        [SerializeField] private float _stoneCostMultiplier = 0.2f;
        [SerializeField] private int _awakeningCostBase = 20;

        public string Mid => _mid;
        public EnhanceType EnhanceType => _enhanceType;
        public int ProtectionTicketCost => _protectionTicketCost;
        public float BlessingBoost => _blessingBoost;
        public float ConsecutiveBonusBase => _consecutiveBonusBase;

        public IReadOnlyList<GradePolicyEntry> GradePolicies => _gradePolicies ?? System.Array.Empty<GradePolicyEntry>();
        public IReadOnlyList<TranscendStepEntry> TranscendSteps => _transcendSteps ?? System.Array.Empty<TranscendStepEntry>();
        public IReadOnlyList<AwakeningOptionEntry> AwakeningOptions => _awakeningOptions ?? System.Array.Empty<AwakeningOptionEntry>();
        public IReadOnlyList<EvolutionStageEntry> EvolutionStages => _evolutionStages ?? System.Array.Empty<EvolutionStageEntry>();

        public int LevelCostBase => _levelCostBase;
        public float LevelCostMultiplier => _levelCostMultiplier;
        public float GradeCostMultiplier => _gradeCostMultiplier;
        public int StoneCostBase => _stoneCostBase;
        public float StoneCostMultiplier => _stoneCostMultiplier;
        public int AwakeningCostBase => _awakeningCostBase;

        public GradePolicyEntry FindGradePolicy(int gradeIndex)
        {
            if (_gradePolicies == null) return null;
            for (int i = 0; i < _gradePolicies.Length; i++)
            {
                if (_gradePolicies[i] != null && _gradePolicies[i].GradeIndex == gradeIndex) return _gradePolicies[i];
            }
            return null;
        }

        public TranscendStepEntry FindTranscendStep(int stepIndex)
        {
            if (_transcendSteps == null) return null;
            for (int i = 0; i < _transcendSteps.Length; i++)
            {
                if (_transcendSteps[i] != null && _transcendSteps[i].StepIndex == stepIndex) return _transcendSteps[i];
            }
            return null;
        }

        public EvolutionStageEntry FindEvolutionStage(int stageIndex)
        {
            if (_evolutionStages == null) return null;
            for (int i = 0; i < _evolutionStages.Length; i++)
            {
                if (_evolutionStages[i] != null && _evolutionStages[i].StageIndex == stageIndex) return _evolutionStages[i];
            }
            return null;
        }
    }
}
