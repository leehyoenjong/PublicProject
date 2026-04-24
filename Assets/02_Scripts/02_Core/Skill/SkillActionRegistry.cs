using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// SkillActionType → ISkillAction 매핑. 프로젝트별 확장은 Register 로 오버라이드 가능.
    /// </summary>
    public class SkillActionRegistry
    {
        private readonly Dictionary<SkillActionType, ISkillAction> _actions = new Dictionary<SkillActionType, ISkillAction>();

        public void Register(ISkillAction action)
        {
            if (action == null) return;
            _actions[action.Type] = action;
        }

        public ISkillAction Get(SkillActionType type)
        {
            return _actions.TryGetValue(type, out ISkillAction action) ? action : null;
        }

        public static SkillActionRegistry CreateDefault()
        {
            var registry = new SkillActionRegistry();
            registry.Register(new ApplyBuffAction());
            registry.Register(new DealDamageAction());
            registry.Register(new HealAction());
            registry.Register(new SpawnAction());
            registry.Register(new MoveAction());
            registry.Register(new PlaySfxAction());
            registry.Register(new PlayVfxAction());
            registry.Register(new PlayAnimationAction());
            return registry;
        }
    }
}
