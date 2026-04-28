using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// BT 실행기. preset + context 를 받아 한 번 Tick.
    /// Composite (Sequence/Selector) 와 Decorator (Inverter/Cooldown/Repeat) 는 enum 분기로 직접 처리.
    /// Action/Condition 만 Registry lookup.
    /// </summary>
    public class BehaviorTreeExecutor
    {
        private readonly BehaviorActionRegistry _registry;

        public BehaviorTreeExecutor(BehaviorActionRegistry registry)
        {
            _registry = registry;
        }

        public BehaviorNodeStatus Tick(BehaviorTreePreset preset, BehaviorContext context)
        {
            if (preset == null || context == null) return BehaviorNodeStatus.Failure;
            return TickNode(preset, preset.RootIndex, context);
        }

        private BehaviorNodeStatus TickNode(BehaviorTreePreset preset, int nodeIndex, BehaviorContext context)
        {
            BehaviorNodeEntry node = preset.GetNode(nodeIndex);
            if (node == null) return BehaviorNodeStatus.Failure;

            switch (node.NodeType)
            {
                case BehaviorNodeType.Sequence:  return TickSequence(preset, node, context);
                case BehaviorNodeType.Selector:  return TickSelector(preset, node, context);
                case BehaviorNodeType.Inverter:  return TickInverter(preset, node, context);
                case BehaviorNodeType.Cooldown:  return TickCooldown(preset, node, nodeIndex, context);
                case BehaviorNodeType.Repeat:    return TickRepeat(preset, node, nodeIndex, context);
                case BehaviorNodeType.Condition:
                case BehaviorNodeType.Action:    return TickActionOrCondition(node, context);
                default:
                    Debug.LogWarning($"[BehaviorTreeExecutor] Unknown node type: {node.NodeType}");
                    return BehaviorNodeStatus.Failure;
            }
        }

        private BehaviorNodeStatus TickSequence(BehaviorTreePreset preset, BehaviorNodeEntry node, BehaviorContext context)
        {
            if (node.ChildIndices == null) return BehaviorNodeStatus.Success;
            for (int i = 0; i < node.ChildIndices.Length; i++)
            {
                BehaviorNodeStatus s = TickNode(preset, node.ChildIndices[i], context);
                if (s != BehaviorNodeStatus.Success) return s;
            }
            return BehaviorNodeStatus.Success;
        }

        private BehaviorNodeStatus TickSelector(BehaviorTreePreset preset, BehaviorNodeEntry node, BehaviorContext context)
        {
            if (node.ChildIndices == null) return BehaviorNodeStatus.Failure;
            for (int i = 0; i < node.ChildIndices.Length; i++)
            {
                BehaviorNodeStatus s = TickNode(preset, node.ChildIndices[i], context);
                if (s != BehaviorNodeStatus.Failure) return s;
            }
            return BehaviorNodeStatus.Failure;
        }

        private BehaviorNodeStatus TickInverter(BehaviorTreePreset preset, BehaviorNodeEntry node, BehaviorContext context)
        {
            if (node.ChildIndices == null || node.ChildIndices.Length == 0) return BehaviorNodeStatus.Failure;
            BehaviorNodeStatus s = TickNode(preset, node.ChildIndices[0], context);
            if (s == BehaviorNodeStatus.Success) return BehaviorNodeStatus.Failure;
            if (s == BehaviorNodeStatus.Failure) return BehaviorNodeStatus.Success;
            return s;
        }

        private BehaviorNodeStatus TickCooldown(BehaviorTreePreset preset, BehaviorNodeEntry node, int nodeIndex, BehaviorContext context)
        {
            if (context.IsOnCooldown(nodeIndex)) return BehaviorNodeStatus.Failure;
            if (node.ChildIndices == null || node.ChildIndices.Length == 0) return BehaviorNodeStatus.Failure;

            BehaviorNodeStatus s = TickNode(preset, node.ChildIndices[0], context);
            if (s == BehaviorNodeStatus.Success)
            {
                if (float.TryParse(node.Param1, out float cooldown) && cooldown > 0f)
                {
                    context.SetCooldown(nodeIndex, cooldown);
                }
            }
            return s;
        }

        private BehaviorNodeStatus TickRepeat(BehaviorTreePreset preset, BehaviorNodeEntry node, int nodeIndex, BehaviorContext context)
        {
            if (node.ChildIndices == null || node.ChildIndices.Length == 0) return BehaviorNodeStatus.Failure;

            int max = 0;
            int.TryParse(node.Param1, out max);

            BehaviorNodeStatus s = TickNode(preset, node.ChildIndices[0], context);
            if (s == BehaviorNodeStatus.Success)
            {
                context.IncrementRepeat(nodeIndex);
                if (max > 0 && context.GetRepeatCount(nodeIndex) >= max)
                {
                    context.ResetRepeat(nodeIndex);
                    return BehaviorNodeStatus.Success;
                }
                return BehaviorNodeStatus.Running;
            }
            return s;
        }

        private BehaviorNodeStatus TickActionOrCondition(BehaviorNodeEntry node, BehaviorContext context)
        {
            IBehaviorAction action = _registry?.Get(node.ActionKey);
            if (action == null)
            {
                Debug.LogWarning($"[BehaviorTreeExecutor] Unknown actionKey: {node.ActionKey}");
                return BehaviorNodeStatus.Failure;
            }
            return action.Tick(context, node.Param1, node.Param2, node.Param3);
        }
    }
}
