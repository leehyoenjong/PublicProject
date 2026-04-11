using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    [CreateAssetMenu(fileName = "NewBuffData", menuName = "PublicFramework/Buff/BuffData")]
    public class BuffData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string _buffId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("분류")]
        [SerializeField] private BuffCategory _category;
        [SerializeField] private ModifierType _modifierType;
        [SerializeField] private BuffSource _source;

        [Header("지속 시간")]
        [SerializeField] private DurationType _durationType;
        [SerializeField] private float _duration;

        [Header("틱 설정")]
        [SerializeField] private float _tickInterval;
        [SerializeField] private float _tickValue;

        [Header("중첩")]
        [SerializeField] private StackPolicy _stackPolicy;
        [SerializeField] private int _maxStack = 1;
        [SerializeField] private RefreshPolicy _refreshPolicy;

        [Header("기타")]
        [SerializeField] private int _priority;
        [SerializeField] private bool _isUndispellable;
        [SerializeField] private string[] _tags;

        [Header("스탯 수정")]
        [SerializeField] private StatModifierEntry[] _targetStats;

        public string BuffId => _buffId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public BuffCategory Category => _category;
        public ModifierType ModifierType => _modifierType;
        public BuffSource Source => _source;
        public DurationType DurationType => _durationType;
        public float Duration => _duration;
        public float TickInterval => _tickInterval;
        public float TickValue => _tickValue;
        public StackPolicy StackPolicy => _stackPolicy;
        public int MaxStack => _maxStack;
        public RefreshPolicy RefreshPolicy => _refreshPolicy;
        public int Priority => _priority;
        public bool IsUndispellable => _isUndispellable;
        public IReadOnlyList<string> Tags => _tags;
        public IReadOnlyList<StatModifierEntry> TargetStats => _targetStats;
    }

    [Serializable]
    public class StatModifierEntry
    {
        [SerializeField] private StatType _statType;
        [SerializeField] private StatModType _modType;
        [SerializeField] private float _value;

        public StatType StatType => _statType;
        public StatModType ModType => _modType;
        public float Value => _value;
    }
}
