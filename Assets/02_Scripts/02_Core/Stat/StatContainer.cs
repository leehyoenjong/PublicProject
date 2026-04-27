using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IStatContainer 기본 구현. 4단계 계산식: Final = (Base + Flat) × (1 + Percent) × Multiplicative.
    /// 기본 enum 15종 + 커스텀 dict 하이브리드. 성장 커브/스냅샷/히스토리/분해/재생 틱 지원.
    /// </summary>
    public class StatContainer : IStatContainer
    {
        private readonly IEventBus _eventBus;
        private readonly ITimeProvider _timeProvider;

        private readonly Dictionary<StatType, float> _baseValues = new();
        private readonly Dictionary<StatType, LevelCurve> _curves = new();
        private readonly Dictionary<StatType, float> _cachedFinal = new();

        private readonly Dictionary<string, float> _customBase = new();
        private readonly Dictionary<string, LevelCurve> _customCurves = new();
        private readonly Dictionary<string, float> _customCachedFinal = new();
        private readonly Dictionary<string, Func<int, float>> _customFormulas = new();

        private readonly List<IStatModifier> _flatMods = new();
        private readonly List<IStatModifier> _percentMods = new();
        private readonly List<IStatModifier> _multMods = new();

        private readonly List<StatHistoryEntry> _history = new();

        private int _level = 1;
        private float _currentHP;
        private float _currentMP;

        public string OwnerId { get; }
        public int Level => _level;
        public float CurrentHP => _currentHP;
        public float CurrentMP => _currentMP;
        public bool IsAlive => _currentHP > 0f;
        public int HistoryCapacity { get; set; } = 100;

        public IReadOnlyCollection<string> CustomKeys => _customBase.Keys;

        public IReadOnlyList<IStatModifier> AllModifiers
        {
            get
            {
                var all = new List<IStatModifier>(_flatMods.Count + _percentMods.Count + _multMods.Count);
                all.AddRange(_flatMods);
                all.AddRange(_percentMods);
                all.AddRange(_multMods);
                return all;
            }
        }

        public float MaxMP => GetFinalValue("MP");

        public StatContainer(string ownerId, int level = 1, IEventBus eventBus = null, ITimeProvider timeProvider = null)
        {
            OwnerId = ownerId;
            _eventBus = eventBus;
            _timeProvider = timeProvider;
            _level = level < 1 ? 1 : level;

            foreach (StatType t in Enum.GetValues(typeof(StatType)))
            {
                _baseValues[t] = 0f;
                _cachedFinal[t] = 0f;
            }

            Debug.Log($"[StatContainer] Created: {ownerId} (level {_level})");
        }

        // ────────────────────────────────────────────────────────────
        // 기본 enum 스탯 API
        // ────────────────────────────────────────────────────────────
        public float GetFinalValue(StatType type) => _cachedFinal.TryGetValue(type, out float v) ? v : 0f;
        public float GetBaseValue(StatType type) => _baseValues.TryGetValue(type, out float v) ? v : 0f;

        public void SetBaseValue(StatType type, float value)
        {
            float oldFinal = GetFinalValue(type);
            _baseValues[type] = value;
            RecalculateStat(type, oldFinal, ModifierSource.Other);
        }

        public void SetGrowthCurve(StatType type, LevelCurve curve)
        {
            _curves[type] = curve;
            ApplyCurveToBase(type);
        }

        public void RegisterCustomCurve(string key, Func<int, float> formula)
        {
            if (string.IsNullOrEmpty(key)) return;
            _customFormulas[key] = formula;
        }

        // ────────────────────────────────────────────────────────────
        // 커스텀 스탯 API
        // ────────────────────────────────────────────────────────────
        public float GetFinalValue(string customKey)
        {
            if (string.IsNullOrEmpty(customKey)) return 0f;
            return _customCachedFinal.TryGetValue(customKey, out float v) ? v : 0f;
        }

        public float GetBaseValue(string customKey)
        {
            if (string.IsNullOrEmpty(customKey)) return 0f;
            return _customBase.TryGetValue(customKey, out float v) ? v : 0f;
        }

        public void SetBaseValue(string customKey, float value)
        {
            if (string.IsNullOrEmpty(customKey)) return;
            float oldFinal = GetFinalValue(customKey);
            _customBase[customKey] = value;
            if (!_customCachedFinal.ContainsKey(customKey)) _customCachedFinal[customKey] = 0f;
            RecalculateCustom(customKey, oldFinal, ModifierSource.Other);
        }

        public void SetGrowthCurve(string customKey, LevelCurve curve)
        {
            if (string.IsNullOrEmpty(customKey)) return;
            _customCurves[customKey] = curve;
            ApplyCustomCurveToBase(customKey);
        }

        // ────────────────────────────────────────────────────────────
        // Modifier 추가/제거
        // ────────────────────────────────────────────────────────────
        public void AddModifier(IStatModifier modifier)
        {
            if (modifier == null) return;

            float oldFinal = string.IsNullOrEmpty(modifier.CustomKey)
                ? GetFinalValue(modifier.TargetStat)
                : GetFinalValue(modifier.CustomKey);

            List<IStatModifier> list = GetListByLayer(modifier.Layer);
            list.Add(modifier);
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            if (string.IsNullOrEmpty(modifier.CustomKey))
            {
                RecalculateStat(modifier.TargetStat, oldFinal, modifier.SourceTag);
            }
            else
            {
                if (!_customCachedFinal.ContainsKey(modifier.CustomKey))
                    _customCachedFinal[modifier.CustomKey] = 0f;
                RecalculateCustom(modifier.CustomKey, oldFinal, modifier.SourceTag);
            }

            _eventBus?.Publish(new ModifierAddedEvent
            {
                OwnerId = OwnerId,
                Type = modifier.TargetStat,
                CustomKey = modifier.CustomKey,
                Layer = modifier.Layer,
                Value = modifier.Value,
                SourceTag = modifier.SourceTag,
            });
        }

        public bool RemoveModifier(IStatModifier modifier)
        {
            if (modifier == null) return false;
            List<IStatModifier> list = GetListByLayer(modifier.Layer);

            float oldFinal = string.IsNullOrEmpty(modifier.CustomKey)
                ? GetFinalValue(modifier.TargetStat)
                : GetFinalValue(modifier.CustomKey);

            if (!list.Remove(modifier)) return false;

            if (string.IsNullOrEmpty(modifier.CustomKey))
                RecalculateStat(modifier.TargetStat, oldFinal, modifier.SourceTag);
            else
                RecalculateCustom(modifier.CustomKey, oldFinal, modifier.SourceTag);

            _eventBus?.Publish(new ModifierRemovedEvent
            {
                OwnerId = OwnerId,
                Type = modifier.TargetStat,
                CustomKey = modifier.CustomKey,
                Layer = modifier.Layer,
            });
            return true;
        }

        public int RemoveModifiersFromSource(object source)
        {
            if (source == null) return 0;
            int removed = 0;
            removed += RemoveFromList(_flatMods, source);
            removed += RemoveFromList(_percentMods, source);
            removed += RemoveFromList(_multMods, source);
            if (removed > 0) RecalculateAll();
            return removed;
        }

        public IReadOnlyList<IStatModifier> GetModifiers(StatLayer layer) =>
            GetListByLayer(layer).AsReadOnly();

        // ────────────────────────────────────────────────────────────
        // 레벨 변경 (성장 커브 적용)
        // ────────────────────────────────────────────────────────────
        public void SetLevel(int level)
        {
            int clamped = level < 1 ? 1 : level;
            if (clamped == _level) return;
            _level = clamped;

            foreach (var t in new List<StatType>(_curves.Keys))
                ApplyCurveToBase(t);
            foreach (var k in new List<string>(_customCurves.Keys))
                ApplyCustomCurveToBase(k);
        }

        // ────────────────────────────────────────────────────────────
        // CurrentHP/MP
        // ────────────────────────────────────────────────────────────
        public void SetCurrentHP(float hp)
        {
            float maxHP = GetFinalValue(StatType.HP);
            _currentHP = Mathf.Clamp(hp, 0f, maxHP);
        }

        public void SetCurrentMP(float mp)
        {
            float max = MaxMP;
            _currentMP = max > 0f ? Mathf.Clamp(mp, 0f, max) : Mathf.Max(0f, mp);
        }

        public void ResetToMax()
        {
            _currentHP = GetFinalValue(StatType.HP);
            _currentMP = MaxMP;
        }

        public void Kill() => _currentHP = 0f;

        public void Revive()
        {
            _currentHP = GetFinalValue(StatType.HP);
        }

        // ────────────────────────────────────────────────────────────
        // 분해 API
        // ────────────────────────────────────────────────────────────
        public StatDecomposition GetDecomposition(StatType type)
        {
            float baseVal = GetBaseValue(type);
            var contribs = new List<StatContribution>();
            float flatTotal = 0f, percentTotal = 0f, multTotal = 1f;

            foreach (var m in _flatMods)
            {
                if (m.TargetStat != type || !string.IsNullOrEmpty(m.CustomKey)) continue;
                flatTotal += m.Value;
                contribs.Add(MakeContribution(m));
            }
            foreach (var m in _percentMods)
            {
                if (m.TargetStat != type || !string.IsNullOrEmpty(m.CustomKey)) continue;
                percentTotal += m.Value;
                contribs.Add(MakeContribution(m));
            }
            foreach (var m in _multMods)
            {
                if (m.TargetStat != type || !string.IsNullOrEmpty(m.CustomKey)) continue;
                multTotal *= m.Value;
                contribs.Add(MakeContribution(m));
            }

            return new StatDecomposition
            {
                Type = type,
                CustomKey = null,
                BaseValue = baseVal,
                FlatTotal = flatTotal,
                PercentTotal = percentTotal,
                MultiplicativeTotal = multTotal,
                FinalValue = GetFinalValue(type),
                Contributions = contribs,
            };
        }

        public StatDecomposition GetDecomposition(string customKey)
        {
            float baseVal = GetBaseValue(customKey);
            var contribs = new List<StatContribution>();
            float flatTotal = 0f, percentTotal = 0f, multTotal = 1f;

            foreach (var m in _flatMods)
            {
                if (m.CustomKey != customKey) continue;
                flatTotal += m.Value;
                contribs.Add(MakeContribution(m));
            }
            foreach (var m in _percentMods)
            {
                if (m.CustomKey != customKey) continue;
                percentTotal += m.Value;
                contribs.Add(MakeContribution(m));
            }
            foreach (var m in _multMods)
            {
                if (m.CustomKey != customKey) continue;
                multTotal *= m.Value;
                contribs.Add(MakeContribution(m));
            }

            return new StatDecomposition
            {
                Type = default,
                CustomKey = customKey,
                BaseValue = baseVal,
                FlatTotal = flatTotal,
                PercentTotal = percentTotal,
                MultiplicativeTotal = multTotal,
                FinalValue = GetFinalValue(customKey),
                Contributions = contribs,
            };
        }

        // ────────────────────────────────────────────────────────────
        // 스냅샷
        // ────────────────────────────────────────────────────────────
        public IStatSnapshot TakeSnapshot()
        {
            return new StatSnapshot(
                OwnerId,
                _level,
                _timeProvider?.NowUtc ?? DateTime.UtcNow,
                new Dictionary<StatType, float>(_baseValues),
                new Dictionary<string, float>(_customBase),
                new Dictionary<StatType, LevelCurve>(_curves),
                new Dictionary<string, LevelCurve>(_customCurves),
                new List<IStatModifier>(AllModifiers),
                _currentHP,
                _currentMP);
        }

        public void RestoreSnapshot(IStatSnapshot snapshot)
        {
            if (snapshot is not StatSnapshot snap) return;
            _level = snap.Level;
            _baseValues.Clear();
            foreach (var kv in snap.BaseValues) _baseValues[kv.Key] = kv.Value;
            _customBase.Clear();
            foreach (var kv in snap.CustomBaseValues) _customBase[kv.Key] = kv.Value;
            _curves.Clear();
            foreach (var kv in snap.Curves) _curves[kv.Key] = kv.Value;
            _customCurves.Clear();
            foreach (var kv in snap.CustomCurves) _customCurves[kv.Key] = kv.Value;
            _flatMods.Clear();
            _percentMods.Clear();
            _multMods.Clear();
            foreach (var m in snap.Modifiers) GetListByLayer(m.Layer).Add(m);
            _currentHP = snap.CurrentHP;
            _currentMP = snap.CurrentMP;
            RecalculateAll();
        }

        // ────────────────────────────────────────────────────────────
        // 히스토리
        // ────────────────────────────────────────────────────────────
        public IReadOnlyList<StatHistoryEntry> GetHistory(int? limit = null)
        {
            if (limit == null || limit.Value >= _history.Count) return _history.AsReadOnly();
            int start = _history.Count - limit.Value;
            return _history.GetRange(start, limit.Value).AsReadOnly();
        }

        public void ClearHistory() => _history.Clear();

        // ────────────────────────────────────────────────────────────
        // 재생 틱 + 임시 modifier 만료
        // ────────────────────────────────────────────────────────────
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            DecrementTemporary(_flatMods, deltaTime);
            DecrementTemporary(_percentMods, deltaTime);
            DecrementTemporary(_multMods, deltaTime);

            float maxHP = GetFinalValue(StatType.HP);
            if (_currentHP > 0f && _currentHP < maxHP)
            {
                float rate = GetFinalValue(StatType.HPRegen);
                if (rate > 0f) _currentHP = Mathf.Min(_currentHP + rate * deltaTime, maxHP);
            }

            float maxMP = MaxMP;
            if (maxMP > 0f && _currentMP < maxMP)
            {
                float rate = GetFinalValue(StatType.MPRegen);
                if (rate > 0f) _currentMP = Mathf.Min(_currentMP + rate * deltaTime, maxMP);
            }
        }

        public void RecalculateAll()
        {
            foreach (StatType t in Enum.GetValues(typeof(StatType)))
            {
                float oldFinal = GetFinalValue(t);
                RecalculateStat(t, oldFinal, ModifierSource.Other);
            }
            foreach (var k in _customBase.Keys)
            {
                float oldFinal = GetFinalValue(k);
                RecalculateCustom(k, oldFinal, ModifierSource.Other);
            }
        }

        // ────────────────────────────────────────────────────────────
        // 내부
        // ────────────────────────────────────────────────────────────
        private void RecalculateStat(StatType type, float oldFinal, ModifierSource source)
        {
            float baseVal = _baseValues.TryGetValue(type, out float b) ? b : 0f;
            float flat = 0f, percent = 0f, mult = 1f;

            foreach (var m in _flatMods)
                if (m.TargetStat == type && string.IsNullOrEmpty(m.CustomKey)) flat += m.Value;
            foreach (var m in _percentMods)
                if (m.TargetStat == type && string.IsNullOrEmpty(m.CustomKey)) percent += m.Value;
            foreach (var m in _multMods)
                if (m.TargetStat == type && string.IsNullOrEmpty(m.CustomKey)) mult *= m.Value;

            float final = (baseVal + flat) * (1f + percent) * mult;
            if (final < 0f) final = 0f;
            _cachedFinal[type] = final;

            if (Math.Abs(oldFinal - final) > float.Epsilon)
            {
                AppendHistory(new StatHistoryEntry
                {
                    Timestamp = _timeProvider?.NowUtc ?? DateTime.UtcNow,
                    Type = type,
                    CustomKey = null,
                    OldValue = oldFinal,
                    NewValue = final,
                    Source = source,
                });
                _eventBus?.Publish(new StatChangedEvent
                {
                    OwnerId = OwnerId,
                    Type = type,
                    CustomKey = null,
                    OldValue = oldFinal,
                    NewValue = final,
                });
            }
        }

        private void RecalculateCustom(string key, float oldFinal, ModifierSource source)
        {
            float baseVal = _customBase.TryGetValue(key, out float b) ? b : 0f;
            float flat = 0f, percent = 0f, mult = 1f;

            foreach (var m in _flatMods)
                if (m.CustomKey == key) flat += m.Value;
            foreach (var m in _percentMods)
                if (m.CustomKey == key) percent += m.Value;
            foreach (var m in _multMods)
                if (m.CustomKey == key) mult *= m.Value;

            float final = (baseVal + flat) * (1f + percent) * mult;
            if (final < 0f) final = 0f;
            _customCachedFinal[key] = final;

            if (Math.Abs(oldFinal - final) > float.Epsilon)
            {
                AppendHistory(new StatHistoryEntry
                {
                    Timestamp = _timeProvider?.NowUtc ?? DateTime.UtcNow,
                    Type = default,
                    CustomKey = key,
                    OldValue = oldFinal,
                    NewValue = final,
                    Source = source,
                });
                _eventBus?.Publish(new StatChangedEvent
                {
                    OwnerId = OwnerId,
                    Type = default,
                    CustomKey = key,
                    OldValue = oldFinal,
                    NewValue = final,
                });
            }
        }

        private void ApplyCurveToBase(StatType type)
        {
            if (!_curves.TryGetValue(type, out LevelCurve curve)) return;
            Func<int, float> custom = null;
            if (curve.Curve == GrowthCurve.Custom && !string.IsNullOrEmpty(curve.CustomKey))
                _customFormulas.TryGetValue(curve.CustomKey, out custom);
            float baseVal = curve.Evaluate(_level, custom);
            SetBaseValue(type, baseVal);
        }

        private void ApplyCustomCurveToBase(string key)
        {
            if (!_customCurves.TryGetValue(key, out LevelCurve curve)) return;
            Func<int, float> custom = null;
            if (curve.Curve == GrowthCurve.Custom && !string.IsNullOrEmpty(curve.CustomKey))
                _customFormulas.TryGetValue(curve.CustomKey, out custom);
            float baseVal = curve.Evaluate(_level, custom);
            SetBaseValue(key, baseVal);
        }

        private List<IStatModifier> GetListByLayer(StatLayer layer) => layer switch
        {
            StatLayer.Flat => _flatMods,
            StatLayer.Percent => _percentMods,
            StatLayer.Multiplicative => _multMods,
            _ => throw new ArgumentException($"[StatContainer] Unknown layer: {layer}"),
        };

        private int RemoveFromList(List<IStatModifier> list, object source)
        {
            int count = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(list[i].Source, source))
                {
                    list.RemoveAt(i);
                    count++;
                }
            }
            return count;
        }

        private void DecrementTemporary(List<IStatModifier> list, float dt)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is StatModifier sm && sm.IsTemporary)
                {
                    sm.DecrementTime(dt);
                    if (sm.IsExpired) RemoveModifier(sm);
                }
            }
        }

        private void AppendHistory(StatHistoryEntry entry)
        {
            _history.Add(entry);
            if (HistoryCapacity > 0 && _history.Count > HistoryCapacity)
                _history.RemoveAt(0);
        }

        private static StatContribution MakeContribution(IStatModifier m) => new StatContribution
        {
            Layer = m.Layer,
            Value = m.Value,
            Source = m.SourceTag,
            SourceLabel = m.SourceLabel,
        };
    }
}
