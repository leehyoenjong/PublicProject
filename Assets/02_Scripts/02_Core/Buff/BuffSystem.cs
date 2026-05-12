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
        private readonly ILocalizationSystem _locSystem;
        private readonly Dictionary<string, List<BuffInstance>> _activeBuffs = new Dictionary<string, List<BuffInstance>>();
        private readonly Dictionary<string, HashSet<string>> _buffIdImmunities = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, HashSet<BuffCategory>> _categoryImmunities = new Dictionary<string, HashSet<BuffCategory>>();

        public BuffSystem(IStatSystem statSystem, IEventBus eventBus, ILocalizationSystem locSystem = null)
        {
            _statSystem = statSystem;
            _eventBus = eventBus;
            _locSystem = locSystem;
            Debug.Log("[버프] 초기화 시작.");
        }

        public BuffResult AddBuff(string targetId, BuffData buffData, string sourceId, string sourceSkillId = "")
        {
            if (buffData == null)
            {
                Debug.LogError("[버프] BuffData가 null임.");
                return new BuffResult { Success = false, FailReason = "BuffData is null" };
            }

            if (string.IsNullOrEmpty(targetId))
            {
                Debug.LogError("[버프] targetId가 null 또는 빈 값임.");
                return new BuffResult { Success = false, FailReason = "targetId is null" };
            }

            string skillKey = sourceSkillId ?? string.Empty;

            // 면역 체크
            if (IsImmune(targetId, buffData))
            {
                _eventBus?.Publish(new BuffImmuneEvent
                {
                    TargetId = targetId,
                    BuffId = buffData.BuffId,
                    SourceSkillId = skillKey,
                    Reason = "Immune"
                });

                Debug.Log($"[버프] 버프 면역: {buffData.BuffId} → {targetId}");
                return new BuffResult { Success = false, BuffId = buffData.BuffId, FailReason = "Immune" };
            }

            // 기존 버프 확인 — (buffId + sourceSkillId) 페어로 매칭
            BuffInstance existing = FindBuff(targetId, buffData.BuffId, skillKey);

            if (existing != null)
            {
                return HandleExistingBuff(targetId, buffData, existing, sourceId, skillKey);
            }

            return ApplyNewBuff(targetId, buffData, sourceId, skillKey);
        }

        public bool RemoveBuff(string targetId, string buffId, string sourceSkillId = null)
        {
            if (!_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs)) return false;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].BuffId != buffId) continue;
                if (sourceSkillId != null && buffs[i].SourceSkillId != sourceSkillId) continue;

                RemoveBuffInstance(targetId, buffs[i], "Manual");
                buffs.RemoveAt(i);
                CleanupEmptyList(targetId);
                return true;
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
            Debug.Log($"[버프] {targetId}에서 버프 {count}개 제거됨.");
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

        public bool HasBuff(string targetId, string buffId, string sourceSkillId = null)
        {
            return FindBuff(targetId, buffId, sourceSkillId) != null;
        }

        public int GetStackCount(string targetId, string buffId, string sourceSkillId = null)
        {
            BuffInstance instance = FindBuff(targetId, buffId, sourceSkillId);
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

            Debug.Log($"[버프] 면역 추가됨: {targetId} → {buffIdOrCategory}");
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
                        BuffInstance expiredInstance = buffs[i];
                        RemoveBuffInstance(targetId, expiredInstance, "Expired");

                        _eventBus?.Publish(new BuffExpiredEvent
                        {
                            TargetId = targetId,
                            BuffId = expiredInstance.BuffId,
                            SourceSkillId = expiredInstance.SourceSkillId
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
                    BuffInstance expiredInstance = buffs[i];
                    RemoveBuffInstance(targetId, expiredInstance, "TurnExpired");

                    _eventBus?.Publish(new BuffExpiredEvent
                    {
                        TargetId = targetId,
                        BuffId = expiredInstance.BuffId,
                        SourceSkillId = expiredInstance.SourceSkillId
                    });

                    buffs.RemoveAt(i);
                }
            }

            CleanupEmptyList(targetId);
        }

        private BuffResult ApplyNewBuff(string targetId, BuffData buffData, string sourceId, string sourceSkillId)
        {
            var instance = new BuffInstance(buffData, targetId, sourceId, sourceSkillId, _locSystem);

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
                SourceSkillId = sourceSkillId,
                StackCount = instance.CurrentStack,
                Duration = instance.RemainingDuration
            });

            Debug.Log($"[버프] 버프 적용됨: {buffData.BuffId} → {targetId} (skill={sourceSkillId})");

            return new BuffResult
            {
                Success = true,
                BuffId = buffData.BuffId,
                CurrentStack = instance.CurrentStack
            };
        }

        private BuffResult HandleExistingBuff(string targetId, BuffData buffData, BuffInstance existing, string sourceId, string sourceSkillId)
        {
            switch (buffData.StackPolicy)
            {
                case StackPolicy.None:
                    existing.RefreshDuration(buffData.RefreshPolicy);

                    _eventBus?.Publish(new BuffRefreshedEvent
                    {
                        TargetId = targetId,
                        BuffId = buffData.BuffId,
                        SourceSkillId = sourceSkillId,
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
                        SourceSkillId = sourceSkillId,
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
                        SourceSkillId = sourceSkillId,
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
                    // 독립 스택: 별도 인스턴스로 추가 (sourceSkillId 동일해도 강제 분리)
                    return ApplyNewBuff(targetId, buffData, sourceId, sourceSkillId);

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
                SourceSkillId = instance.SourceSkillId,
                RemoveReason = reason
            });

            Debug.Log($"[버프] 버프 제거됨: {instance.BuffId} ← {targetId} ({reason})");
        }

        private BuffInstance FindBuff(string targetId, string buffId, string sourceSkillId)
        {
            if (!_activeBuffs.TryGetValue(targetId, out List<BuffInstance> buffs)) return null;

            foreach (BuffInstance buff in buffs)
            {
                if (buff.IsExpired) continue;
                if (buff.BuffId != buffId) continue;
                // sourceSkillId 가 null 이면 buffId 만으로 매칭 (외부 호출 호환)
                if (sourceSkillId != null && buff.SourceSkillId != sourceSkillId) continue;

                return buff;
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
