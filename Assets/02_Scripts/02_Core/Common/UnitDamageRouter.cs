using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 유닛 HP delta 적용 + 클램프 + 이벤트 발행 + 사망 분기 로직.
    /// MonoBehaviour 분리 — UnitController/Adapter 어디서든 동일 규칙으로 호출 가능, EditMode 테스트 친화.
    /// </summary>
    public static class UnitDamageRouter
    {
        /// <summary>
        /// stats.CurrentHP 에 delta 를 적용하고 UnitHpChangedEvent / UnitDiedEvent 를 발행한다.
        /// 이미 사망 상태(wasAlive=false)면 무시. stats null 도 무시.
        /// </summary>
        /// <returns>적용 후 생존 여부. 호출자는 이 값을 자신의 _isAlive 에 대입.</returns>
        public static bool Apply(
            IStatContainer stats,
            IEventBus eventBus,
            string instanceId,
            string unitId,
            bool wasAlive,
            float delta,
            string source)
        {
            if (stats == null || !wasAlive) return wasAlive;

            float maxHp = stats.GetFinalValue(StatType.HP);
            float oldHp = stats.CurrentHP;
            float newHp = Mathf.Clamp(oldHp + delta, 0f, maxHp);
            stats.SetCurrentHP(newHp);

            Debug.Log($"[유닛] {instanceId} HP {oldHp:F1} → {newHp:F1} ({delta:+0.0;-0.0}) / 최대 {maxHp:F1}");

            eventBus?.Publish(new UnitHpChangedEvent
            {
                InstanceId = instanceId,
                OldHp = oldHp,
                NewHp = newHp,
                Source = source,
            });

            bool isAlive = newHp > 0f;
            if (!isAlive)
            {
                eventBus?.Publish(new UnitDiedEvent
                {
                    InstanceId = instanceId,
                    UnitId = unitId,
                    LastDamageSource = source,
                });
            }
            return isAlive;
        }
    }
}
