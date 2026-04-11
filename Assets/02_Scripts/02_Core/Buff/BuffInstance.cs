using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IBuffInstance 구현체 — 런타임 버프 상태 관리
    /// </summary>
    public class BuffInstance : IBuffInstance, IBuffUIData
    {
        private readonly BuffData _data;
        private readonly string _targetId;
        private readonly string _sourceId;
        private readonly List<IStatModifier> _modifiers = new List<IStatModifier>();

        private int _currentStack;
        private float _remainingDuration;
        private int _remainingTurns;
        private float _tickTimer;
        private bool _isExpired;

        public string BuffId => _data.BuffId;
        public string TargetId => _targetId;
        public string SourceId => _sourceId;
        public BuffCategory Category => _data.Category;
        public int CurrentStack => _currentStack;
        public int MaxStack => _data.MaxStack;
        public float RemainingDuration => _remainingDuration;
        public bool IsExpired => _isExpired;
        public bool IsUndispellable => _data.IsUndispellable;
        public IReadOnlyList<IStatModifier> Modifiers => _modifiers.AsReadOnly();

        // IBuffUIData
        public Sprite Icon => _data.Icon;
        public float RemainingRatio => _data.Duration > 0f ? _remainingDuration / _data.Duration : 1f;
        public string RemainingText => FormatRemainingText();
        public int StackCount => _currentStack;
        public string TooltipTitle => _data.DisplayName;
        public string TooltipDesc => _data.Description;

        public BuffData Data => _data;
        public IBuffEffect CustomEffect => _customEffect;

        private readonly IBuffEffect _customEffect;

        public BuffInstance(BuffData data, string targetId, string sourceId, IBuffEffect customEffect = null)
        {
            _data = data;
            _targetId = targetId;
            _sourceId = sourceId;
            _customEffect = customEffect;
            _currentStack = 1;
            _remainingDuration = data.Duration;
            _remainingTurns = Mathf.RoundToInt(data.Duration);
            _tickTimer = 0f;
            _isExpired = false;

            CreateModifiers();

            Debug.Log($"[BuffInstance] Created: {data.BuffId} on {targetId} from {sourceId}");
        }

        public void AddStack()
        {
            if (_currentStack >= _data.MaxStack) return;

            int oldStack = _currentStack;
            _currentStack++;

            UpdateModifierValues();

            Debug.Log($"[BuffInstance] Stack changed: {_data.BuffId} {oldStack} -> {_currentStack}");
        }

        public void RefreshDuration(RefreshPolicy policy)
        {
            switch (policy)
            {
                case RefreshPolicy.Reset:
                    _remainingDuration = _data.Duration;
                    _remainingTurns = Mathf.RoundToInt(_data.Duration);
                    break;
                case RefreshPolicy.Extend:
                    _remainingDuration += _data.Duration;
                    _remainingTurns += Mathf.RoundToInt(_data.Duration);
                    break;
                case RefreshPolicy.Keep:
                    break;
                case RefreshPolicy.Replace:
                    _remainingDuration = _data.Duration;
                    _remainingTurns = Mathf.RoundToInt(_data.Duration);
                    _currentStack = 1;
                    UpdateModifierValues();
                    break;
            }
        }

        public bool TickTime(float deltaTime, IEventBus eventBus)
        {
            if (_isExpired) return true;
            if (_data.DurationType == DurationType.Permanent) return false;
            if (_data.DurationType == DurationType.TurnBased) return false;
            if (_data.DurationType == DurationType.Conditional) return false;

            _remainingDuration -= deltaTime;

            if (_data.TickInterval > 0f)
            {
                _tickTimer += deltaTime;

                while (_tickTimer >= _data.TickInterval)
                {
                    _tickTimer -= _data.TickInterval;
                    int remainingTicks = _data.Duration > 0f
                        ? Mathf.CeilToInt(_remainingDuration / _data.TickInterval)
                        : 0;

                    _customEffect?.OnTick(_targetId, deltaTime);

                    eventBus?.Publish(new BuffTickEvent
                    {
                        TargetId = _targetId,
                        BuffId = _data.BuffId,
                        TickValue = _data.TickValue * _currentStack,
                        RemainingTicks = remainingTicks
                    });
                }
            }

            if (_remainingDuration <= 0f)
            {
                _isExpired = true;
                return true;
            }

            return false;
        }

        public bool ProcessTurn(IEventBus eventBus)
        {
            if (_isExpired) return true;
            if (_data.DurationType != DurationType.TurnBased) return false;

            _remainingTurns--;
            _remainingDuration = _remainingTurns;

            _customEffect?.OnTick(_targetId, 0f);

            if (_data.TickInterval > 0f)
            {
                int remainingTicks = _remainingTurns;

                eventBus?.Publish(new BuffTickEvent
                {
                    TargetId = _targetId,
                    BuffId = _data.BuffId,
                    TickValue = _data.TickValue * _currentStack,
                    RemainingTicks = remainingTicks
                });
            }

            if (_remainingTurns <= 0)
            {
                _isExpired = true;
                return true;
            }

            return false;
        }

        public void MarkExpired()
        {
            _isExpired = true;
        }

        public void ClearModifiers()
        {
            _modifiers.Clear();
        }

        private void CreateModifiers()
        {
            if (_data.TargetStats == null) return;

            foreach (StatModifierEntry entry in _data.TargetStats)
            {
                var modifier = new StatModifier(
                    entry.StatType,
                    entry.ModType,
                    entry.Value,
                    StatLayer.Buff,
                    this
                );
                _modifiers.Add(modifier);
            }
        }

        private void UpdateModifierValues()
        {
            _modifiers.Clear();

            if (_data.TargetStats == null) return;

            foreach (StatModifierEntry entry in _data.TargetStats)
            {
                float scaledValue = _data.StackPolicy == StackPolicy.Intensity
                    ? entry.Value * _currentStack
                    : entry.Value;

                var modifier = new StatModifier(
                    entry.StatType,
                    entry.ModType,
                    scaledValue,
                    StatLayer.Buff,
                    this
                );
                _modifiers.Add(modifier);
            }
        }

        private string FormatRemainingText()
        {
            if (_data.DurationType == DurationType.Permanent) return "";
            if (_data.DurationType == DurationType.TurnBased) return $"{_remainingTurns}T";

            if (_remainingDuration >= 60f)
            {
                int minutes = Mathf.FloorToInt(_remainingDuration / 60f);
                int seconds = Mathf.FloorToInt(_remainingDuration % 60f);
                return $"{minutes}:{seconds:D2}";
            }

            return $"{_remainingDuration:F1}s";
        }
    }
}
