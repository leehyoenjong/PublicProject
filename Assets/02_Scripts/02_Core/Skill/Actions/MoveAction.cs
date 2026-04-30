using System.Globalization;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// Move — caster 를 이동시키는 선언적 액션. param1=direction("Forward"/"Back"/"Left"/"Right" 또는 "{x,y,z}"), param2=distance(float).
    /// 실제 이동 수행은 SkillMoveRequestedEvent 를 구독하는 캐릭터 컨트롤러가 처리한다 (Physics/Animator 침범 방지).
    /// </summary>
    public class MoveAction : ISkillAction
    {
        public SkillActionType Type => SkillActionType.Move;

        public void Execute(SkillContext context, SkillActionEntry entry)
        {
            if (context == null || entry == null) return;

            Vector3 dir = ResolveDirection(entry.Param1, context);
            float distance = 0f;
            if (!string.IsNullOrEmpty(entry.Param2))
            {
                float.TryParse(entry.Param2, NumberStyles.Float, CultureInfo.InvariantCulture, out distance);
            }

            context.EventBus?.Publish(new SkillMoveRequestedEvent
            {
                SkillId = context.SkillData != null ? context.SkillData.SkillId : null,
                CasterId = context.CasterId,
                Direction = dir,
                Distance = distance,
                Duration = entry.Duration
            });

            Debug.Log($"[스킬액션] 이동: {context.CasterId} 방향={dir} 거리={distance}");
        }

        private static Vector3 ResolveDirection(string raw, SkillContext context)
        {
            if (string.IsNullOrEmpty(raw)) return Vector3.zero;
            if (raw.StartsWith("{")) return SkillActionHelpers.ParseVector3(raw);

            Vector3 facing = context.TargetPosition - context.CasterPosition;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.right;
            facing.Normalize();

            return raw switch
            {
                "Forward" => facing,
                "Back" => -facing,
                "Left" => new Vector3(-facing.y, facing.x, 0f),
                "Right" => new Vector3(facing.y, -facing.x, 0f),
                _ => Vector3.zero
            };
        }
    }

    public struct SkillMoveRequestedEvent
    {
        public string SkillId;
        public string CasterId;
        public Vector3 Direction;
        public float Distance;
        public float Duration;
    }
}
