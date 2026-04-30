using System.Globalization;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// Heal — param1=amount(float), param2=scaleStat(StatType 이름, 선택).
    /// SkillHealEvent 발행. 실제 HP 증가는 수신 쪽 책임.
    /// </summary>
    public class HealAction : ISkillAction
    {
        public SkillActionType Type => SkillActionType.Heal;

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            if (context == null || entry == null) return;
            if (string.IsNullOrEmpty(context.TargetId))
            {
                Debug.LogWarning("[스킬액션] targetId가 비어 있음.");
                return;
            }

            if (!float.TryParse(entry.Param1, NumberStyles.Float, CultureInfo.InvariantCulture, out float baseAmount))
            {
                Debug.LogError($"[스킬액션] param1 파싱 실패: '{entry.Param1}'");
                return;
            }

            float finalAmount = baseAmount * context.PowerMultiplier;

            if (!string.IsNullOrEmpty(entry.Param2) && context.StatSystem != null)
            {
                if (System.Enum.TryParse(entry.Param2, true, out StatType scaleStat))
                {
                    IStatContainer container = context.StatSystem.GetContainer(context.CasterId);
                    if (container != null)
                    {
                        float statValue = container.GetFinalValue(scaleStat);
                        if (statValue > 0f) finalAmount *= statValue * 0.01f;
                    }
                }
            }

            context.EventBus?.Publish(new SkillHealEvent
            {
                SkillId = context.SkillData != null ? context.SkillData.SkillId : null,
                CasterId = context.CasterId,
                TargetId = context.TargetId,
                Amount = finalAmount
            });

            Debug.Log($"[스킬액션] 힐: {context.CasterId} → {context.TargetId} : +{finalAmount}");
        }
    }
}
