using UnityEngine;

namespace PublicFramework
{
    /// <summary>아무 것도 안 하고 즉시 Success.</summary>
    public class IdleAction : IBehaviorAction
    {
        public string ActionKey => "Idle";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            return BehaviorNodeStatus.Success;
        }
    }

    /// <summary>param1 초만큼 대기. Blackboard 의 "wait_end_{ActionKey}" 키로 종료시각 추적.</summary>
    public class WaitAction : IBehaviorAction
    {
        public string ActionKey => "Wait";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (!float.TryParse(p1, out float seconds) || seconds <= 0f) return BehaviorNodeStatus.Success;

            string key = "wait_end";
            float end = c.GetBlackboard<float>(key, 0f);
            if (end <= 0f)
            {
                c.SetBlackboard(key, c.NowSeconds + seconds);
                return BehaviorNodeStatus.Running;
            }
            if (c.NowSeconds >= end)
            {
                c.SetBlackboard(key, 0f);
                return BehaviorNodeStatus.Success;
            }
            return BehaviorNodeStatus.Running;
        }
    }

    /// <summary>
    /// TargetPosition 을 향해 Self 를 이동시킨다(2D, z 무시).
    /// 속도 = MoveSpeed 스탯 × param1(배수, 생략 시 1.0). 스탯이 0/없으면 DEFAULT_MOVE_SPEED.
    /// param2 = 정지거리(생략 시 0) — 타깃과 이 거리 이내면 더 안 다가가고 Success(근접 몬스터가 겹치지 않게).
    /// 이동 중엔 Running, 정지거리 도달 시 Success, Self/Target 없으면 Failure.
    /// 실제 위치는 Core(MonsterInstance.Position)에서 갱신하고 외부 어댑터가 transform 에 반영한다.
    /// </summary>
    public class MoveToTargetAction : IBehaviorAction
    {
        private const float DEFAULT_MOVE_SPEED = 1.5f;
        private const float ARRIVE_EPSILON = 0.01f;

        public string ActionKey => "MoveToTarget";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (c.Self == null || c.Target == null) return BehaviorNodeStatus.Failure;

            float baseSpeed = c.SelfStats != null ? c.SelfStats.GetFinalValue(StatType.MoveSpeed) : 0f;
            if (baseSpeed <= 0f) baseSpeed = DEFAULT_MOVE_SPEED;
            float mult = (float.TryParse(p1, out float m) && m > 0f) ? m : 1f;
            float speed = baseSpeed * mult;
            float stopDistance = (float.TryParse(p2, out float sd) && sd > 0f) ? sd : 0f;

            Vector3 cur = c.Self.Position;
            Vector3 to = c.TargetPosition - cur;
            to.z = 0f;
            float dist = to.magnitude;

            float remaining = dist - stopDistance;
            if (remaining <= ARRIVE_EPSILON) return BehaviorNodeStatus.Success; // 정지거리 이내 — 도착

            float step = speed * c.DeltaTime;
            Vector3 dir = to.normalized;
            Vector3 next = step >= remaining ? cur + dir * remaining : cur + dir * step;
            c.Self.SetPosition(next);
            return BehaviorNodeStatus.Running;
        }
    }

    /// <summary>
    /// Self 가 param1 (skillId) 시전. ISkillSystem 주입 필수.
    /// </summary>
    public class CastSkillAction : IBehaviorAction
    {
        private readonly ISkillSystem _skillSystem;

        public CastSkillAction(ISkillSystem skillSystem)
        {
            _skillSystem = skillSystem;
        }

        public string ActionKey => "CastSkill";

        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (string.IsNullOrEmpty(p1) || c.Self == null) return BehaviorNodeStatus.Failure;
            if (_skillSystem == null) return BehaviorNodeStatus.Failure;

            string casterId = c.Self.InstanceId;
            // 런타임 InstanceId 우선: UnitController.OnSkillDamage 는 ev.TargetId 를 자신의 InstanceId 로 필터한다.
            // IUnit.UnitId(카탈로그 MID)를 넘기면 영원히 불일치 → TargetInstanceId 를 우선 사용(없으면 fallback).
            string targetId = !string.IsNullOrEmpty(c.TargetInstanceId) ? c.TargetInstanceId : c.Target?.UnitId;
            bool ok = _skillSystem.Cast(p1, casterId, targetId);
            return ok ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
        }
    }
}
