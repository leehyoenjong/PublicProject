using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 제네릭 타입 기반 이벤트 버스 구현.
    /// 이벤트 타입(struct)을 키로 사용하여 타입 안전한 Pub/Sub 제공.
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
                _handlers[type] = Delegate.Combine(existing, handler);
            else
                _handlers[type] = handler;
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing))
                return;

            var result = Delegate.Remove(existing, handler);
            if (result == null)
                _handlers.Remove(type);
            else
                _handlers[type] = result;
        }

        public void Publish<T>(T eventData) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var handler))
                return;

            foreach (var del in handler.GetInvocationList())
            {
                try
                {
                    ((Action<T>)del).Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Handler error for {typeof(T).Name}: {e}");
                }
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        public void Clear<T>() where T : struct
        {
            _handlers.Remove(typeof(T));
        }
    }
}
