using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// MonoBehaviour에서 이벤트 자동 해제를 위한 헬퍼.
    /// 이 클래스를 상속하면 OnDestroy 시 구독이 자동 해제된다.
    /// </summary>
    public abstract class EventListener : MonoBehaviour
    {
        private readonly List<Action> _unsubscribeActions = new();
        private IEventBus _eventBus;

        protected IEventBus EventBus
        {
            get
            {
                _eventBus ??= ServiceLocator.Get<IEventBus>();
                return _eventBus;
            }
        }

        /// <summary>
        /// 이벤트를 구독하면서 해제 목록에 등록한다.
        /// OnDestroy에서 자동으로 Unsubscribe된다.
        /// </summary>
        protected void Listen<T>(Action<T> handler) where T : struct
        {
            EventBus.Subscribe(handler);
            _unsubscribeActions.Add(() => EventBus.Unsubscribe(handler));
        }

        protected virtual void OnDestroy()
        {
            foreach (var unsubscribe in _unsubscribeActions)
                unsubscribe();
            _unsubscribeActions.Clear();
        }
    }
}
