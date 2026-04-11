using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IStatSystem 구현체 — 엔티티별 StatContainer 생성/조회/삭제
    /// </summary>
    public class StatSystem : IStatSystem
    {
        private readonly Dictionary<string, IStatContainer> _containers = new Dictionary<string, IStatContainer>();
        private readonly IEventBus _eventBus;

        public StatSystem(IEventBus eventBus)
        {
            _eventBus = eventBus;
            Debug.Log("[StatSystem] Init started");
        }

        public IStatContainer CreateContainer(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                Debug.LogError("[StatSystem] ownerId is null or empty");
                return null;
            }

            if (_containers.ContainsKey(ownerId))
            {
                Debug.LogWarning($"[StatSystem] Container already exists for: {ownerId}");
                return _containers[ownerId];
            }

            var container = new StatContainer(ownerId, _eventBus);
            _containers[ownerId] = container;

            Debug.Log($"[StatSystem] Container created for: {ownerId}");
            return container;
        }

        public IStatContainer GetContainer(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                Debug.LogError("[StatSystem] ownerId is null or empty");
                return null;
            }

            if (_containers.TryGetValue(ownerId, out IStatContainer container))
            {
                return container;
            }

            Debug.LogWarning($"[StatSystem] Container not found for: {ownerId}");
            return null;
        }

        public bool RemoveContainer(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                Debug.LogError("[StatSystem] ownerId is null or empty");
                return false;
            }

            bool removed = _containers.Remove(ownerId);

            if (removed)
            {
                Debug.Log($"[StatSystem] Container removed for: {ownerId}");
            }
            else
            {
                Debug.LogWarning($"[StatSystem] Container not found for removal: {ownerId}");
            }

            return removed;
        }
    }
}
