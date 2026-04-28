using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 시스템 게임별 설정. 스태미나 최대치/회복 속도 등.
    /// </summary>
    [CreateAssetMenu(fileName = "StageConfig", menuName = "PublicFramework/Stage/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        [Header("스태미나")]
        [SerializeField] private int _maxStamina = 120;
        [SerializeField] private float _staminaRecoverSeconds = 360f;

        [Header("자동전투")]
        [SerializeField] private bool _autoBattleAllowedByDefault = false;

        public int MaxStamina => _maxStamina;
        public float StaminaRecoverSeconds => _staminaRecoverSeconds;
        public bool AutoBattleAllowedByDefault => _autoBattleAllowedByDefault;
    }
}
