using System.Globalization;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// DealDamage — param1=amount(float), param2=element(free-form string), param3=scaleStat(StatType 이름, 선택).
    /// scaleStat 이 있고 StatSystem 이 주입되면 `amount × caster.GetFinalValue(scaleStat) × powerMult` 로 계산.
    /// 실제 HP 차감은 이 액션 책임이 아니고 SkillDamageEvent 를 발행한다. 수신 쪽(적 AI/StatSystem 확장)이 처리.
    /// </summary>
    public class DealDamageAction : ISkillAction
    {
        public SkillActionType Type => SkillActionType.DealDamage;

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            if (context == null || entry == null) return;
            if (string.IsNullOrEmpty(context.TargetId))
            {
                Debug.LogWarning("[DealDamageAction] targetId is empty");
                return;
            }

            if (!float.TryParse(entry.Param1, NumberStyles.Float, CultureInfo.InvariantCulture, out float baseAmount))
            {
                Debug.LogError($"[DealDamageAction] param1 parse failed: '{entry.Param1}'");
                return;
            }

            float finalAmount = baseAmount * context.PowerMultiplier;

            if (!string.IsNullOrEmpty(entry.Param3) && context.StatSystem != null)
            {
                if (System.Enum.TryParse(entry.Param3, true, out StatType scaleStat))
                {
                    IStatContainer container = context.StatSystem.GetContainer(context.CasterId);
                    if (container != null)
                    {
                        float statValue = container.GetFinalValue(scaleStat);
                        if (statValue > 0f) finalAmount *= statValue * 0.01f;
                    }
                }
            }

            context.EventBus?.Publish(new SkillDamageEvent
            {
                SkillId = context.SkillData != null ? context.SkillData.SkillId : null,
                CasterId = context.CasterId,
                TargetId = context.TargetId,
                Amount = finalAmount,
                Element = entry.Param2
            });

            Debug.Log($"[데미지] {context.CasterId} → {context.TargetId} : {finalAmount:F1} ({entry.Param2})");
        }
    }
}
