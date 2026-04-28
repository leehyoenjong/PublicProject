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
        public Vector3 TargetPosition { get; set; }
        public float DeltaTime { get; set; }
        public float NowSeconds { get; set; }
        public IStatContainer SelfStats { get; set; }
        public IStatContainer TargetStats { get; set; }

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
