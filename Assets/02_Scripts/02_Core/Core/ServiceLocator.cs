using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 서비스 등록/조회. static 접근으로 어디서든 사용 가능.
    /// 인터페이스 기반 등록으로 DIP 보장.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> _services = new();

        /// <summary>
        /// 인터페이스 타입으로 서비스 등록.
        /// </summary>
        public static void Register<TInterface>(TInterface service)
            where TInterface : IService
        {
            var type = typeof(TInterface);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] {type.Name} already registered. Overwriting.");
            }
            _services[type] = service;
            Debug.Log($"[ServiceLocator] {type.Name} registered.");
        }

        /// <summary>
        /// 인터페이스 타입으로 서비스 조회.
        /// </summary>
        public static TInterface Get<TInterface>() where TInterface : IService
        {
            var type = typeof(TInterface);
            if (_services.TryGetValue(type, out var service))
                return (TInterface)service;

            Debug.LogError($"[ServiceLocator] {type.Name} not found. Did you register it?");
            return default;
        }

        /// <summary>
        /// 서비스 등록 해제.
        /// </summary>
        public static void Unregister<TInterface>() where TInterface : IService
        {
            var type = typeof(TInterface);
            if (_services.Remove(type))
                Debug.Log($"[ServiceLocator] {type.Name} unregistered.");
        }

        /// <summary>
        /// 모든 서비스 초기화.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            Debug.Log("[ServiceLocator] All services cleared.");
        }

        /// <summary>
        /// 서비스 등록 여부 확인.
        /// </summary>
        public static bool Has<TInterface>() where TInterface : IService
        {
            return _services.ContainsKey(typeof(TInterface));
        }
    }
}
