using UnityEngine;

namespace PublicFramework
{
    /// <summary>Self 의 HP 비율이 param1 (0~1) 이하이면 Success. SelfStats 필요.</summary>
    public class HpBelowCondition : IBehaviorAction
    {
        public string ActionKey => "HpBelow";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (c.SelfStats == null) return BehaviorNodeStatus.Failure;
            if (!float.TryParse(p1, out float threshold)) return BehaviorNodeStatus.Failure;

            float hp = c.SelfStats.CurrentHP;
            float maxHp = c.SelfStats.GetFinalValue(StatType.HP);
            if (maxHp <= 0f) return BehaviorNodeStatus.Failure;

            float ratio = hp / maxHp;
            return ratio <= threshold ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
        }
    }

    /// <summary>Self ↔ Target 거리가 param1 (미터) 이하이면 Success.</summary>
    public class TargetInRangeCondition : IBehaviorAction
    {
        public string ActionKey => "TargetInRange";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (c.Self == null || c.Target == null) return BehaviorNodeStatus.Failure;
            if (!float.TryParse(p1, out float range)) return BehaviorNodeStatus.Failure;

            Vector3 selfPos = (c.Self is MonsterInstance m) ? m.Position : Vector3.zero;
            float dist = Vector3.Distance(selfPos, c.TargetPosition);
            return dist <= range ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
        }
    }

    /// <summary>
    /// Self 가 param1 (buffMID) 보유 중이면 Success. SelfStats 의 버프 컨테이너 조회.
    /// 현재 골격은 Blackboard 의 "buff_{buffMID}" 키 존재 여부로 단순 판정.
    /// </summary>
    public class HasBuffCondition : IBehaviorAction
    {
        public string ActionKey => "HasBuff";
        public BehaviorNodeStatus Tick(BehaviorContext c, string p1, string p2, string p3)
        {
            if (string.IsNullOrEmpty(p1)) return BehaviorNodeStatus.Failure;
            bool has = c.GetBlackboard<bool>($"buff_{p1}", false);
            return has ? BehaviorNodeStatus.Success : BehaviorNodeStatus.Failure;
        }
    }
}
