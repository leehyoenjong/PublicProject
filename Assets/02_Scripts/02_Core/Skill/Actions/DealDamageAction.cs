using System.Globalization;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// DealDamage — param1=amount(float), param2=element(free-form string), param3=scaleStat(StatType 이름, 선택).
    /// scaleStat 이 있고 StatSystem 이 주입되면 `amount × caster.GetFinalValue(scaleStat) × powerMult` 로 계산.
    /// 발행 전 타겟 Defense 로 방어 경감(DamageCalculator)을 적용한다. 크리티컬/회피/속성저항은 의도적 빈칸(파생 확장).
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
                Debug.LogWarning("[데미지] 타겟 ID가 비어있음");
                return;
            }

            if (!float.TryParse(entry.Param1, NumberStyles.Float, CultureInfo.InvariantCulture, out float baseAmount))
            {
                Debug.LogError($"[데미지] param1 파싱 실패: '{entry.Param1}'");
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

            // 크리티컬: 프레임워크 기본은 미적용(의도적 빈칸). 파생은 여기서 caster 의 CritRate 확률 판정 →
            // 성공 시 finalAmount *= CritDamage, DamageType.Critical 표기. 게임마다 규칙이 달라 비워 둔다.

            // 방어 경감: 타겟의 Defense 로 감쇠(DamageCalculator). true damage 가 필요하면 파생이 별도 액션/플래그로 분기.
            if (context.StatSystem != null)
            {
                IStatContainer targetContainer = context.StatSystem.GetContainer(context.TargetId);
                if (targetContainer != null)
                {
                    float defense = targetContainer.GetFinalValue(StatType.Defense);
                    finalAmount = DamageCalculator.Mitigate(finalAmount, defense);
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
