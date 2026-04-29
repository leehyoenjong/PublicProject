using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 진화 단계별 비용/게이트. Enhance_진화 시트가 parentId=enhance_evolution 로 EnhanceData._evolutionStages 에 주입.
    /// </summary>
    [Serializable]
    public class EvolutionStageEntry
    {
        [SerializeField] private int _stageIndex;
        [SerializeField] private int _cost;
        [SerializeField] private EquipmentGrade _requiredGrade;
        [SerializeField] private int _requiredTranscendStep;
        [SerializeField] private string _materialMID;

        public int StageIndex => _stageIndex;
        public int Cost => _cost;
        public EquipmentGrade RequiredGrade => _requiredGrade;
        public int RequiredTranscendStep => _requiredTranscendStep;
        public string MaterialMID => _materialMID;
    }
}
