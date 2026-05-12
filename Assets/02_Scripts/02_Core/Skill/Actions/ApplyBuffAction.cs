using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ApplyBuff — param1=BuffId(string, 시트의 BuffData.MID), param2=target(Self/Target).
    /// IBuffSystem 이 필요. BuffData 는 SkillSystem 또는 외부에서 사전 로드되어 있어야 한다.
    /// </summary>
    public class ApplyBuffAction : ISkillAction
    {
        public SkillActionType Type => SkillActionType.ApplyBuff;

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            if (context == null || entry == null) return;
            if (context.BuffSystem == null)
            {
                Debug.LogError("[스킬액션] IBuffSystem이 제공되지 않음.");
                return;
            }

            string buffId = entry.Param1;
            if (string.IsNullOrEmpty(buffId))
            {
                Debug.LogError("[스킬액션] param1(buffId)가 비어 있음.");
                return;
            }

            string targetRole = string.IsNullOrEmpty(entry.Param2) ? "Target" : entry.Param2;
            string targetId = ResolveTarget(context, targetRole);
            if (string.IsNullOrEmpty(targetId))
            {
                Debug.LogWarning($"[스킬액션] 타겟을 찾을 수 없음: '{targetRole}'");
                return;
            }

            BuffData data = SkillActionHelpers.FindBuffData(buffId);
            if (data == null)
            {
                Debug.LogError($"[스킬액션] BuffData를 찾을 수 없음: '{buffId}'");
                return;
            }

            string sourceSkillId = context.SkillData != null ? context.SkillData.SkillId : string.Empty;
            context.BuffSystem.AddBuff(targetId, data, context.CasterId, sourceSkillId);
        }

        private static string ResolveTarget(SkillContext context, string role)
        {
            return role switch
            {
                "Self" => context.CasterId,
                "Caster" => context.CasterId,
                "Target" => context.TargetId,
                _ => context.TargetId
            };
        }
    }
}
