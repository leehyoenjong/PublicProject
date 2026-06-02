using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// AI 프리셋(Aggressive/Ranged/Patrol/Boss)을 표현하기 위한 확장 BT 노드 모음.
    /// 기존 Default 어휘(Move/Cast/HpBelow/TargetInRange)에 더해 거리유지·정찰·군집분리를 제공한다.
    /// 모두 IBehaviorAction 계약을 따르고 BehaviorActionRegistry.CreateDefault 에서 등록된다.
    /// </summary>

    /// <summary>
    /// 원거리 유닛용 거리 유지(kite). Target 과의 거리가 param1(유지거리)보다 가까우면 반대 방향으로 후퇴.
    /// param2 = 속도 배수(MoveSpeed 스탯 × 배수, 생략 시 1.0).
    /// 유지거리 이상이면 Success(이미 적정 거리), 후퇴 중이면 Running, Self/Target 없으면 Failure.
    /// </summary>
    public class KeepDistanceAction : IBehaviorAction
    {
        private const float DEFAULT_MOVE_SPEED = 1.5f;

        public string ActionKey => "KeepDistance";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (c.Self == null || c.Target == null) return BehaviorNodeStatus.Failure;
            if (!float.TryParse(p1, out float keep) || keep <= 0f) return BehaviorNodeStatus.Failure;

            Vector3 cur = c.Self.Position;
            Vector3 to = c.TargetPosition - cur;
            to.z = 0f;
            float dist = to.magnitude;
            if (dist >= keep) return BehaviorNodeStatus.Success; // 충분히 멈 — 유지 OK

            float baseSpeed = c.SelfStats != null ? c.SelfStats.GetFinalValue(StatType.MoveSpeed) : 0f;
            if (baseSpeed <= 0f) baseSpeed = DEFAULT_MOVE_SPEED;
            float mult = (float.TryParse(p2, out float m) && m > 0f) ? m : 1f;
            float step = baseSpeed * mult * c.DeltaTime;

            Vector3 away = dist > 0.0001f ? -to.normalized : Vector3.right;
            c.Self.SetPosition(cur + away * step);
            return BehaviorNodeStatus.Running;
        }
    }

    /// <summary>
    /// 정찰: 최초 위치(원점)를 중심으로 param1(반경) 원주를 따라 배회한다(Target 추격이 아닌 무관심 상태).
    /// param2 = 속도 배수(생략 1.0). 원점·각속도는 결정론적이라 테스트 가능하며 Random 의존이 없다.
    /// Blackboard "patrol_origin" 에 원점 저장. 배회는 끝나지 않으므로 항상 Running.
    /// </summary>
    public class PatrolAction : IBehaviorAction
    {
        private const float DEFAULT_MOVE_SPEED = 1.5f;
        private const float DEFAULT_RADIUS = 2f;

        public string ActionKey => "Patrol";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (c.Self == null) return BehaviorNodeStatus.Failure;
            float radius = (float.TryParse(p1, out float r) && r > 0f) ? r : DEFAULT_RADIUS;
            float mult = (float.TryParse(p2, out float m) && m > 0f) ? m : 1f;

            Vector3 cur = c.Self.Position;
            if (!c.GetBlackboard<bool>("patrol_init", false))
            {
                c.SetBlackboard("patrol_origin", cur);
                c.SetBlackboard("patrol_init", true);
            }
            Vector3 origin = c.GetBlackboard<Vector3>("patrol_origin", cur);

            float baseSpeed = c.SelfStats != null ? c.SelfStats.GetFinalValue(StatType.MoveSpeed) : 0f;
            if (baseSpeed <= 0f) baseSpeed = DEFAULT_MOVE_SPEED;
            float speed = baseSpeed * mult;

            // 목표점이 원점 주위 원주를 따라 돈다(각속도 = 속도/반경). Self 가 그 점을 추적해 배회.
            float omega = radius > 0.01f ? speed / radius : speed;
            float angle = c.NowSeconds * omega;
            Vector3 dest = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

            Vector3 to = dest - cur;
            to.z = 0f;
            float step = speed * c.DeltaTime;
            Vector3 next = to.magnitude <= step ? dest : cur + to.normalized * step;
            c.Self.SetPosition(next);
            return BehaviorNodeStatus.Running;
        }
    }

    /// <summary>
    /// 군집 분리(anti-stack): 가까운 동족(Neighbors)으로부터 밀어내는 분리 이동.
    /// param1 = 분리 반경(이 거리 내 동족만 회피, 생략 시 0.5),
    /// param2 = 속도 배수(MoveSpeed 스탯 × 배수, 생략 시 0.5).
    /// 회피 대상이 없으면 Success(겹침 없음), 있으면 분리 방향으로 한 스텝 이동 후 Running.
    /// Neighbors 미주입(MonsterSystem 외 사용)이면 Success(no-op) — 단일 Self/Target 액션엔 영향 없음.
    /// </summary>
    public class AvoidCrowdingAction : IBehaviorAction
    {
        private const float DEFAULT_RADIUS = 0.5f;
        private const float DEFAULT_SPEED = 1.5f;
        private const float MIN_GAP = 0.0001f;

        public string ActionKey => "AvoidCrowding";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (c.Self == null || c.Neighbors == null) return BehaviorNodeStatus.Success;
            float radius = (float.TryParse(p1, out float r) && r > 0f) ? r : DEFAULT_RADIUS;
            float mult = (float.TryParse(p2, out float m) && m > 0f) ? m : 0.5f;

            Vector3 self = c.Self.Position;
            Vector3 push = Vector3.zero;
            int count = 0;
            foreach (IMonsterInstance other in c.Neighbors)
            {
                if (other == null || other.InstanceId == c.Self.InstanceId) continue;
                Vector3 away = self - other.Position;
                away.z = 0f;
                float d = away.magnitude;
                if (d > radius || d <= MIN_GAP) continue;
                push += away.normalized * (radius - d); // 가까울수록 강하게
                count++;
            }
            if (count == 0) return BehaviorNodeStatus.Success;
            // 양옆 동족이 정확히 상쇄되면 분리 방향이 없음 — 이동 0 인데 Running 으로 상위 Selector 를 막지 않도록 Success.
            if (push.sqrMagnitude <= MIN_GAP * MIN_GAP) return BehaviorNodeStatus.Success;

            float baseSpeed = c.SelfStats != null ? c.SelfStats.GetFinalValue(StatType.MoveSpeed) : 0f;
            if (baseSpeed <= 0f) baseSpeed = DEFAULT_SPEED;
            float step = baseSpeed * mult * c.DeltaTime;
            c.Self.SetPosition(self + push.normalized * step);
            return BehaviorNodeStatus.Running;
        }
    }

    /// <summary>Self ↔ Target 거리가 param1(미터)보다 크면 Success (원거리 사격 진입 조건 등). TargetInRange 의 보수.</summary>
    public class TargetOutOfRangeCondition : IBehaviorAction
    {
        public string ActionKey => "TargetOutOfRange";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (c.Self == null || c.Target == null) return BehaviorNodeStatus.Failure;
            if (!float.TryParse(p1, out float range)) return BehaviorNodeStatus.Failure;

            float dist = Vector3.Distance(c.Self.Position, c.TargetPosition);
            return dist > range ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
        }
    }
}
