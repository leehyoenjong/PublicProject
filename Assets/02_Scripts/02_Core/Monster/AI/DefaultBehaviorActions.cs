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
    /// 타깃 위치로 이동 요청을 발생. param1 = speed (생략 시 1.0).
    /// 실제 이동은 외부 컨트롤러가 SkillMoveRequestedEvent 와 유사한 패턴으로 처리.
    /// 본 액션은 BehaviorContext.TargetPosition 을 갱신하고 Success 반환 (Running 모드는 외부 보고로 처리).
    /// </summary>
    public class MoveToTargetAction : IBehaviorAction
    {
        public string ActionKey => "MoveToTarget";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (c.Target == null) return BehaviorNodeStatus.Failure;
            return BehaviorNodeStatus.Success;
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
