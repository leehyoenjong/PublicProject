using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// async 콜백을 Unity 메인 스레드로 전달하기 위한 디스패처.
    /// <see cref="BackendBootstrapper"/> 가 자동 생성/유지(DontDestroyOnLoad)한다.
    /// 사용: <c>BackendMainThreadDispatcher.Instance?.Enqueue(() =&gt; ...)</c>
    /// </summary>
    public class BackendMainThreadDispatcher : MonoBehaviour
    {
        private static BackendMainThreadDispatcher _instance;
        private readonly Queue<Action> _queue = new();
        private readonly object _lock = new();

        public static BackendMainThreadDispatcher Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[BackendMainThreadDispatcher] 중복 인스턴스 감지 — 기존 인스턴스 유지");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public void Enqueue(Action action)
        {
            if (action == null) return;
            lock (_lock)
            {
                _queue.Enqueue(action);
            }
        }

        private void Update()
        {
            while (TryDequeue(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendMainThreadDispatcher] 콜백 예외: {e.Message}");
                }
            }
        }

        private bool TryDequeue(out Action action)
        {
            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    action = null;
                    return false;
                }
                action = _queue.Dequeue();
                return true;
            }
        }
    }
}
