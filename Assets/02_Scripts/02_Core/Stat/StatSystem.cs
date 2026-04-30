using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IStatSystem 기본 구현. 엔티티별 StatContainer 관리.
    /// 게임 루프에서 TickAll(deltaTime) 호출 시 모든 컨테이너의 재생/임시 modifier 만료 처리.
    /// </summary>
    public class StatSystem : IStatSystem
    {
        private readonly Dictionary<string, IStatContainer> _containers = new();
        private readonly IEventBus _eventBus;
        private readonly ITimeProvider _timeProvider;

        public int Count => _containers.Count;

        public StatSystem(IEventBus eventBus = null, ITimeProvider timeProvider = null)
        {
            _eventBus = eventBus;
            _timeProvider = timeProvider;
            Debug.Log("[스탯] 초기화 시작.");
        }

        public IStatContainer CreateContainer(string ownerId, int level = 1)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                Debug.LogError("[스탯] ownerId가 null 또는 빈 값.");
                return null;
            }
            if (_containers.TryGetValue(ownerId, out IStatContainer existing))
            {
                Debug.LogWarning($"[스탯] 컨테이너 이미 존재함: {ownerId}");
                return existing;
            }
            var container = new StatContainer(ownerId, level, _eventBus, _timeProvider);
            _containers[ownerId] = container;
            Debug.Log($"[스탯] 컨테이너 생성됨: {ownerId}");
            return container;
        }

        public IStatContainer GetContainer(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId)) return null;
            return _containers.TryGetValue(ownerId, out IStatContainer c) ? c : null;
        }

        public bool RemoveContainer(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId)) return false;
            bool removed = _containers.Remove(ownerId);
            if (removed) Debug.Log($"[스탯] 컨테이너 제거됨: {ownerId}");
            return removed;
        }

        public void TickAll(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            foreach (var c in _containers.Values) c.Tick(deltaTime);
        }
    }
}
