using System.Globalization;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// PlayAnimation — param1=animKey(Animator Trigger/State 이름), param2=target(Self/Target, 기본 Self), param3=layer(int, 기본 0).
    /// entry.Duration 은 강제 종료까지 시간(초). 0 이하면 자동 재생 길이에 맡김.
    /// 실제 Animator 접근은 하지 않고 SkillAnimationEvent 발행만 — 캐릭터/몬스터 컨트롤러가 수신해 Animator 재생.
    /// </summary>
    public class PlayAnimationAction : ISkillAction
    {
        public SkillActionType Type => SkillActionType.PlayAnimation;

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            if (context == null || entry == null) return;
            if (context.EventBus == null)
            {
                Debug.LogWarning("[PlayAnimationAction] IEventBus not provided");
                return;
            }

            string animKey = entry.Param1;
            if (string.IsNullOrEmpty(animKey))
            {
                Debug.LogError("[PlayAnimationAction] param1(animKey) is empty");
                return;
            }

            string targetRole = string.IsNullOrEmpty(entry.Param2) ? "Self" : entry.Param2;

            int layer = 0;
            if (!string.IsNullOrEmpty(entry.Param3))
            {
                int.TryParse(entry.Param3, NumberStyles.Integer, CultureInfo.InvariantCulture, out layer);
            }

            context.EventBus.Publish(new SkillAnimationEvent
            {
                SkillId = context.SkillData != null ? context.SkillData.SkillId : null,
                CasterId = context.CasterId,
                TargetId = context.TargetId,
                AnimKey = animKey,
                TargetRole = targetRole,
                Layer = layer,
                Duration = entry.Duration
            });
        }
    }
}
