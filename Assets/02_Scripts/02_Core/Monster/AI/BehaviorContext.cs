using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// BT 실행 컨텍스트. 인스턴스 단위로 1개 보유.
    /// Blackboard 는 임의 string 키 → object 값 (커스텀 액션이 자유롭게 사용).
    /// CooldownEndTime 은 Cooldown 노드가 인덱스별로 다음 실행 가능 시각 저장.
    /// </summary>
    public class BehaviorContext
    {
        private readonly Dictionary<string, object> _blackboard = new Dictionary<string, object>();
        private readonly Dictionary<int, float> _cooldownEndTime = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _repeatCount = new Dictionary<int, int>();

        public IMonsterInstance Self { get; set; }
        public IUnit Target { get; set; }
        // 타겟의 런타임 InstanceId. IUnit.UnitId 는 카탈로그 MID 라 런타임 인스턴스와 불일치하므로,
        // 스킬 시전 등 "특정 인스턴스 대상" 액션은 이 값을 우선 사용한다(없으면 Target.UnitId fallback).
        public string TargetInstanceId { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float DeltaTime { get; set; }
        public float NowSeconds { get; set; }
        public IStatContainer SelfStats { get; set; }
        public IStatContainer TargetStats { get; set; }
        // 군집 인지(AvoidCrowding 등) 액션용 동족 집합. MonsterSystem 이 자신의 인스턴스 집합을 주입.
        // null 이면 군집 액션은 no-op — 단일 Self/Target 만 쓰는 액션엔 영향이 없다.
        public IReadOnlyCollection<IMonsterInstance> Neighbors { get; set; }

        public void SetBlackboard(string key, object value) => _blackboard[key] = value;
        public T GetBlackboard<T>(string key, T defaultValue = default)
        {
            return _blackboard.TryGetValue(key, out object v) && v is T t ? t : defaultValue;
        }

        public bool IsOnCooldown(int nodeIndex) =>
            _cooldownEndTime.TryGetValue(nodeIndex, out float end) && NowSeconds < end;

        public void SetCooldown(int nodeIndex, float seconds) =>
            _cooldownEndTime[nodeIndex] = NowSeconds + seconds;

        public int GetRepeatCount(int nodeIndex) =>
            _repeatCount.TryGetValue(nodeIndex, out int c) ? c : 0;

        public void IncrementRepeat(int nodeIndex) =>
            _repeatCount[nodeIndex] = GetRepeatCount(nodeIndex) + 1;

        public void ResetRepeat(int nodeIndex) =>
            _repeatCount[nodeIndex] = 0;
    }
}
