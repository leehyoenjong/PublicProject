using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ISkillSystem 구현. 스킬 등록/시전/쿨다운/액션 시퀀스 실행.
    /// MonoBehaviour 비의존 — 외부(호출자)가 Tick(deltaTime) 로 진행.
    /// 액션 Delay 는 시간 기반 스케줄링으로 처리 (코루틴 없음).
    /// </summary>
    public class SkillSystem : ISkillSystem
    {
        private readonly IEventBus _eventBus;
        private readonly IBuffSystem _buffSystem;
        private readonly IStatSystem _statSystem;
        private readonly ISoundManager _soundManager;
        private readonly IObjectPoolManager _objectPool;
        private readonly SkillActionRegistry _actionRegistry;

        private readonly Dictionary<string, SkillData> _skills = new Dictionary<string, SkillData>();
        private readonly Dictionary<string, Dictionary<string, SkillInstance>> _instances
            = new Dictionary<string, Dictionary<string, SkillInstance>>();
        private readonly List<ScheduledAction> _pending = new List<ScheduledAction>();
        private readonly List<ScheduledAction> _fireBuffer = new List<ScheduledAction>();

        public SkillSystem(
            IEventBus eventBus,
            IBuffSystem buffSystem = null,
            IStatSystem statSystem = null,
            ISoundManager soundManager = null,
            IObjectPoolManager objectPool = null,
            SkillActionRegistry registry = null)
        {
            _eventBus = eventBus;
            _buffSystem = buffSystem;
            _statSystem = statSystem;
            _soundManager = soundManager;
            _objectPool = objectPool;
            _actionRegistry = registry ?? SkillActionRegistry.CreateDefault();

            Debug.Log("[SkillSystem] Init started");
        }

        public void RegisterSkill(SkillData data)
        {
            if (data == null || string.IsNullOrEmpty(data.SkillId)) return;
            _skills[data.SkillId] = data;
        }

        public SkillData GetSkillData(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            return _skills.TryGetValue(skillId, out SkillData d) ? d : null;
        }

        public bool Cast(string skillId, string casterId, string targetId, int level = 1)
        {
            return Cast(skillId, casterId, targetId, Vector3.zero, Vector3.zero, level);
        }

        public bool Cast(string skillId, string casterId, string targetId, Vector3 casterPos, Vector3 targetPos, int level = 1)
        {
            SkillData data = GetSkillData(skillId);
            if (data == null)
            {
                Fail(skillId, casterId, "NotFound");
                return false;
            }

            SkillInstance inst = GetOrCreateInstance(casterId, skillId, level);
            if (!inst.IsReady)
            {
                Fail(skillId, casterId, "Cooldown");
                return false;
            }

            SkillLevelEntry lv = data.GetLevelEntry(level);
            float cooldown = (lv != null && lv.CooldownOverride > 0f) ? lv.CooldownOverride : data.Cooldown;
            float cost = (lv != null && lv.CostOverride > 0f) ? lv.CostOverride : data.CostAmount;
            float powerMult = lv != null ? lv.PowerMultiplier : 1f;

            if (!TryPayCost(casterId, data.CostType, cost))
            {
                Fail(skillId, casterId, "InsufficientCost");
                return false;
            }

            inst.StartCooldown(cooldown);

            _eventBus?.Publish(new SkillCastStartedEvent
            {
                SkillId = skillId,
                CasterId = casterId,
                TargetId = targetId,
                Level = level
            });
            _eventBus?.Publish(new SkillCooldownStartedEvent
            {
                SkillId = skillId,
                CasterId = casterId,
                Duration = cooldown
            });

            ScheduleSequence(data, casterId, targetId, casterPos, targetPos, level, powerMult, publishCompleted: true);
            Debug.Log($"[SkillSystem] Cast: {skillId} by {casterId} -> {targetId} (Lv{level}, cd={cooldown}, cost={cost})");
            return true;
        }

        public void Execute(string skillId, string casterId, string targetId, Vector3 casterPos, Vector3 targetPos, int level, float powerMultiplier)
        {
            SkillData data = GetSkillData(skillId);
            if (data == null)
            {
                Debug.LogError($"[SkillSystem] Execute: skill '{skillId}' not found");
                return;
            }

            float pm = powerMultiplier <= 0f ? 1f : powerMultiplier;
            ScheduleSequence(data, casterId, targetId, casterPos, targetPos, level, pm, publishCompleted: false);
        }

        public ISkillInstance GetInstance(string casterId, string skillId)
        {
            if (_instances.TryGetValue(casterId, out var map) && map.TryGetValue(skillId, out SkillInstance inst))
                return inst;
            return null;
        }

        public IReadOnlyList<ISkillInstance> GetInstances(string casterId)
        {
            var result = new List<ISkillInstance>();
            if (_instances.TryGetValue(casterId, out var map))
            {
                foreach (var v in map.Values) result.Add(v);
            }
            return result;
        }

        public void Tick(float deltaTime)
        {
            TickCooldowns(deltaTime);
            TickPendingActions(deltaTime);
        }

        private SkillInstance GetOrCreateInstance(string casterId, string skillId, int level)
        {
            if (!_instances.TryGetValue(casterId, out var map))
            {
                map = new Dictionary<string, SkillInstance>();
                _instances[casterId] = map;
            }
            if (!map.TryGetValue(skillId, out SkillInstance inst))
            {
                inst = new SkillInstance(skillId, casterId, level);
                map[skillId] = inst;
            }
            else
            {
                inst.SetLevel(level);
            }
            return inst;
        }

        private void Fail(string skillId, string casterId, string reason)
        {
            Debug.LogWarning($"[SkillSystem] Cast failed: {skillId} by {casterId} reason={reason}");
            _eventBus?.Publish(new SkillCastFailedEvent
            {
                SkillId = skillId,
                CasterId = casterId,
                Reason = reason
            });
        }

        private bool TryPayCost(string casterId, SkillCostType costType, float amount)
        {
            if (costType == SkillCostType.None || amount <= 0f) return true;
            // 실제 자원 차감은 StatSystem 확장(리소스 컨테이너) 또는 전용 ResourceSystem 에 위임.
            // 여기서는 이벤트로 외부에 요청만 위임하고 성공으로 가정(MVP). 실패 처리는 추후 IResourceSystem 도입 시 연결.
            _eventBus?.Publish(new SkillCostPaidEvent
            {
                CasterId = casterId,
                CostType = costType,
                Amount = amount
            });
            return true;
        }

        private void ScheduleSequence(
            SkillData data,
            string casterId,
            string targetId,
            Vector3 casterPos,
            Vector3 targetPos,
            int level,
            float powerMult,
            bool publishCompleted)
        {
            IReadOnlyList<SkillActionEntry> actions = data.Actions;
            if (actions == null || actions.Count == 0)
            {
                if (publishCompleted)
                {
                    _eventBus?.Publish(new SkillCastCompletedEvent { SkillId = data.SkillId, CasterId = casterId, TargetId = targetId });
                }
                return;
            }

            var context = new SkillContext
            {
                SkillData = data,
                CasterId = casterId,
                TargetId = targetId,
                CasterPosition = casterPos,
                TargetPosition = targetPos,
                Level = level,
                PowerMultiplier = powerMult,
                EventBus = _eventBus,
                BuffSystem = _buffSystem,
                StatSystem = _statSystem,
                SoundManager = _soundManager,
                ObjectPool = _objectPool,
                SkillSystem = this
            };

            float maxDelay = 0f;
            for (int i = 0; i < actions.Count; i++)
            {
                SkillActionEntry entry = actions[i];
                if (entry == null) continue;

                if (entry.Delay <= 0f)
                {
                    RunAction(entry, context);
                }
                else
                {
                    _pending.Add(new ScheduledAction(entry, context, entry.Delay));
                }

                if (entry.Delay > maxDelay) maxDelay = entry.Delay;
            }

            if (publishCompleted && maxDelay <= 0f)
            {
                _eventBus?.Publish(new SkillCastCompletedEvent { SkillId = data.SkillId, CasterId = casterId, TargetId = targetId });
            }
            else if (publishCompleted)
            {
                _pending.Add(ScheduledAction.CompletionMarker(context, maxDelay + 0.0001f));
            }
        }

        private void RunAction(SkillActionEntry entry, SkillContext context)
        {
            ISkillAction action = _actionRegistry.Get(entry.ActionType);
            if (action == null)
            {
                Debug.LogError($"[SkillSystem] No handler for {entry.ActionType}");
                _eventBus?.Publish(new SkillActionExecutedEvent
                {
                    SkillId = context.SkillData != null ? context.SkillData.SkillId : null,
                    ActionType = entry.ActionType,
                    CasterId = context.CasterId,
                    TargetId = context.TargetId,
                    Success = false,
                    Error = "NoHandler"
                });
                return;
            }

            try
            {
                action.Execute(context, entry);
                _eventBus?.Publish(new SkillActionExecutedEvent
                {
                    SkillId = context.SkillData != null ? context.SkillData.SkillId : null,
                    ActionType = entry.ActionType,
                    CasterId = context.CasterId,
                    TargetId = context.TargetId,
                    Success = true
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SkillSystem] Action {entry.ActionType} failed: {ex.Message}");
                _eventBus?.Publish(new SkillActionExecutedEvent
                {
                    SkillId = context.SkillData != null ? context.SkillData.SkillId : null,
                    ActionType = entry.ActionType,
                    CasterId = context.CasterId,
                    TargetId = context.TargetId,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        private void TickCooldowns(float deltaTime)
        {
            foreach (var kv in _instances)
            {
                foreach (var inst in kv.Value.Values)
                {
                    if (inst.TickCooldown(deltaTime))
                    {
                        _eventBus?.Publish(new SkillCooldownEndedEvent
                        {
                            SkillId = inst.SkillId,
                            CasterId = kv.Key
                        });
                    }
                }
            }
        }

        private void TickPendingActions(float deltaTime)
        {
            if (_pending.Count == 0) return;

            _fireBuffer.Clear();
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                ScheduledAction sa = _pending[i];
                sa.Remaining -= deltaTime;
                if (sa.Remaining <= 0f)
                {
                    _fireBuffer.Add(sa);
                    _pending.RemoveAt(i);
                }
                else
                {
                    _pending[i] = sa;
                }
            }

            for (int i = 0; i < _fireBuffer.Count; i++)
            {
                ScheduledAction sa = _fireBuffer[i];
                if (sa.IsCompletionMarker)
                {
                    _eventBus?.Publish(new SkillCastCompletedEvent
                    {
                        SkillId = sa.Context.SkillData != null ? sa.Context.SkillData.SkillId : null,
                        CasterId = sa.Context.CasterId,
                        TargetId = sa.Context.TargetId
                    });
                }
                else
                {
                    RunAction(sa.Entry, sa.Context);
                }
            }
            _fireBuffer.Clear();
        }

        private struct ScheduledAction
        {
            public SkillActionEntry Entry;
            public SkillContext Context;
            public float Remaining;
            public bool IsCompletionMarker;

            public ScheduledAction(SkillActionEntry entry, SkillContext context, float remaining)
            {
                Entry = entry;
                Context = context;
                Remaining = remaining;
                IsCompletionMarker = false;
            }

            public static ScheduledAction CompletionMarker(SkillContext context, float remaining)
            {
                return new ScheduledAction
                {
                    Entry = null,
                    Context = context,
                    Remaining = remaining,
                    IsCompletionMarker = true
                };
            }
        }
    }

    public struct SkillCostPaidEvent
    {
        public string CasterId;
        public SkillCostType CostType;
        public float Amount;
    }
}
