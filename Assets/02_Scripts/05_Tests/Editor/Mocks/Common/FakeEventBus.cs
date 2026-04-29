using System;
using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>
    /// 테스트용 IEventBus. Publish 호출을 순서대로 기록해 GetPublished&lt;T&gt; 로 assert.
    /// 실제 핸들러 호출도 수행 — Subscribe/Unsubscribe 도 동작.
    /// </summary>
    public class FakeEventBus : IEventBus
    {
        private readonly List<object> _published = new List<object>();
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public IReadOnlyList<object> AllPublished => _published;

        public IReadOnlyList<T> GetPublished<T>() where T : struct
        {
            var result = new List<T>();
            foreach (object evt in _published)
            {
                if (evt is T typed) result.Add(typed);
            }
            return result;
        }

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                list.Remove(handler);
            }
        }

        public void Publish<T>(T eventData) where T : struct
        {
            _published.Add(eventData);
            if (_handlers.TryGetValue(typeof(T), out List<Delegate> list))
            {
                foreach (Delegate d in list.ToArray())
                {
                    ((Action<T>)d).Invoke(eventData);
                }
            }
        }

        public void Clear()
        {
            _handlers.Clear();
            _published.Clear();
        }

        public void Clear<T>() where T : struct
        {
            _handlers.Remove(typeof(T));
        }
    }
}
