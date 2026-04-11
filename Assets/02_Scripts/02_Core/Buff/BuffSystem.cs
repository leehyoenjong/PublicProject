using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IBuffSystem 구현체 — 버프/디버프 관리
    /// IStatSystem과 IEventBus를 생성자 주입 (DIP)
    /// </summary>
    public class BuffSystem : IBuffSystem
    {
        private readonly IStatSystem _statSystem;
        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, List<BuffInstance>> _activeBuffs = new Dictionary<string, List<BuffInstance>>();
        private readonly Dictionary<string, HashSet<string>> _buffIdImmunities = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, HashSet<BuffCategory>> _categoryImmunities = new Dictionary<string, HashSet<BuffCategory>>();

        public BuffSystem(IStatSystem statSystem, IEventBus eventBus)
        {
            _statSystem = statSystem;
            _eventBus = eventBus;
            Debug.Log("[BuffSystem] Init started");
        }

        public BuffResult AddBuff(string targetId, BuffData buffData, string sourceId)
        {
            if (buffData == null)
            {
                Debug.LogError("[BuffSystem] BuffData is null");
                return new BuffResult { Success = false, FailReason = "BuffData is null" };
            }

            if (string.IsNullOrEmpty(targetId))
            {
                Debug.LogError("[BuffSystem] targetId is null or empty");
                return new BuffResult { Success = false, FailReason = "targetId is null" };
            }

            // 면역 체크
            if (IsImmune(targetId, buffData))
            {
                _eventBus?.Publish(new BuffImmuneEvent
                {
                    TargetId = targetId,
                    BuffId = buffData.BuffId,
                    Reason = "Immune"
                });

                Debug.Log($"[BuffSystem] Buff immune: {buffData.BuffId} on {targetId}");
                return new BuffResult { Success = false, BuffId = buffData.BuffId, FailReason = "Immune" };
            }

            // 기존 버프 확인
            BuffInstance existing = FindBuff(targetId, buffData.BuffId);

            if (existing != null)
            {
                return HandleExistingBuff(targetId, buffData, existing, sourceId);
            }

            return ApplyNewBuff(targetId, buffData, sourceId);
        }

        public bool RemoveBuff(string targetId, string buffId)
        {
            if (!_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs)) return false;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].BuffId == buffId)
                {
                    RemoveBuffInstance(targetId, buffs[i], "Manual");
                    buffs.RemoveAt(i);
                    CleanupEmptyList(targetId);
                    return true;
                }
            }

            return false;
        }

        public int RemoveAllBuffs(string targetId, BuffCategory? category = null)
        {
            if (!_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs)) return 0;

            int count = 0;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].IsUndispellable) continue;
                if (category.HasValue && buffs[i].Category != category.Value) continue;

                RemoveBuffInstance(targetId, buffs[i], "RemoveAll");
                buffs.RemoveAt(i);
                count++;
            }

            CleanupEmptyList(targetId);
            Debug.Log($"[BuffSystem] Removed {count} buffs from {targetId}");
            return count;
        }

        public IReadOnlyList<IBuffInstance> GetBuffs(string targetId)
        {
            if (_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs))
            {
                return buffs.AsReadOnly();
            }

            return new List<IBuffInstance>().AsReadOnly();
        }

        public bool HasBuff(string targetId, string buffId)
        {
            return FindBuff(targetId, buffId) != null;
        }

        public int GetStackCount(string targetId, string buffId)
        {
            BuffInstance instance = FindBuff(targetId, buffId);
            return instance?.CurrentStack ?? 0;
        }

        public void AddImmunity(string targetId, string buffIdOrCategory)
        {
            if (System.Enum.TryParse<BuffCategory>(buffIdOrCategory, out BuffCategory category))
            {
                if (!_categoryImmunities.TryGetValue(targetId, out HashSet<BuffCategory> catSet))
                {
                    catSet = new HashSet<BuffCategory>();
                    _categoryImmunities[targetId] = catSet;
                }
                catSet.Add(category);
            }
            else
            {
                if (!_buffIdImmunities.TryGetValue(targetId, out HashSet<string> idSet))
                {
                    idSet = new HashSet<string>();
                    _buffIdImmunities[targetId] = idSet;
                }
                idSet.Add(buffIdOrCategory);
            }

            Debug.Log($"[BuffSystem] Immunity added: {targetId} -> {buffIdOrCategory}");
        }

        public void RemoveImmunity(string targetId, string buffIdOrCategory)
        {
            if (System.Enum.TryParse<BuffCategory>(buffIdOrCategory, out BuffCategory category))
            {
                if (_categoryImmunities.TryGetValue(targetId, out HashSet<BuffCategory> catSet))
                {
                    catSet.Remove(category);
                }
            }
            else
            {
                if (_buffIdImmunities.TryGetValue(targetId, out HashSet<string> idSet))
                {
                    idSet.Remove(buffIdOrCategory);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            List<string> keys = new List<string>(_activeBuffs.Keys);

            foreach (string targetId in keys)
            {
                if (!_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs)) continue;

                for (int i = buffs.Count - 1; i >= 0; i--)
                {
                    bool expired = buffs[i].TickTime(deltaTime, _eventBus);

                    if (expired)
                    {
                        RemoveBuffInstance(targetId, buffs[i], "Expired");

                        _eventBus?.Publish(new BuffExpiredEvent
                        {
                            TargetId = targetId,
                            BuffId = buffs[i].BuffId
                        });

                        buffs.RemoveAt(i);
                    }
                }

                CleanupEmptyList(targetId);
            }
        }

        public void ProcessTurn(string targetId)
        {
            if (!_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs)) return;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                bool expired = buffs[i].ProcessTurn(_eventBus);

                if (expired)
                {
                    RemoveBuffInstance(targetId, buffs[i], "TurnExpired");

                    _eventBus?.Publish(new BuffExpiredEvent
                    {
                        TargetId = targetId,
                        BuffId = buffs[i].BuffId
                    });

                    buffs.RemoveAt(i);
                }
            }

            CleanupEmptyList(targetId);
        }

        private BuffResult ApplyNewBuff(string targetId, BuffData buffData, string sourceId)
        {
            var instance = new BuffInstance(buffData, targetId, sourceId);

            if (!_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs))
            {
                buffs = new List<BuffInstance>();
                _activeBuffs[targetId] = buffs;
            }

            buffs.Add(instance);

            // 커스텀 이펙트 적용
            instance.CustomEffect?.OnApply(targetId);

            // StatContainer에 Modifier 등록
            IStatContainer container = _statSystem.GetContainer(targetId);
            if (container != null)
            {
                foreach (IStatModifier mod in instance.Modifiers)
                {
                    container.AddModifier(mod);
                }
            }

            _eventBus?.Publish(new BuffAppliedEvent
            {
                TargetId = targetId,
                BuffId = buffData.BuffId,
                StackCount = instance.CurrentStack,
                Duration = instance.RemainingDuration
            });

            Debug.Log($"[BuffSystem] Buff applied: {buffData.BuffId} on {targetId}");

            return new BuffResult
            {
                Success = true,
                BuffId = buffData.BuffId,
                CurrentStack = instance.CurrentStack
            };
        }

        private BuffResult HandleExistingBuff(string targetId, BuffData buffData, BuffInstance existing, string sourceId)
        {
            switch (buffData.StackPolicy)
            {
                case StackPolicy.None:
                    existing.RefreshDuration(buffData.RefreshPolicy);

                    _eventBus?.Publish(new BuffRefreshedEvent
                    {
                        TargetId = targetId,
                        BuffId = buffData.BuffId,
                        NewDuration = existing.RemainingDuration
                    });

                    return new BuffResult
                    {
                        Success = true,
                        BuffId = buffData.BuffId,
                        CurrentStack = existing.CurrentStack
                    };

                case StackPolicy.Duration:
                    existing.RefreshDuration(RefreshPolicy.Extend);

                    _eventBus?.Publish(new BuffRefreshedEvent
                    {
                        TargetId = targetId,
                        BuffId = buffData.BuffId,
                        NewDuration = existing.RemainingDuration
                    });

                    return new BuffResult
                    {
                        Success = true,
                        BuffId = buffData.BuffId,
                        CurrentStack = existing.CurrentStack
                    };

                case StackPolicy.Intensity:
                    if (existing.CurrentStack >= existing.MaxStack)
                    {
                        existing.RefreshDuration(buffData.RefreshPolicy);
                        return new BuffResult
                        {
                            Success = true,
                            BuffId = buffData.BuffId,
                            CurrentStack = existing.CurrentStack,
                            FailReason = "MaxStack"
                        };
                    }

                    int oldStack = existing.CurrentStack;

                    // 기존 Modifier 제거
                    IStatContainer container = _statSystem.GetContainer(targetId);
                    if (container != null)
                    {
                        container.RemoveModifiersFromSource(existing);
                    }

                    existing.AddStack();
                    existing.RefreshDuration(buffData.RefreshPolicy);

                    // 커스텀 이펙트 스택 콜백
                    existing.CustomEffect?.OnStack(targetId, existing.CurrentStack);

                    // 새 Modifier 등록
                    if (container != null)
                    {
                        foreach (IStatModifier mod in existing.Modifiers)
                        {
                            container.AddModifier(mod);
                        }
                    }

                    _eventBus?.Publish(new BuffStackChangedEvent
                    {
                        TargetId = targetId,
                        BuffId = buffData.BuffId,
                        OldStack = oldStack,
                        NewStack = existing.CurrentStack
                    });

                    return new BuffResult
                    {
                        Success = true,
                        BuffId = buffData.BuffId,
                        CurrentStack = existing.CurrentStack
                    };

                case StackPolicy.Independent:
                    // 독립 스택: 별도 인스턴스로 추가
                    return ApplyNewBuff(targetId, buffData, sourceId);

                default:
                    return new BuffResult { Success = false, FailReason = "Unknown StackPolicy" };
            }
        }

        private void RemoveBuffInstance(string targetId, BuffInstance instance, string reason)
        {
            // 커스텀 이펙트 제거 콜백
            instance.CustomEffect?.OnRemove(targetId);

            // StatContainer에서 Modifier 회수
            IStatContainer container = _statSystem.GetContainer(targetId);
            container?.RemoveModifiersFromSource(instance);

            instance.MarkExpired();
            instance.ClearModifiers();

            _eventBus?.Publish(new BuffRemovedEvent
            {
                TargetId = targetId,
                BuffId = instance.BuffId,
                RemoveReason = reason
            });

            Debug.Log($"[BuffSystem] Buff removed: {instance.BuffId} from {targetId} ({reason})");
        }

        private BuffInstance FindBuff(string targetId, string buffId)
        {
            if (!_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs)) return null;

            foreach (BuffInstance buff in buffs)
            {
                if (buff.BuffId == buffId && !buff.IsExpired)
                {
                    return buff;
                }
            }

            return null;
        }

        private bool IsImmune(string targetId, BuffData buffData)
        {
            if (_buffIdImmunities.TryGetValue(targetId, out HashSet<string> idSet) && idSet.Contains(buffData.BuffId))
            {
                return true;
            }

            if (_categoryImmunities.TryGetValue(targetId, out HashSet<BuffCategory> catSet) && catSet.Contains(buffData.Category))
            {
                return true;
            }

            return false;
        }

        private void CleanupEmptyList(string targetId)
        {
            if (_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs) && buffs.Count == 0)
            {
                _activeBuffs.Remove(targetId);
            }
        }
    }
}
