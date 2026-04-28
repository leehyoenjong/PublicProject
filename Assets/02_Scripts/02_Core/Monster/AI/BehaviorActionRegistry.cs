using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// actionKey → IBehaviorAction 매핑. Action / Condition 노드가 같이 사용.
    /// 게임별 확장은 Register 로 추가 (SkillActionRegistry 패턴).
    /// </summary>
    public class BehaviorActionRegistry
    {
        private readonly Dictionary<string, IBehaviorAction> _actions = new Dictionary<string, IBehaviorAction>();

        public void Register(IBehaviorAction action)
        {
            if (action == null || string.IsNullOrEmpty(action.ActionKey)) return;
            _actions[action.ActionKey] = action;
        }

        public IBehaviorAction Get(string actionKey)
        {
            if (string.IsNullOrEmpty(actionKey)) return null;
            return _actions.TryGetValue(actionKey, out IBehaviorAction action) ? action : null;
        }

        public static BehaviorActionRegistry CreateDefault(ISkillSystem skillSystem = null)
        {
            var r = new BehaviorActionRegistry();
            r.Register(new IdleAction());
            r.Register(new WaitAction());
            r.Register(new MoveToTargetAction());
            r.Register(new CastSkillAction(skillSystem));
            r.Register(new HpBelowCondition());
            r.Register(new TargetInRangeCondition());
            r.Register(new HasBuffCondition());
            return r;
        }
    }
}
