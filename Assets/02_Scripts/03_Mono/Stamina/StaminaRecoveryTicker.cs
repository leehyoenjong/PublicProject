using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스태미나 자동 회복 — Currency MID 의 보유량을 StageConfig.RecoverSeconds 마다 +1.
    /// 부팅 시 stamina 가 max 미만이면 max 까지 채움 (Save 인프라 통합 후 영속화 트랙에서 제거).
    /// IInventorySystem.AddItem 호출로 ItemAcquiredEvent 자동 발화 → HUD/Save 자동 갱신.
    /// 본 사이클은 stamina 한정 단순 구현 — 일반화는 RecoverableCurrency 도메인 트랙.
    /// </summary>
    public class StaminaRecoveryTicker : MonoBehaviour
    {
        [SerializeField] private int _staminaMid = 40002;

        private IInventorySystem _inventory;
        private StageConfig _stageConfig;
        private float _elapsed;

        private void Start()
        {
            _inventory = ServiceLocator.Has<IInventorySystem>() ? ServiceLocator.Get<IInventorySystem>() : null;
            _stageConfig = ServiceLocator.Has<IStageSystem>() ? ServiceLocator.Get<IStageSystem>().Config : null;

            if (_inventory == null)
            {
                Debug.LogWarning("[스태미나회복] IInventorySystem 미등록 — 자동 회복 비활성", this);
                return;
            }
            if (_stageConfig == null)
            {
                Debug.LogWarning("[스태미나회복] IStageSystem.Config 부재 — 자동 회복 비활성", this);
                return;
            }
            FillToMax();
        }

        private void FillToMax()
        {
            int current = _inventory.GetCount(_staminaMid);
            int max = _stageConfig.MaxStamina;
            if (current < max)
            {
                _inventory.AddItem(_staminaMid, max - current, "boot");
            }
        }

        private void Update()
        {
            if (_inventory == null || _stageConfig == null) return;
            float interval = _stageConfig.StaminaRecoverSeconds;
            if (interval <= 0f) return;

            int max = _stageConfig.MaxStamina;
            int current = _inventory.GetCount(_staminaMid);
            if (current >= max)
            {
                _elapsed = 0f;
                return;
            }

            _elapsed += Time.deltaTime;
            while (_elapsed >= interval && current < max)
            {
                _elapsed -= interval;
                _inventory.AddItem(_staminaMid, 1, "recover");
                current++;
            }
        }
    }
}
