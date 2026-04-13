using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ScriptableObject 기반 이벤트 채널. 기획자가 Asset으로 생성/참조하여 Inspector에서 연결.
    /// 런타임에 Raise 호출 시 모든 리스너에 알림.
    /// </summary>
    [CreateAssetMenu(fileName = "GameEvent", menuName = "PublicFramework/Events/GameEvent")]
    public class GameEventSO : ScriptableObject
    {
        private readonly List<Action> _listeners = new List<Action>();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke();
            }
        }

        public void Register(Action listener)
        {
            if (listener == null) return;
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void Unregister(Action listener)
        {
            _listeners.Remove(listener);
        }
    }
}
