using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 초월 단계별 비용. Enhance_초월 시트가 parentId=enhance_transcend 로 EnhanceData._transcendSteps 에 주입.
    /// </summary>
    [Serializable]
    public class TranscendStepEntry
    {
        [SerializeField] private int _stepIndex;
        [SerializeField] private int _cost;
        [SerializeField] private int _requiredSameItemCount;

        public int StepIndex => _stepIndex;
        public int Cost => _cost;
        public int RequiredSameItemCount => _requiredSameItemCount;
    }
}
